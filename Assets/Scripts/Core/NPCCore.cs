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

        public ItemPromise CurrentMask;
        public NPCState State;
        public bool IsIdle => State == NPCState.Idle;
        public float Hunger;
        public float MaxHunger;

        public TaskInstance CurrentTask { get; private set; }
        public Transform MoveTarget { get; private set; }
        protected float distanceTolerance;
        protected float moveSpeed = 2f;

        public event Action<NPCCore> OnDied;

        protected virtual void Awake()
        {
            if (capacity <= 0) Debug.LogWarning($"NPC {name} has non-positive capacity.");
        }

        protected virtual void Update()
        {
            if (State == NPCState.Idle)
            {
                TryClaimTask();
            }

            if (MoveTarget != null)
            {
                MoveTowardsTarget();

                if (IsAtTarget())
                {
                    MoveTarget = null;
                    AdvanceTaskAndState();
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

        protected virtual void MoveTowardsTarget()
        {
            if (MoveTarget == null)
                return;

            Vector2Int direction = PathfindingSystem.Instance?.GetDirection(transform.position, MoveTarget.position) ?? Vector2Int.zero;

            if (direction != Vector2Int.zero)
            {
                Vector3 moveDirection = new Vector3(direction.x, 0, direction.y);
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
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
            TaskInstance newTask = CurrentTask.Repeat();
            if (newTask != null)
            {
                Debug.Log($"NPC {name} repeating task {newTask.Id}.");
                // TODO: Add to task list
                SetupTask(newTask);
                return;
            } else
            {
                CurrentTask = null;
                State = NPCState.Idle;
            }
        }

        private void ResetStates()
        {
            CurrentTask = null;
            State = NPCState.Idle;
            MoveTarget = null;
        }
    }
}
