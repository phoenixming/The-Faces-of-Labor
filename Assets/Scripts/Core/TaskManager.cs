using System.Collections.Generic;
using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Central manager for task scheduling and execution.
    ///
    /// Responsibilities:
    /// - Maintain global task list (Pending and Ready tasks)
    /// - Perform readiness checks with atomic reservation
    /// - Match NPCs to available tasks
    /// - Handle task completion and respawning
    ///
    /// Design:
    /// - Singleton pattern (one scheduler per scene)
    /// - Tasks are the only schedulable entities
    /// - Atomic reservation prevents race conditions
    /// - NPCs select nearest Ready task (no affinity)
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        /// <summary>Total number of tasks in the system.</summary>
        public int TotalTaskCount => 0;

        /// <summary>Tasks waiting for inputs to become available.</summary>
        public int PendingTaskCount => 0;

        /// <summary>Tasks with all inputs reserved and ready for claiming.</summary>
        public int ReadyTaskCount => 0;

        /// <summary>Tasks currently being executed by NPCs.</summary>
        public int ActiveTaskCount => 0;

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #endregion

        #region Task Lifecycle

        /// <summary>
        /// Creates a new task instance from a definition.
        /// </summary>
        public TaskInstance CreateTask(TaskDefinition definition, uint respawnCount = 0)
        {
            return null;
        }

        /// <summary>
        /// Destroys a task instance.
        /// Should only be called after completion or cancellation.
        /// </summary>
        public void DestroyTask(TaskInstance task) { }

        #endregion

        #region Task Queries

        /// <summary>
        /// Returns all Ready tasks available for claiming.
        /// </summary>
        public List<TaskInstance> GetReadyTasks()
        {
            return null;
        }

        /// <summary>
        /// Finds the nearest Ready task to a given location.
        /// Used by NPCs to select which task to claim.
        /// </summary>
        public TaskInstance GetNearestReadyTask(Vector2Int location)
        {
            return null;
        }

        /// <summary>
        /// Finds the nearest Ready task of a specific type.
        /// </summary>
        public TaskInstance GetNearestReadyTaskOfType(Vector2Int location, TaskType type)
        {
            return null;
        }

        #endregion

        #region NPC Interaction

        /// <summary>
        /// Attempts to claim a task for an NPC.
        /// </summary>
        public bool ClaimTask(TaskInstance task)
        {
            return false;
        }

        /// <summary>
        /// Begins execution of a claimed task.
        /// </summary>
        public bool StartTaskExecution(TaskInstance task)
        {
            return false;
        }

        /// <summary>
        /// Called when an NPC completes a task.
        /// Handles consumption, production, and respawning.
        /// </summary>
        public void OnTaskCompleted(TaskInstance task) { }

        /// <summary>
        /// Called when an NPC is interrupted during task execution.
        /// Returns task to Ready state for re-claiming.
        /// </summary>
        public void OnTaskInterrupted(TaskInstance task) { }

        #endregion

        #region System Integration

        /// <summary>
        /// Periodically checks Pending tasks for readiness.
        /// Called by the main update loop.
        /// </summary>
        private void Update()
        {
        }

        #endregion
    }
}
