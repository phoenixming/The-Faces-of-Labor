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

        // Interval in seconds to check for task promotion
        [SerializeField] protected float updateInterval = 0.5f;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Debug")]
        [Tooltip("Enable debug logging for task promotion.")]
        [SerializeField] private bool debugPrintEnabled = false;
#endif

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

        protected void Start()
        {
            StartCoroutine(TaskPromotionLoop());
        }

        private void DebugLog(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugPrintEnabled)
                Debug.Log($"[TaskManager] {message}");
#endif
        }

        private System.Collections.IEnumerator TaskPromotionLoop()
        {
            while (true)
            {
                TryPromoteTasks();
                yield return new WaitForSeconds(updateInterval);
            }
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

        public void AddFreeWorkStation(WorkStation station)
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
            Debug.Log($"[TaskManager] Adding {task.Type} task {task.Id}.");
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
            DebugLog($"=== TryPromoteTasks: {pendingTasks.Count} pending tasks ===");

            for (int i = pendingTasks.Count - 1; i >= 0; i--)
            {
                var task = pendingTasks[i];
                DebugLog($"Inspecting task {task.Id} ({task.Type})");

                var resources = FindTaskResources(task);

                if (resources.HasValue)
                {
                    var res = resources.Value;
                    string resourceInfo = task.Type == TaskType.Delivery
                        ? $"source={res.PickupSource?.Id}, destination={res.DropoffDestination?.Id}"
                        : $"station={res.Station?.Id}";

                    DebugLog($"Task {task.Id}: Found resources [{resourceInfo}]. Attempting bind.");

                    if (BindTaskToResources(task, resources.Value))
                    {
                        DebugLog($"Task {task.Id}: Bind succeeded. Promoting to Ready.");
                        PromoteTask(task);
                    }
                    else
                    {
                        DebugLog($"Task {task.Id}: Bind failed.");
                    }
                }
                else
                {
                    DebugLog($"Task {task.Id}: No resources available. Skipping.");
                }
            }

            DebugLog($"=== TryPromoteTasks complete: {pendingTasks.Count} pending, {readyTasks.Count} ready ===");
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
            {
                DebugLog($"FindTaskResources({task.Id}): Not in Pending state.");
                return null;
            }

            if (task.Type == TaskType.Delivery)
                return FindDeliveryPair(task);

            return FindWorkStation(task);
        }

        private TaskResources? FindWorkStation(TaskInstance task)
        {
            var workstationType = task.Definition.RequiredWorkstation;
            if (workstationType == WorkstationType.None)
                throw new InvalidOperationException("[TaskManager] Task requires a workstation.");

            DebugLog($"FindWorkStation({task.Id}): Looking for {workstationType} stations.");

            CleanNullStations(workstationType);

            if (!freeWorkStations.TryGetValue(workstationType, out List<WorkStation> stations))
            {
                DebugLog($"FindWorkStation({task.Id}): No stations of type {workstationType} registered.");
                return null;
            }

            int availableCount = 0;
            foreach (var station in stations)
            {
                if (station == null) continue;

                availableCount++;
                bool needsItems = task.Type == TaskType.Refinement || task.Type == TaskType.Consumption;
                string itemCheck = needsItems ? $"items={station.AvailableItems}" : "no item check";

                DebugLog($"FindWorkStation({task.Id}): Checking station {station.Id}, {itemCheck}");

                if (!needsItems || station.AvailableItems > 0)
                {
                    DebugLog($"FindWorkStation({task.Id}): Selected station {station.Id}.");
                    return new TaskResources { Station = station };
                }
            }

            DebugLog($"FindWorkStation({task.Id}): No viable stations found ({availableCount} available, none suitable).");
            return null;
        }

        private TaskResources? FindDeliveryPair(TaskInstance task)
        {
            var definition = task.Definition as DeliveryTaskDefinition;
            if (definition == null)
                throw new InvalidOperationException("[TaskManager] Delivery task must use DeliveryTaskDefinition.");

            var promise = definition.DeliversPromise;
            DebugLog($"FindDeliveryPair({task.Id}): Looking for delivery of {promise}.");

            var source = FindDeliverySource(promise);
            if (source == null)
            {
                DebugLog($"FindDeliveryPair({task.Id}): No valid pickup source found.");
                return null;
            }

            DebugLog($"FindDeliveryPair({task.Id}): Found pickup source {source.Id}.");

            var destination = FindDeliveryDestination(definition);
            if (destination == null)
            {
                DebugLog($"FindDeliveryPair({task.Id}): No valid dropoff destination found.");
                return null;
            }

            DebugLog($"FindDeliveryPair({task.Id}): Found dropoff destination {destination.Id}.");
            return new TaskResources { PickupSource = source, DropoffDestination = destination };
        }

        /// <returns>True if successfully bound and reserved.</returns>
        public bool BindTaskToResources(TaskInstance task, TaskResources resources)
        {
            if (task.State != TaskState.Pending)
            {
                DebugLog($"BindTaskToResources({task.Id}): Cannot bind - not in Pending state.");
                return false;
            }

            DebugLog($"BindTaskToResources({task.Id}): Binding to resources.");

            if (task.Type == TaskType.Delivery)
                return BindDeliveryPair(task, resources.PickupSource, resources.DropoffDestination);
            else
                return BindWorkStation(task, resources.Station);
        }

        private bool BindWorkStation(TaskInstance task, WorkStation station)
        {
            if (station == null)
            {
                DebugLog($"BindWorkStation({task.Id}): Station is null.");
                return false;
            }

            bool needsItems = task.Type == TaskType.Refinement || task.Type == TaskType.Consumption;
            DebugLog($"BindWorkStation({task.Id}): Station {station.Id}, needsItems={needsItems}");

            if (needsItems && !station.TryReserveItem(1))
            {
                DebugLog($"BindWorkStation({task.Id}): Failed to reserve item from station {station.Id}.");
                return false;
            }

            task.AssignWorkStation(station);
            RemoveFromFreeList(station);
            DebugLog($"BindWorkStation({task.Id}): Successfully bound to station {station.Id}.");
            return true;
        }

        private bool BindDeliveryPair(TaskInstance task, ItemBufferOwner source, ItemBufferOwner destination)
        {
            if (source == null || destination == null)
            {
                DebugLog($"BindDeliveryPair({task.Id}): Source or destination is null.");
                return false;
            }

            DebugLog($"BindDeliveryPair({task.Id}): Source={source.Id}, Destination={destination.Id}");

            if (!source.TryReserveItem(1))
            {
                DebugLog($"BindDeliveryPair({task.Id}): Failed to reserve item from source {source.Id}.");
                return false;
            }

            DebugLog($"BindDeliveryPair({task.Id}): Reserved item from source {source.Id}.");

            if (!destination.TryReserveSlot(1))
            {
                DebugLog($"BindDeliveryPair({task.Id}): Failed to reserve slot at destination {destination.Id}. Rolling back.");
                source.ReleaseReservedItems(1);
                return false;
            }

            DebugLog($"BindDeliveryPair({task.Id}): Reserved slot at destination {destination.Id}.");

            task.AssignPickupEntity(source);
            task.AssignDropoffEntity(destination);
            DebugLog($"BindDeliveryPair({task.Id}): Successfully bound delivery from {source.Id} to {destination.Id}.");
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
            DebugLog($"FindDeliverySource: Searching for promise {promise}.");

            if (!pickupSources.TryGetValue(promise, out var exactList))
            {
                DebugLog($"FindDeliverySource: No pickup sources registered for promise {promise}.");
                return null;
            }

            DebugLog($"FindDeliverySource: Checking {exactList.Count} sources for promise {promise}.");

            for (int i = 0; i < exactList.Count; i++)
            {
                var owner = exactList[i];
                if (owner == null)
                {
                    DebugLog($"FindDeliverySource: Skipping null source at index {i}.");
                    continue;
                }

                DebugLog($"FindDeliverySource: Checking source {owner.Id}, available items: {owner.AvailableItems}");

                if (owner.AvailableItems > 0)
                {
                    DebugLog($"FindDeliverySource: Selected source {owner.Id}.");
                    return owner;
                }

                DebugLog($"FindDeliverySource: Source {owner.Id} has no available items.");
            }

            DebugLog($"FindDeliverySource: No valid source found for promise {promise}.");
            return null;
        }

        /// <summary>
        /// Finds the first valid dropoff destination that can accept the required item promise.
        /// </summary>
        /// <param name="definition">The delivery task definition.</param>
        /// <returns>The first valid dropoff destination or null if none found.</returns>
        private ItemBufferOwner FindDeliveryDestination(DeliveryTaskDefinition definition)
        {
            var promise = definition.DeliversPromise;
            DebugLog($"FindDeliveryDestination: Searching for promise {promise}.");

            if (!dropoffDestinations.TryGetValue(promise, out var exactList))
            {
                DebugLog($"FindDeliveryDestination: No destinations registered for promise {promise}.");
                return null;
            }

            DebugLog($"FindDeliveryDestination: Checking {exactList.Count} destinations for promise {promise}.");

            for (int i = 0; i < exactList.Count; i++)
            {
                var owner = exactList[i];
                if (owner == null)
                {
                    DebugLog($"FindDeliveryDestination: Skipping null destination at index {i}.");
                    continue;
                }

                DebugLog($"FindDeliveryDestination: Checking destination {owner.Id}, available slots: {owner.AvailableSlots}");

                if (owner.AvailableSlots > 0)
                {
                    DebugLog($"FindDeliveryDestination: Selected destination {owner.Id}.");
                    return owner;
                }

                DebugLog($"FindDeliveryDestination: Destination {owner.Id} has no available slots.");
            }

            DebugLog($"FindDeliveryDestination: No valid destination found for promise {promise}.");
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
