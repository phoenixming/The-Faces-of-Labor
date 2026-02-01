using UnityEngine;
using System;
using System.Threading.Tasks;

namespace FacesOfLabor.Core
{
    public class NPCCore : ItemBufferOwner
    {
        public enum NPCState
        {
            Idle,
            Working,
            Dead
        }

        public ItemPromise CurrentMask => AcceptsPromise;
        public NPCState State;
        public bool IsIdle => State == NPCState.Idle;
        public float Hunger;
        public float MaxHunger;

        public TaskInstance CurrentTask { get; private set; }
        public Transform MoveTarget { get; private set; }
        protected float distanceTolerance;
        protected float moveSpeed = 2f;

        protected Vector2Int previousGridPosition;

        public event Action<NPCCore> OnDied;

        protected virtual void Awake()
        {
            if (capacity <= 0) Debug.LogWarning($"NPC {name} has non-positive capacity.");

            forPickup = true;

            GridSystem gridSystem = GridSystem.Instance;
            if (gridSystem != null)
            {
                previousGridPosition = gridSystem.WorldToGrid(transform.position);
            }
        }

        protected virtual void Update()
        {
            CheckGridCellChanged();

            if (State == NPCState.Idle && AvailableSlots > 0)
            {
                TryClaimTask();
            }

            if (MoveTarget != null)
            {
                MoveTowardsTarget();

                if (IsAtTarget())
                {
                    Debug.Log($"NPC {name} reached target {MoveTarget.name}.");
                    MoveTarget = null;
                    AdvanceTaskAndState();
                }
            }
        }

        protected virtual void CheckGridCellChanged()
        {
            GridSystem gridSystem = GridSystem.Instance;
            if (gridSystem == null)
                return;

            Vector2Int currentGridPosition = gridSystem.WorldToGrid(transform.position);

            if (currentGridPosition != previousGridPosition)
            {
                OnGridCellChanged(previousGridPosition, currentGridPosition);
                previousGridPosition = currentGridPosition;
            }
        }

        protected virtual void OnGridCellChanged(Vector2Int fromCell, Vector2Int toCell)
        {
            GridSystem gridSystem = GridSystem.Instance;
            if (gridSystem == null)
                return;

            MaskStation maskStation = gridSystem.GetMaskStation(toCell);
            if (maskStation != null)
            {
                InteractWithMaskStation(maskStation);
            }
        }

        protected virtual void InteractWithMaskStation(MaskStation maskStation)
        {
            if (CurrentMask != ItemPromise.None)
            {
                if (maskStation.TryPutMask(CurrentMask))
                {
                    Debug.Log($"NPC {name} removed mask '{CurrentMask}' at MaskStation.");
                    acceptsPromise = ItemPromise.None;
                    DeregisterFromTaskManager();
                    Debug.Log($"NPC {name} deregistered from TaskManager as buffer holder.");
                }
                else
                {
                    Debug.LogWarning($"NPC {name} could not return mask '{CurrentMask}' to MaskStation.");
                }
            }
            else
            {
                if (maskStation.TryGetMask(out ItemPromise newMask))
                {
                    acceptsPromise = newMask;
                    RegisterWithTaskManager();
                    Debug.Log($"NPC {name} picked up mask '{CurrentMask}' at MaskStation and registered as buffer holder.");
                }
            }
        }

        protected virtual bool IsAtTarget()
        {
            if (MoveTarget == null)
                return false;

            float distance = Vector3.Distance(transform.position, MoveTarget.position);
            return distance <= distanceTolerance;
        }

        /// <summary>
        /// Moves the NPC towards the current MoveTarget by heading towards the next cell center.
        /// </summary>
        protected virtual void MoveTowardsTarget()
        {
            if (MoveTarget == null)
                throw new InvalidOperationException("No MoveTarget set for NPC.");

            GridSystem gridSystem = GridSystem.Instance;
            if (gridSystem == null)
                throw new InvalidOperationException("GridSystem instance not found.");

            Vector2Int currentCell = gridSystem.WorldToGrid(transform.position);
            Vector2Int targetCell = gridSystem.WorldToGrid(MoveTarget.position);


            if (PathfindingSystem.Instance.IsReachable(currentCell, targetCell))
            {
                Vector2Int direction = PathfindingSystem.Instance.GetDirection(currentCell, targetCell);
                Vector2Int nextCell = currentCell + direction;
                Vector3 nextCellCenter = gridSystem.GetCellCenterWorld(nextCell);
                Vector3 directionToNext = (nextCellCenter - transform.position).normalized;
                transform.position += moveSpeed * Time.deltaTime * directionToNext;
            }
            else
            {
                Debug.LogWarning($"NPC {name} cannot move to target {MoveTarget.name} from current cell {currentCell} to target cell {targetCell}.");
            }
        }

