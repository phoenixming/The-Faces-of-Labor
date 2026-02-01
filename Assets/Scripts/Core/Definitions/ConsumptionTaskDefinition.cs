using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Consumption task: autonomous self-care activities.
    /// Examples: Eating, Taking Medicine.
    /// These tasks are triggered by NPC needs, not player scheduling.
    /// </summary>
    [CreateAssetMenu(fileName = "ConsumptionTask", menuName = "Faces of Labor/Tasks/Consumption Task")]
    public class ConsumptionTaskDefinition : TaskDefinition
    {
        // We don't need an `ItemPromise` for consumption here. That is implied
        // by the required work station

        public override RealItem ProcessItem(RealItem input)
        {
            return new RealItem
            {
                BaseType = BaseItemType.None,
                State = ProcessingState.Raw
            };
        }
    }
}
