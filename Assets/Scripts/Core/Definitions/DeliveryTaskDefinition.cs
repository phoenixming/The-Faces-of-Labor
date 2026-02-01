using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Delivery task: moves items based on ItemPromise.
    /// Does not require a workstation.
    /// </summary>
    [CreateAssetMenu(fileName = "DeliveryTask", menuName = "Faces of Labor/Tasks/Delivery Task")]
    public class DeliveryTaskDefinition : TaskDefinition
    {
        [Header("Delivery Contract")]
        [Tooltip("ItemPromise this delivery fulfills.")]
        public ItemPromise DeliversPromise;

        [Tooltip("Desired target type for delivery.")]
        public WorkstationType TargetType;

        public override RealItem ProcessItem(RealItem input)
        {
            return input;
        }
    }
}
