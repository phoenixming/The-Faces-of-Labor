using UnityEngine;

namespace FacesOfLabor.Core
{
    [CreateAssetMenu(fileName = "JobDefinition", menuName = "Faces of Labor/Job Definition")]
    public class JobDefinition : ScriptableObject
    {
        public JobType Type;
        public string Description;
        public WorkstationType WorkstationType;
        public float BaseExecutionTime;
        public ItemType RequiredInput;
        public int InputQuantity;
        public ItemType ProducedOutput;
        public int OutputQuantity;
        public bool IsDeliveryJob;
    }
}
