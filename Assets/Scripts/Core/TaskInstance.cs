using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Runtime instance of an executable task.
    ///
    /// Responsibilities:
    /// - Carry all runtime state for scheduling
    /// - Manage task lifecycle (Pending -> Ready -> Claimed -> Executing)
    /// - Track execution progress and time remaining
    /// - Handle respawning for continuous tasks
    ///
    /// Design:
    /// - Created from TaskDefinition template
    /// - Only TaskInstances participate in scheduling
    /// - All state is instance-local (no hidden scheduling state)
    /// </summary>
    public class TaskInstance
    {
        /// <summary>
        /// Unique identifier for this instance.
        /// Used for tracking reservations and debugging.
        /// </summary>
        public readonly uint InstanceId;

        /// <summary>
        /// Template this instance was created from.
        /// Immutable reference to static task definition.
        /// </summary>
        public readonly TaskDefinition Definition;

        /// <summary>
        /// Current lifecycle state.
        /// </summary>
        public TaskState State { get; private set; }

        /// <summary>
        /// Number of times this task should respawn after completion.
        /// 0 = one-shot task, UINT_MAX = effectively infinite.
        /// </summary>
        public uint RespawnCount;

        /// <summary>
        /// Remaining execution time in seconds.
        /// </summary>
        public float TimeRemaining;

        /// <summary>
        /// Handles to all reserved inputs and output buffer slots.
        /// </summary>
        public List<object> Reservations { get; private set; }

        /// <summary>
        /// Task type shortcut (inherited from definition).
        /// </summary>
        public TaskType Type => Definition.Type;

        /// <summary>
        /// Execution progress as a ratio (0.0 to 1.0).
        /// Returns -1 if not currently executing.
        /// </summary>
        public float Progress
        {
            get
            {
                if (State != TaskState.Executing)
                    return -1f;

                float totalTime = Definition.Duration;
                if (totalTime <= 0f)
                    return 1f;

                return 1f - (TimeRemaining / totalTime);
            }
        }

        private static uint nextInstanceId = 0;

        /// <summary>
        /// Creates a new task instance from a definition.
        /// </summary>
        /// <param name="definition">Template to instantiate.</param>
        /// <param name="respawnCount">Number of times to respawn after completion (0 = one-shot).</param>
        public TaskInstance(TaskDefinition definition, uint respawnCount = 0)
        {
            InstanceId = nextInstanceId++;
            Definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            State = TaskState.Pending;
            RespawnCount = respawnCount;
            TimeRemaining = definition.Duration;
            Reservations = new List<object>();
        }

        /// <summary>
        /// Attempts to transition to a new state.
        /// Returns true if transition was valid and successful.
        /// </summary>
        public bool SetState(TaskState newState)
        {
            if (!IsValidTransition(newState))
                return false;

            State = newState;
            return true;
        }

        /// <summary>
        /// Checks if a state transition is valid.
        /// </summary>
        private bool IsValidTransition(TaskState newState)
        {
            return (State, newState) switch
            {
                (TaskState.Pending, TaskState.Ready) => true,
                (TaskState.Ready, TaskState.Claimed) => true,
                (TaskState.Claimed, TaskState.Executing) => true,
                (TaskState.Executing, TaskState.Pending) => true, // Interrupted
                _ => false
            };
        }

        /// <summary>
        /// Updates execution progress by deltaTime.
        /// Should only be called when state is Executing.
        /// </summary>
        public void UpdateExecution(float deltaTime)
        {
            if (State != TaskState.Executing)
                return;

            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
            }
        }

        /// <summary>
        /// Checks if execution has completed.
        /// </summary>
        public bool IsComplete => State == TaskState.Executing && TimeRemaining <= 0f;

        /// <summary>
        /// Creates a new instance for respawning with decremented counter.
        /// Returns null if respawn count is zero.
        /// </summary>
        public TaskInstance CreateRespawnedInstance()
        {
            if (RespawnCount == 0)
                return null;

            return new TaskInstance(Definition, RespawnCount - 1);
        }

        /// <summary>
        /// Clears all reservations.
        /// Should be called when task completes or is cancelled.
        /// </summary>
        public void ClearReservations()
        {
            Reservations?.Clear();
        }

        /// <summary>
        /// Adds a reservation handle.
        /// </summary>
        public void AddReservation(object handle)
        {
            Reservations ??= new List<object>();
            Reservations.Add(handle);
        }
    }
}
