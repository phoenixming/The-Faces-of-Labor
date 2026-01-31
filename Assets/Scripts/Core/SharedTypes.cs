using UnityEngine;

namespace FacesOfLabor.Core
{
    public enum JobType
    {
        None,
        Farmer,
        Chef,
        Carrier
    }

    public enum JobState
    {
        Pending,
        Ready,
        Claimed,
        Executing
    }

    public enum WorkstationType
    {
        None,
        Farm,
        Stove,
        DiningHall
    }

    public enum ItemType
    {
        None,
        Crops,
        Meals
    }

    public enum InfrastructureType
    {
        None,
        Wall,
        MaskStation,
        Workstation
    }
}
