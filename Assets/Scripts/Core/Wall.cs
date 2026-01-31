using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Represents a wall that blocks movement and pathfinding.
    /// Inherits from Infrastructure for event-based registration.
    ///
    /// Design:
    /// - Walls are simple grid-blocking entities
    /// - Registered with GridSystem via Infrastructure events
    /// - No additional data beyond base Infrastructure
    /// </summary>
    public class Wall : Infrastructure {}
}
