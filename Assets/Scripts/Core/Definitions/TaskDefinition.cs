using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Base class for all task definitions.
    /// Tasks are ScriptableObjects authored in the editor.
    ///
    /// Responsibilities:
    /// - Specify task type and metadata
    /// - Define required workstation for execution
    /// - Set execution time for task completion
    ///
    /// Design:
    /// - Immutable after creation (modifications should create new assets)
    /// - Tasks decide WHAT work occurs; stations decide WHAT can sit there
    /// - Use derived classes for specific task behaviors:
    ///   - ProductionTaskDefinition: generates items from nothing
    ///   - RefinementTaskDefinition: transforms items using methods
    ///   - DeliveryTaskDefinition: moves items based on ItemPromise
    ///   - ConsumptionTaskDefinition: autonomous self-care activities
    /// </summary>
    public abstract class TaskDefinition : ScriptableObject
    {
        [Header("Task Identity")]
        [Tooltip("Category of task (Production, Refinement, Carrier, SelfCare).")]
        public TaskType Type;

        [Tooltip("Human-readable name for editor UI.")]
        public string DisplayName;

        [Tooltip("Description for debugging and UI.")]
        public string Description;

        [Tooltip("If false, task does not appear in the player task list.")]
        public bool ShowInList = true;

        [Header("Execution")]
        [Tooltip("Time in seconds to complete one iteration.")]
        public float Duration = 1f;

        [Tooltip("Workstation type required. Null for Carrier/Delivery tasks.")]
        public WorkstationType RequiredWorkstation;
    }
}
