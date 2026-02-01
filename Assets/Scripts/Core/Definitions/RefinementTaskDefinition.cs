using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Refinement task: transforms input items using a method.
    /// Examples: Cooking, Extracting, Smelting.
    /// </summary>
    [CreateAssetMenu(fileName = "RefinementTask", menuName = "Faces of Labor/Tasks/Refinement Task")]
    public class RefinementTaskDefinition : TaskDefinition
    {
        [Header("Refinement Method")]
        [Tooltip("Method applied to transform the input item.")]
        public RefinementMethod RefinementAction;

        public override RealItem ProcessItem(RealItem input)
        {
            ProcessingState newState = RefinementTable.Refine(input.State, RefinementAction);
            return new RealItem
            {
                BaseType = input.BaseType,
                State = newState
            };
        }
    }
}
