using UnityEngine;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Central registry for all workstation presets.
    /// Provides easy lookup of workstation definitions and their corresponding prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "WorkStationRegistry", menuName = "Faces of Labor/Workstation Registry")]
    public class WorkStationRegistry : ScriptableObject
    {
        [Tooltip("List of all workstation presets available in the game.")]
        public List<WorkStationPreset> Presets;

        private Dictionary<WorkstationType, WorkStationPreset> presetDictionary;

        public void Initialize()
        {
            presetDictionary = new Dictionary<WorkstationType, WorkStationPreset>();

            if (Presets == null)
                return;

            foreach (var preset in Presets)
            {
                if (preset != null && preset.Definition != null)
                {
                    presetDictionary[preset.Definition.Type] = preset;
                }
            }
        }

        public WorkStationPreset GetPreset(WorkstationType type)
        {
            if (presetDictionary == null)
                Initialize();

            return presetDictionary.TryGetValue(type, out var preset) ? preset : null;
        }

        public WorkStationDefinition GetDefinition(WorkstationType type)
        {
            var preset = GetPreset(type);
            return preset != null ? preset.Definition : null;
        }

        public GameObject GetPrefab(WorkstationType type)
        {
            var preset = GetPreset(type);
            return preset != null ? preset.Prefab : null;
        }

        public bool HasPreset(WorkstationType type)
        {
            if (presetDictionary == null)
                Initialize();

            return presetDictionary.ContainsKey(type);
        }

        public IEnumerable<WorkStationPreset> GetAllPresets()
        {
            return Presets ?? System.Linq.Enumerable.Empty<WorkStationPreset>();
        }
    }
}
