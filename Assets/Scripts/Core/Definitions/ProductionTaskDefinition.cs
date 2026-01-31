using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Production task: generates raw items from nothing.
    /// Examples: Farming, Mining, Lumberjacking.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductionTask", menuName = "Faces of Labor/Tasks/Production Task")]
    public class ProductionTaskDefinition : TaskDefinition
    {
        [Header("Production Output")]
        [Tooltip("Base item type produced by this task.")]
        public BaseItemType ProducesBaseItem;
    }
}
