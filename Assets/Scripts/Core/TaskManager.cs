using UnityEngine;
using System;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Manages tasks and workstations in the colony.
    ///
    /// Responsibilities:
    /// - Track tasks in different states (Pending, Ready, Claimed, Executing)
    /// - Track free workstations by type
    /// - Move tasks from Pending to Ready when workstations are available
    /// - Provide interface for NPCs to claim tasks
    /// - Subscribe to Infrastructure events for workstation registration
    ///
    /// Design:
    /// - Singleton pattern for global access
    /// - Event-based workstation discovery
    /// - Tasks assigned to workstations when moving to Ready state
    /// - NPCs claim tasks from the Ready list
    /// - Periodic promotion checks: tasks reserve stations, stations leave free list
    /// - Null stations cleaned lazily when encountered
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        private List<TaskInstance> pendingTasks = new List<TaskInstance>();
        private List<TaskInstance> readyTasks = new List<TaskInstance>();
        private List<TaskInstance> claimedTasks = new List<TaskInstance>();
        private List<TaskInstance> executingTasks = new List<TaskInstance>();

        private Dictionary<WorkstationType, List<WorkStation>> freeWorkStations = new Dictionary<WorkstationType, List<WorkStation>>();
        private List<WorkStation> allWorkStations = new List<WorkStation>();

        // Partition buffer owners by (IsForPickup, ItemPromise) for efficient delivery routing
        // pickupSources[promise] = list of buffer owners available for pickup
        // dropoffDestinations[promise] = list of buffer owners available for dropoff
        private Dictionary<ItemPromise, List<ItemBufferOwner>> pickupSources = new Dictionary<ItemPromise, List<ItemBufferOwner>>();
        private Dictionary<ItemPromise, List<ItemBufferOwner>> dropoffDestinations = new Dictionary<ItemPromise, List<ItemBufferOwner>>();

        public int PendingCount => pendingTasks.Count;
        public int ReadyCount => readyTasks.Count;
        public int ClaimedCount => claimedTasks.Count;
        public int ExecutingCount => executingTasks.Count;

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Infrastructure.InfrastructureStarted += OnInfrastructureStarted;
            Infrastructure.InfrastructureDestroyed += OnInfrastructureDestroyed;
            ItemBufferOwner.BufferOwnerStarted += OnBufferOwnerStarted;
            ItemBufferOwner.BufferOwnerDestroyed += OnBufferOwnerDestroyed;
        }

        private void OnDestroy()
        {
            Infrastructure.InfrastructureStarted -= OnInfrastructureStarted;
            Infrastructure.InfrastructureDestroyed -= OnInfrastructureDestroyed;
            ItemBufferOwner.BufferOwnerStarted -= OnBufferOwnerStarted;
            ItemBufferOwner.BufferOwnerDestroyed -= OnBufferOwnerDestroyed;
        }

        #endregion

        #region Workstation Event Handlers

        private void OnInfrastructureStarted(Infrastructure infrastructure)
        {
            if (infrastructure is WorkStation station)
            {
                AddWorkStation(station);
                AddFreeWorkStation(station);
            }
        }

        private void OnInfrastructureDestroyed(Guid id)
        {
            RemoveWorkStation(id);
        }

        private void OnBufferOwnerStarted(ItemBufferOwner owner)
        {
            AddBufferOwner(owner);
        }

        private void OnBufferOwnerDestroyed(Guid id)
        {
            RemoveBufferOwner(id);
        }

        #endregion

        #region Workstation Management

        private void AddWorkStation(WorkStation station)
        {
            if (station == null || allWorkStations.Contains(station))
                return;

            allWorkStations.Add(station);
        }

        private void RemoveWorkStation(Guid id)
        {
            for (int i = allWorkStations.Count - 1; i >= 0; i--)
            {
                var station = allWorkStations[i];
                if (station == null || station.Id == id)
                    allWorkStations.RemoveAt(i);
            }

            foreach (var entry in freeWorkStations)
            {
                var list = entry.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var station = list[i];
                    if (station == null || station.Id == id)
                        list.RemoveAt(i);
                }
            }
        }

        private void AddFreeWorkStation(WorkStation station)
        {
            if (!freeWorkStations.TryGetValue(station.Type, out List<WorkStation> list))
            {
                list = new List<WorkStation>();
                freeWorkStations[station.Type] = list;
            }
            list.Add(station);
        }

        private void AddBufferOwner(ItemBufferOwner owner)
        {
            if (owner == null)
                return;

            var promise = owner.AcceptsPromise;
            if (owner.IsForPickup)
            {
                if (!pickupSources.TryGetValue(promise, out var list))
                {
                    list = new List<ItemBufferOwner>();
                    pickupSources[promise] = list;
                }
                if (!list.Contains(owner))
                    list.Add(owner);
            }
            else
            {
                if (!dropoffDestinations.TryGetValue(promise, out var list))
                {
                    list = new List<ItemBufferOwner>();
                    dropoffDestinations[promise] = list;
                }
                if (!list.Contains(owner))
                    list.Add(owner);
            }
        }

        private void RemoveBufferOwner(Guid id)
        {
            RemoveFromDictionary(pickupSources, id);
            RemoveFromDictionary(dropoffDestinations, id);
        }

        private void RemoveFromDictionary(Dictionary<ItemPromise, List<ItemBufferOwner>> dict, Guid id)
        {
            foreach (var entry in dict)
            {
                var list = entry.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var owner = list[i];
                    if (owner == null || owner.Id == id)
                        list.RemoveAt(i);
                }
            }
        }

        public IEnumerable<WorkStation> GetFreeWorkStations(WorkstationType type)
        {
            if (freeWorkStations.TryGetValue(type, out List<WorkStation> list))
                return list;
            return Array.Empty<WorkStation>();
        }

        private void CleanNullStations(WorkstationType type)
        {
            if (freeWorkStations.TryGetValue(type, out List<WorkStation> list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] == null)
                        list.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Task Management

        protected void Update()
        {
            TryPromoteTasks();
        }

        /// <summary>
        /// Add a new task from a TaskDefinition.
        /// Task starts in Pending state.
        /// </summary>
        public TaskInstance AddTask(TaskDefinition definition)
        {
            var task = new TaskInstance(definition);
            pendingTasks.Add(task);
            return task;
        }

        public TaskInstance AddTask(TaskInstance task)
        {
            if (task.State == TaskState.Pending) pendingTasks.Add(task);
            else throw new NotImplementedException("Adding non-pending tasks is not supported.");
            return task;
        }

        /// <summary>
        /// Try to move tasks from Pending to Ready state.
        /// Periodically called to check for available workstations.
        /// </summary>
        public void TryPromoteTasks()
        {
            for (int i = pendingTasks.Count - 1; i >= 0; i--)
            {
                var task = pendingTasks[i];
                if (task.Type == TaskType.Delivery)
                {
                    if (TryAssignDelivery(task))
                    {
                        PromoteTask(task);
                    }
                    continue;
                }

                var workstationType = task.Definition.RequiredWorkstation;
                if (workstationType == WorkstationType.None)
                {
                    throw new InvalidOperationException("Task requires a workstation.");
                }

                CleanNullStations(workstationType);

                if (!freeWorkStations.TryGetValue(workstationType, out List<WorkStation> stations))
                    continue;

                foreach (var station in stations)
                {
                    if (station == null)
                        continue;

                    if (CanReserveStation(task, station))
                    {
                        task.AssignWorkStation(station);
                        RemoveFromFreeList(station);
                        PromoteTask(task);
                        break;
                    }
                }
            }
        }

        private bool CanReserveStation(TaskInstance task, WorkStation station)
        {
            var taskType = task.Definition.Type;

            switch (taskType)
            {
                case TaskType.Production:
                    return true;
                case TaskType.Delivery:
                    throw new InvalidOperationException("Delivery tasks don't use workstations.");
                case TaskType.Refinement:
                case TaskType.Consumption:
                    return station.AvailableItems > 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(taskType), taskType, null);
            }
        }

        private bool TryAssignDelivery(TaskInstance task)
        {
            Debug.Log($"Trying to assign delivery task {task.Id}.");
            var definition = task.Definition as DeliveryTaskDefinition;
            if (definition == null)
                throw new InvalidOperationException("Delivery task must use DeliveryTaskDefinition.");

            var source = FindDeliverySource(definition.DeliversPromise);
            if (source == null) {
                Debug.Log($"No valid pickup source found for delivery task {task.Id}.");
                return false;
            }

            var destination = FindDeliveryDestination(definition);
            if (destination == null) {
                Debug.Log($"No valid dropoff destination found for delivery task {task.Id}.");
                return false;
            }

            if (!source.TryReserveItem(1)) {
                Debug.Log($"Failed to reserve item from source for delivery task {task.Id}.");
                return false;
            }

            if (!destination.TryReserveSlot(1))
            {
                Debug.Log($"Failed to reserve slot at destination for delivery task {task.Id}.");
                source.ReleaseReservedItems(1);
                return false;
            }

            Debug.Log($"Prepared delivery task {task.Id} from source {source.Id} to destination {destination.Id}.");

            task.AssignPickupEntity(source);
            task.AssignDropoffEntity(destination);
            return true;
        }

        private ItemBufferOwner FindDeliverySource(ItemPromise promise)
        {
            // Search by exact promise match first
            if (pickupSources.TryGetValue(promise, out var exactList))
            {
                for (int i = 0; i < exactList.Count; i++)
                {
                    var owner = exactList[i];
                    if (owner == null)
                        continue;

                    if (owner.AvailableItems <= 0)
                        continue;

                    return owner;
                }
            }

            // No fallback to promise.None
            return null;
        }

        private ItemBufferOwner FindDeliveryDestination(DeliveryTaskDefinition definition)
        {
            // Search by exact promise match first
            if (dropoffDestinations.TryGetValue(definition.DeliversPromise, out var exactList))
            {
                for (int i = 0; i < exactList.Count; i++)
                {
                    var owner = exactList[i];
                    if (owner == null)
                        continue;

                    if (owner.AvailableSlots <= 0)
                        continue;

                    return owner;
                }
            }

            // No fallback to promise.None
            return null;
        }


        private void RemoveFromFreeList(WorkStation station)
        {
            if (freeWorkStations.TryGetValue(station.Type, out List<WorkStation> list))
            {
                list.Remove(station);
            }
        }

        private void PromoteTask(TaskInstance task)
        {
            Debug.Log($"Task {task.Id} promoted from Pending to Ready.");
            pendingTasks.Remove(task);
            task.TransitionTo(TaskState.Ready);
            readyTasks.Add(task);
        }

        #endregion

        #region Task Claiming

        /// <summary>
        /// Claim a ready task for an NPC.
        /// Returns the claimed task or null if none available.
        /// </summary>
        public TaskInstance ClaimTask()
        {
            if (readyTasks.Count == 0)
                return null;

            var task = readyTasks[0];
            readyTasks.RemoveAt(0);
            task.TransitionTo(TaskState.Claimed);
            claimedTasks.Add(task);
            return task;
        }

        /// <summary>
        /// Get all ready tasks (for NPC inspection/selection).
        /// </summary>
        public IReadOnlyList<TaskInstance> GetReadyTasks()
        {
            return readyTasks;
        }

        /// <summary>
        /// Get all pending tasks (for UI display).
        /// </summary>
        public IReadOnlyList<TaskInstance> GetPendingTasks()
        {
            return pendingTasks;
        }

        #endregion

        #region Task Completion

        /// <summary>
        /// Mark a task as executing (NPC started work).
        /// </summary>
        public void StartExecuting(TaskInstance task)
        {
            if (claimedTasks.Remove(task))
            {
                task.TransitionTo(TaskState.Executing);
                executingTasks.Add(task);
            }
        }

        /// <summary>
        /// Complete a task and clean up.
        /// </summary>
        public void CompleteTask(TaskInstance task)
        {
            executingTasks.Remove(task);
        }

        #endregion
    }
}
