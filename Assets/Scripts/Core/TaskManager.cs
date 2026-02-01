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
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            if (owner.AcceptsPromise == ItemPromise.None) return;

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

                Debug.Log($"[TaskManager] Registered pickup buffer owner {owner} for promise {promise}.");
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

                Debug.Log($"[TaskManager] Registered dropoff buffer owner {owner} for promise {promise}.");
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
                var resources = FindTaskResources(task);

                if (resources.HasValue && BindTaskToResources(task, resources.Value))
                {
                    PromoteTask(task);
                }
            }
        }

        #region Test-Test-Reserve Pattern

        /// <summary>
        /// Result of resource finding for a task.
        /// Contains the resources that would be used if the task is promoted.
        /// </summary>
        public struct TaskResources
        {
            public WorkStation Station;
            public ItemBufferOwner PickupSource;
            public ItemBufferOwner DropoffDestination;
        }

        /// <summary>
        /// Phase 1: Test if a task can be promoted.
        /// Finds and returns the resources that would be used without reserving them.
        /// Returns null if resources are not available.
        /// </summary>
        /// <param name="task">The task to check.</param>
        /// <returns>Resources if available, null otherwise.</returns>
        public TaskResources? FindTaskResources(TaskInstance task)
        {
            if (task.State != TaskState.Pending)
                return null;

            if (task.Type == TaskType.Delivery)
                return FindDeliveryPair(task);

            return FindWorkStation(task);
        }

        private TaskResources? FindWorkStation(TaskInstance task)
        {
            var workstationType = task.Definition.RequiredWorkstation;
            if (workstationType == WorkstationType.None)
                throw new InvalidOperationException("[TaskManager] Task requires a workstation.");

            CleanNullStations(workstationType);

            if (!freeWorkStations.TryGetValue(workstationType, out List<WorkStation> stations))
                return null;

            foreach (var station in stations)
            {
                if (station != null)
                {
                    bool needsItems = task.Type == TaskType.Refinement || task.Type == TaskType.Consumption;
                    if (!needsItems || station.AvailableItems > 0)
                    {
                        return new TaskResources { Station = station };
                    }
                }
            }

            return null;
        }

        private TaskResources? FindDeliveryPair(TaskInstance task)
        {
            // Debug.Log($"[TaskManager] Finding delivery pair for task {task.Id}.");
            var definition = task.Definition as DeliveryTaskDefinition;
            if (definition == null)
                throw new InvalidOperationException("[TaskManager] Delivery task must use DeliveryTaskDefinition.");

            var source = FindDeliverySource(definition.DeliversPromise);
            if (source == null) {
                // Debug.Log($"[TaskManager] No valid pickup source found for delivery task {task.Id}.");
                return null;
            }

            var destination = FindDeliveryDestination(definition);
            if (destination == null) {
                // Debug.Log($"[TaskManager] No valid dropoff destination found for delivery task {task.Id}.");
                return null;
            }

            return new TaskResources { PickupSource = source, DropoffDestination = destination };
        }

        /// <summary>
        /// Phase 2: Bind and reserve resources for a task.
        /// Takes the resources found by FindTaskResources and actually reserves them.
        /// Returns true if binding succeeded.
        /// </summary>
        /// <param name="task">The task to bind.</param>
        /// <param name="resources">The resources to use (from FindTaskResources).</param>
        /// <returns>True if successfully bound and reserved.</returns>
        public bool BindTaskToResources(TaskInstance task, TaskResources resources)
        {
            if (task.State != TaskState.Pending)
            {
                Debug.LogWarning($"[TaskManager] Cannot bind task {task.Id}: not in Pending state.");
                return false;
            }

            if (task.Type == TaskType.Delivery)
                return BindDeliveryPair(task, resources.PickupSource, resources.DropoffDestination);
            else
                return BindWorkStation(task, resources.Station);
        }

        private bool BindWorkStation(TaskInstance task, WorkStation station)
        {
            if (station == null)
                return false;

            bool needsItems = task.Type == TaskType.Refinement || task.Type == TaskType.Consumption;
            if (needsItems && !station.TryReserveItem(1)) return false;

            task.AssignWorkStation(station);
            RemoveFromFreeList(station);
            return true;
        }

        private bool BindDeliveryPair(TaskInstance task, ItemBufferOwner source, ItemBufferOwner destination)
        {
            if (source == null || destination == null)
                return false;

            if (!source.TryReserveItem(1))
            {
                Debug.Log($"[TaskManager] Failed to reserve item from source for delivery task {task.Id}.");
                return false;
            }

            if (!destination.TryReserveSlot(1))
            {
                Debug.Log($"[TaskManager] Failed to reserve slot at destination for delivery task {task.Id}.");
                source.ReleaseReservedItems(1);
                return false;
            }

            Debug.Log($"[TaskManager] Bound delivery task {task.Id} from source {source.Id} to destination {destination.Id}.");

            task.AssignPickupEntity(source);
            task.AssignDropoffEntity(destination);
            return true;
        }

        #endregion

        /// <summary>
        /// Finds the first valid pickup source that can provide the required item promise.
        /// </summary>
        /// <param name="promise">The item promise required for delivery.</param>
        /// <returns>The first valid pickup source or null if none found.</returns>
        private ItemBufferOwner FindDeliverySource(ItemPromise promise)
        {
            // Search by exact promise match first
            if (pickupSources.TryGetValue(promise, out var exactList))
            {
                // Debug.Log($"[TaskManager] Found {exactList.Count} pickup sources for promise {promise}.");
                for (int i = 0; i < exactList.Count; i++)
                {
                    var owner = exactList[i];
                    if (owner == null) {
                        // Debug.Log($"[TaskManager] Skipping null pickup source for promise {promise}.");
                        continue;
                    }

                    if (owner.AvailableItems > 0)
                    {
                        // Debug.Log($"[TaskManager] Selected pickup source {owner.Id} for promise {promise}.");
                        return owner;
                    }

                    // Debug.Log($"[TaskManager] Pickup source {owner.Id} has no available items for promise {promise}.");
                }
            }

            // No fallback to promise.None
            return null;
        }

        /// <summary>
        /// Finds the first valid dropoff destination that can accept the required item promise.
        /// </summary>
        /// <param name="definition">The delivery task definition.</param>
        /// <returns>The first valid dropoff destination or null if none found.</returns>
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
        /// Add a task with pre-assigned resources and try to claim it.
        /// If binding succeeds, the task is claimed immediately.
        /// If binding fails, the task goes to pending and resources are cleared.
        /// Used by NPCs to claim specific tasks with their preferred resources.
        /// </summary>
        /// <param name="task">Task with desired station/entity already assigned.</param>
        /// <returns>True if task is claimed successfully, false if binding fails.</returns>
        public bool AddTaskAndClaim(TaskInstance task)
        {
            if (task == null)
            {
                Debug.LogError($"[TaskManager] Cannot claim null task.");
                return false;
            }
            if (task.State != TaskState.Pending)
            {
                Debug.LogError($"[TaskManager] Cannot claim task {task.Id}: not in Pending state.");
                return false;
            }

            var resources = new TaskResources
            {
                Station = task.WorkStation,
                PickupSource = task.PickupEntity,
                DropoffDestination =task.DropoffEntity
            };

            if (BindTaskToResources(task, resources))
            {
                task.TransitionTo(TaskState.Claimed);
                claimedTasks.Add(task);
                Debug.Log($"[TaskManager] Task {task.Id} claimed with preferred resources.");
                return true;
            }

            Debug.Log($"[TaskManager] Failed to bind task {task.Id} to preferred resources. Moving to pending.");
            pendingTasks.Add(task);
            task.ClearWorkStation();
            task.ClearDeliveryEntities();
            return false;
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
            task.TransitionTo(TaskState.Completed);
            executingTasks.Remove(task);
        }

        #endregion
    }
}
