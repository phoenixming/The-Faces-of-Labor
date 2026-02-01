using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Base class for all workstations in the colony.
    /// Acts as a thin container referencing a WorkStationDefinition.
    ///
    /// Responsibilities:
    /// - Hold reference to WorkStationDefinition for physical constraints
    /// - Manage input buffer with reservation system for slots and items
    /// - Execute tasks on this workstation
    ///
    /// Design:
    /// - Scene objects are thin containers around ScriptableObject definitions
    /// - Workstations never decide WHAT happens to items — only WHETHER items may be present
    /// - See WorkStationDefinition for capacity and acceptance rules
    /// - Reservation system ensures atomicity: reserve before add/consume
    /// </summary>
    public class WorkStation : Infrastructure
    {
        [Header("Configuration")]
        [Tooltip("Workstation definition specifying physical constraints.")]
        [SerializeField] private WorkStationDefinition definition;

        [Header("Runtime State")]
        [Tooltip("Grid cell this workstation occupies.")]
        public Vector2Int GridPosition;

        public WorkStationDefinition Definition => definition;
        public WorkstationType Type => definition?.Type ?? WorkstationType.None;
        public override int Capacity => definition?.InputBufferSize ?? 0;
        public override bool AcceptsInput => Capacity > 0;
        public override ItemPromise AcceptsPromise => definition?.AcceptsInput ?? ItemPromise.None;

        public void SetDefinition(WorkStationDefinition newDefinition)
        {
            definition = newDefinition;
        }

        public void ExecuteTask(TaskDefinition taskDefinition, Action<RealItem> onComplete)
        {
            if (taskDefinition.Type == TaskType.Delivery)
            {
                Debug.LogError($"Workstation {name} cannot execute Delivery tasks. Use delivery flow instead.");
                onComplete?.Invoke(default);
                return;
            }

            RealItem inputItem = default;

            if (taskDefinition.Type == TaskType.Refinement || taskDefinition.Type == TaskType.Consumption)
            {
                // No need to check for available items, as we already checked during reservation
                inputItem = ConsumeItem();
            }

            StartCoroutine(ExecuteTaskCoroutine(taskDefinition, inputItem, onComplete));
        }

        private System.Collections.IEnumerator ExecuteTaskCoroutine(TaskDefinition taskDefinition, RealItem inputItem, Action<RealItem> onComplete)
        {
            yield return new WaitForSeconds(taskDefinition.Duration);

            RealItem outputItem = taskDefinition.ProcessItem(inputItem);
            onComplete?.Invoke(outputItem);
        }
    }
}
