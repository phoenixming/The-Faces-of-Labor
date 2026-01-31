using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Defines physical constraints for a workstation.
    /// Represents what a place can hold and accept — not behavior.
    ///
    /// Responsibilities:
    /// - Specify accepted ItemPromise for input
    /// - Define input buffer capacity
    ///
    /// Design:
    /// - Created as ScriptableObjects for easy asset-based configuration
    /// - Immutable after creation
    /// - Workstations never decide WHAT happens to items — only WHETHER items may be present
    /// - Tasks decide what work occurs; stations decide what can sit there
    /// - InputBufferSize == 0 means station does not accept input
    /// </summary>
    [CreateAssetMenu(fileName = "WorkStationDefinition", menuName = "Faces of Labor/Workstation Definition")]
    public class WorkStationDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Workstation type this definition corresponds to.")]
        public WorkstationType Type;

        [Tooltip("Human-readable name for editor UI.")]
        public string DisplayName;

        [Header("Input")]
        [Tooltip("ItemPromise this station accepts as input. Null if station does not accept input.")]
        public ItemPromise AcceptsInput;

        [Tooltip("Maximum items that can be stored. 0 means station does not accept input.")]
        public int InputBufferSize = 1;
    }
}
