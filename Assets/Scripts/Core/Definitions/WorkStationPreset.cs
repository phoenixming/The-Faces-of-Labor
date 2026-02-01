using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Pairs a WorkStationDefinition with its corresponding prefab.
    /// Used to define the visual representation of each workstation type.
    /// </summary>
    [CreateAssetMenu(fileName = "WorkStationPreset", menuName = "Faces of Labor/Workstation Preset")]
    public class WorkStationPreset : ScriptableObject
    {
        [Header("Definition")]
        [Tooltip("The workstation definition containing type, capacity, and input rules.")]
        public WorkStationDefinition Definition;

        [Header("Visuals")]
        [Tooltip("Prefab to instantiate for this workstation type.")]
        public GameObject Prefab;

        public WorkstationType Type => Definition != null ? Definition.Type : WorkstationType.None;
        public string DisplayName => Definition != null ? Definition.DisplayName : "Unnamed";
    }
}
