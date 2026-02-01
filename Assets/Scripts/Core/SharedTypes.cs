using System;

namespace FacesOfLabor.Core
{
    #region Base Item Type

    /// <summary>
    /// Immutable material category of an item.
    /// Does not imply usability or intent.
    /// Never used by NPC logic or routing directly.
    /// </summary>
    public enum BaseItemType
    {
        None,
        Crop,
        Ore,
        Wood
    }

    #endregion

    #region Refinement Method

    /// <summary>
    /// Actions applied to items that transform their processing state.
    /// </summary>
    public enum RefinementMethod
    {
        Cook,
        Extract,
        Smelt
    }

    #endregion

    #region Processing State

    /// <summary>
    /// What has been done to an item.
    /// Exactly one state per item. "Ruined" is a sink state.
    /// </summary>
    public enum ProcessingState
    {
        Raw,
        Cooked,
        Burnt,
        Extracted,
        Smelted,
        Ruined
    }

    #endregion

    #region Item True Type

    /// <summary>
    /// Combination of BaseItemType and ProcessingState.
    /// Represents the complete set of possible item types.
    /// </summary>
    public struct RealItem
    {
        public BaseItemType BaseType;
        public ProcessingState State;
    }

    #endregion

    #region Refinement Table (Authoritative Transition Matrix)

    /// <summary>
    /// Single source of truth for all refinement transitions.
    /// No conditionals, no special cases, no illegal transitions.
    /// </summary>
    public static class RefinementTable
    {
        private static readonly ProcessingState[,] Table = {
            // Cook              Extract             Smelt
            { ProcessingState.Cooked,    ProcessingState.Extracted,    ProcessingState.Smelted },  // Raw
            { ProcessingState.Burnt,     ProcessingState.Ruined,       ProcessingState.Ruined },    // Cooked
            { ProcessingState.Burnt,     ProcessingState.Ruined,       ProcessingState.Burnt },     // Burnt
            { ProcessingState.Ruined,    ProcessingState.Ruined,       ProcessingState.Ruined },    // Extracted
            { ProcessingState.Ruined,    ProcessingState.Ruined,       ProcessingState.Smelted },   // Smelted
            { ProcessingState.Ruined,    ProcessingState.Ruined,       ProcessingState.Ruined }     // Ruined
        };

        public static ProcessingState Refine(ProcessingState current, RefinementMethod method)
        {
            return Table[(int)current, (int)method];
        }
    }

    #endregion

    #region Item Promise (Delivery Contract)

    /// <summary>
    /// Delivery contract, routing vocabulary, and intent surface.
    /// The only item identity NPCs can see.
    /// Represents expected legal combinations of BaseItemType and ProcessingState.
    /// </summary>
    public enum ItemPromise
    {
        None,
        Crop,      // Raw crop
        Meal,      // Cooked crop
        Medicine,  // Extracted crop
        Metal,     // Smelted ore
        Ore,       // Raw ore
        Wood       // Raw wood
    }

    #endregion

    #region Task Type

    /// <summary>
    /// Categories of work that NPCs can perform.
    /// </summary>
    public enum TaskType
    {
        /// <summary>No task or unassigned.</summary>
        None,

        /// <summary>Production tasks (farming, mining, lumberjacking).</summary>
        Production,

        /// <summary>Refinement tasks (cooking, extracting, smelting).</summary>
        Refinement,

        /// <summary>Delivery tasks (pickup from one location, deliver to another).</summary>
        Delivery,

        /// <summary>Self-care tasks (eating, medicine) - autonomous.</summary>
        Consumption
    }

    #endregion

    #region Task State

    /// <summary>
    /// Lifecycle states for a TaskInstance.
    /// </summary>
    public enum TaskState
    {
        /// <summary>Task exists but inputs are not fully available.</summary>
        Pending,

        /// <summary>All inputs and output slots are reserved.</summary>
        Ready,

        /// <summary>An NPC has committed to executing the task.</summary>
        Claimed,

        /// <summary>NPC is performing work.</summary>
        Executing
    }

    #endregion

    #region Workstation Type

    /// <summary>
    /// Workstation types available in the game.
    /// </summary>
    public enum WorkstationType
    {
        None,
        Farm,
        Stove,
        ExtractionTable,
        Smelter,
        DiningHall,
        MaskStation,
    }

    #endregion

    #region Infrastructure Type

    /// <summary>
    /// Types of infrastructure that can be placed on the grid.
    /// </summary>
    public enum InfrastructureType
    {
        None,
        Wall,
        MaskStation,
        Workstation
    }

    #endregion
}