        protected virtual void AdvanceTaskAndState()
        {
            if (CurrentTask == null) throw new InvalidOperationException("No current task to advance.");
            Debug.Log($"Advancing task {CurrentTask.Id} for NPC {name} in state {CurrentTask.State}.");

            switch (CurrentTask.State)
            {
                case TaskState.Claimed:
                    TaskManager.Instance.StartExecuting(CurrentTask);

                    // NPCs performing delivery tasks need to go to dropoff next
                    // NPCs performing workstation tasks stay where they are
                    if (CurrentTask.Type == TaskType.Delivery) {
                        // Move item from pickup to internal buffer
                        AddItem(CurrentTask.PickupEntity.ConsumeItem());
                        if (!TryReserveItem(1))
                        {
                            Debug.LogError($"NPC {name} could not reserve item for delivery task {CurrentTask.Id}.");
                            FinishTask();
                            return;
                        }
                        Debug.Log($"NPC {name} picked up item {PeekItem()} for delivery task {CurrentTask.Id}.");
                        MoveTarget = CurrentTask.DropoffEntity.transform;
                    } else {
                        MoveTarget = null;
                        CurrentTask.WorkStation.ExecuteTask(CurrentTask.Definition, FinishStationTask);
                    }
                    break;
                case TaskState.Executing:
                    if (CurrentTask.Type == TaskType.Delivery)
                    {
                        FinishDeliveryTask();
                    }
                    break;
                default:
                    break;
            }
        }

        public bool TryClaimTask()
        {
            var task = TaskManager.Instance?.ClaimTask();
            if (task == null)
                return false;

            SetupTask(task);

            return true;
        }

        public void SetupTask(TaskInstance task)
        {
            CurrentTask = task;
            task.TransitionTo(TaskState.Claimed);

            Debug.Log($"NPC {name} claimed {task.Type} task {task.Id}.");

            State = NPCState.Working;
            MoveTarget = task.HasWorkStation ? task.WorkStation.transform :
                         task.Type == TaskType.Delivery && task.HasPickupEntity ? task.PickupEntity.transform :
                         null;

            if (task.Type != TaskType.Consumption)
            {
                // Reserve a slot in item buffer
                if (!TryReserveSlot(1))
                {
                    Debug.LogError($"NPC {name} could not reserve personal item slot for task {task.Id}.");
                    CurrentTask = null;
                    State = NPCState.Idle;
                    return;
                }
            }

            if (MoveTarget == null)
            {
                Debug.LogError($"NPC {name} claimed task {task.Id} but has no valid move target.");
                CurrentTask = null;
                State = NPCState.Idle;
                return;
            }

            distanceTolerance = task.Type == TaskType.Delivery ? 1.0f : 0.1f;
        }

        private void FinishStationTask(RealItem resultItem)
        {
            if (resultItem.BaseType != BaseItemType.None)
            {
                AddItem(resultItem);
                Debug.Log($"NPC {name} received item {resultItem} from workstation task {CurrentTask.Id}.");
                Debug.Log($"NPC {name} now holds {AvailableItems} free items.");
            }
            else
            {
                Debug.Log($"NPC {name} workstation task {CurrentTask.Id} produced no item.");
            }

            FinishTask();
        }

        private void FinishDeliveryTask()
        {
            var item = ConsumeItem();
            CurrentTask.DropoffEntity.AddItem(item);
            Debug.Log($"NPC {name} delivered item '{item.State} {item.BaseType}' to {CurrentTask.DropoffEntity} for delivery task {CurrentTask.Id}.");

            FinishTask();
        }

        private void FinishTask()
        {
            Debug.Log($"NPC {name} completed task {CurrentTask.Id}.");
            TaskManager.Instance.CompleteTask(CurrentTask);
            // TODO: Check for emergency state (e.g., starvation)
            // TODO: Check for re-scheduled tasks
            if (CurrentTask.RepeatCount > 0) {
                TaskInstance newTask = CurrentTask.Repeat();
                if (AvailableSlots == 0)
                {
                    Debug.Log($"NPC {name} has no available slots to repeat task {CurrentTask.Id}.");
                    // Put the task back in TaskManager
                    newTask.ClearDeliveryEntities();
                    newTask.ClearWorkStation();
                    TaskManager.Instance.AddTask(newTask);
                }
                else if (TaskManager.Instance.AddTaskAndClaim(newTask))
                {
                    Debug.Log($"NPC {name} repeating task {newTask.Id}.");
                    // TODO: Add to task list
                    SetupTask(newTask);
                    return;
                }
                else
                {
                    Debug.LogError($"NPC {name} could not repeat task {CurrentTask.Id}.");
                }
            }

            // Default task clean-up
            CurrentTask = null;
            State = NPCState.Idle;

        }

        private void ResetStates()
        {
            CurrentTask = null;
            State = NPCState.Idle;
            MoveTarget = null;
        }
    }
}
