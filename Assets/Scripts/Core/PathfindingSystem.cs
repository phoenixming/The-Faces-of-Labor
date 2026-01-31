using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Lazy, incremental flow-field pathfinding system.
    ///
    /// Responsibilities:
    /// - Own FlowFields for each destination
    /// - Route agent queries to fields
    /// - Expand fields lazily on demand
    /// - Handle invalidation on map changes
    ///
    /// Design:
    /// - Flow fields share work across agents
    /// - Resume from stored frontiers (never redo work)
    /// - Sparse storage (only reachable cells stored)
    /// - Invalidate on wall changes within boundary
    /// </summary>
    [RequireComponent(typeof(GridSystem))]
    public class PathfindingSystem : MonoBehaviour
    {
        public static PathfindingSystem Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Expansion buffer for boundary expansion.")]
        [Range(10, 100)]
        [SerializeField] private int expansionBuffer = 20;

        [Header("Debug Visualization")]
        [Tooltip("Draw flow field for this destination.")]
        [SerializeField] private Vector2Int debugDestination;

        [Tooltip("Ensure the flow field covers this point before drawing.")]
        [SerializeField] private Vector2Int debugCoverPoint;

        [Tooltip("Show the boundary of the flow field.")]
        [SerializeField] private bool showBoundary;

        [Tooltip("Show stored frontier cells.")]
        [SerializeField] private bool showStoredFrontier;

        private Dictionary<Vector2Int, FlowField> flowFields;
        private GridSystem gridSystem;

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            gridSystem = GetComponent<GridSystem>();
            flowFields = new Dictionary<Vector2Int, FlowField>();

            if (gridSystem != null)
            {
                gridSystem.OnWalkabilityChanged += OnWalkabilityChanged;
            }
        }

        private void OnDestroy()
        {
            if (gridSystem != null)
            {
                gridSystem.OnWalkabilityChanged -= OnWalkabilityChanged;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the direction an agent should move to reach the destination.
        /// </summary>
        /// <param name="start">Current position in grid coordinates.</param>
        /// <param name="destination">Target position in grid coordinates.</param>
        /// <returns>Direction to move, or Vector2Int.zero if unreachable.</returns>
        public Vector2Int GetDirection(Vector2Int start, Vector2Int destination)
        {
            if (start == destination)
            {
                return Vector2Int.zero;
            }

            if (!gridSystem.IsWalkable(destination))
            {
                return Vector2Int.zero;
            }

            FlowField field = GetOrCreateField(destination);

            if (!IsInsideBoundary(start, field.boundaryRadius, destination))
            {
                ExpandFieldToCover(field, start);
            }

            if (field.flow.TryGetValue(start, out Direction direction))
            {
                return DirectionToVector(direction);
            }

            return Vector2Int.zero;
        }

        /// <summary>
        /// Checks if a destination is reachable from a start position.
        /// </summary>
        public bool IsReachable(Vector2Int start, Vector2Int destination)
        {
            return GetDirection(start, destination) != Vector2Int.zero;
        }

        #endregion

        #region Field Management

        private FlowField GetOrCreateField(Vector2Int destination)
        {
            if (!flowFields.TryGetValue(destination, out FlowField field))
            {
                field = new FlowField
                {
                    destination = destination,
                    flow = new Dictionary<Vector2Int, Direction>(),
                    storedFrontier = new Queue<Vector2Int>(),
                    boundaryRadius = 0
                };

                // The destination cell is the initial seed for BFS expansion.
                // It has no direction (agents at destination don't move).
                field.storedFrontier.Enqueue(destination);

                flowFields[destination] = field;
            }
            return field;
        }

        private bool IsInsideBoundary(Vector2Int cell, int boundaryRadius, Vector2Int destination)
        {
            return ManhattanDistance(cell, destination) <= boundaryRadius;
        }

        private void ExpandFieldToCover(FlowField field, Vector2Int target)
        {
            int requiredRadius = ManhattanDistance(field.destination, target) + expansionBuffer;

            // If requested radius is within current coverage, nothing to do.
            if (requiredRadius <= field.boundaryRadius)
            {
                return;
            }

            field.boundaryRadius = requiredRadius;

            // Move stored frontier to working frontier, filtering out blocked cells.
            // Blocked cells may have become walls since last expansion.
            Queue<Vector2Int> workingFrontier = new Queue<Vector2Int>();
            while (field.storedFrontier.Count > 0)
            {
                Vector2Int cell = field.storedFrontier.Dequeue();
                if (gridSystem.IsWalkable(cell))
                {
                    workingFrontier.Enqueue(cell);
                }
            }

            // If working frontier is empty, all reachable cells have been fully explored.
            // Expanding the radius further won't discover new cells (they're either walls
            // or already processed). We still update boundaryRadius for coverage tracking.
            if (workingFrontier.Count == 0)
            {
                return;
            }

            // BFS expansion: process current frontier level, queue next level.
            // Cells inside boundary get flow directions; cells outside are stored for later.
            while (workingFrontier.Count > 0)
            {
                Vector2Int cell = workingFrontier.Dequeue();

                foreach (var direction in AllDirections)
                {
                    Vector2Int neighbor = cell + direction.Value;

                    // Skip if already processed.
                    if (field.flow.ContainsKey(neighbor))
                        continue;

                    // Skip blocked cells - they don't get flow directions.
                    if (!gridSystem.IsWalkable(neighbor))
                        continue;

                    // The algorithm assumes all cells in the frontier already have flow directions.
                    // Assigning flow direction here ensures correct directionality.
                    field.flow[neighbor] = GetOppositeDirection(direction.Key);
                    // Inside boundary: add to working frontier for next level processing.
                    if (IsInsideBoundary(neighbor, field.boundaryRadius, field.destination))
                    {
                        workingFrontier.Enqueue(neighbor);
                    }
                    // Outside boundary: store for future expansion.
                    else
                    {
                        field.storedFrontier.Enqueue(neighbor);
                    }
                }
            }
        }

        private void OnWalkabilityChanged(Vector2Int cell)
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();

            // Invalidate any field where the wall change could affect pathfinding.
            // A wall at distance D invalidates fields with radius >= D-1.
            foreach (var kvp in flowFields)
            {
                FlowField field = kvp.Value;
                int effectiveRadius = field.boundaryRadius + 1;

                if (IsInsideBoundary(cell, effectiveRadius, field.destination))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var dest in toRemove)
            {
                flowFields.Remove(dest);
            }
        }

        #endregion

        #region Direction Helpers

        private static readonly KeyValuePair<Direction, Vector2Int>[] AllDirections = new[]
        {
            new KeyValuePair<Direction, Vector2Int>(Direction.Up, new Vector2Int(0, 1)),
            new KeyValuePair<Direction, Vector2Int>(Direction.Down, new Vector2Int(0, -1)),
            new KeyValuePair<Direction, Vector2Int>(Direction.Left, new Vector2Int(-1, 0)),
            new KeyValuePair<Direction, Vector2Int>(Direction.Right, new Vector2Int(1, 0))
        };

        private Vector2Int DirectionToVector(Direction direction)
        {
            return direction switch
            {
                Direction.Up => new Vector2Int(0, 1),
                Direction.Down => new Vector2Int(0, -1),
                Direction.Left => new Vector2Int(-1, 0),
                Direction.Right => new Vector2Int(1, 0),
                _ => Vector2Int.zero
            };
        }

        private Direction GetOppositeDirection(KeyValuePair<Direction, Vector2Int> direction)
        {
            return GetOppositeDirection(direction.Key);
        }

        private Direction GetOppositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => Direction.Up
            };
        }

        private int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        #endregion

        #region Nested Types

        private class FlowField
        {
            public Vector2Int destination;
            public Dictionary<Vector2Int, Direction> flow;
            public Queue<Vector2Int> storedFrontier;
            public int boundaryRadius;
        }

        private enum Direction
        {
            Up = 0,
            Down = 1,
            Left = 2,
            Right = 3
        }

        #endregion

        #region Debug Visualization

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (gridSystem == null)
                return;

            // Expand flow field to cover debug cover point, then draw that field.
            // GetDirection call ensures field exists and covers the point.
            GetDirection(debugCoverPoint, debugDestination);
            DrawFlowField(debugDestination);
        }

        private void DrawFlowField(Vector2Int destination)
        {
            if (!flowFields.TryGetValue(destination, out FlowField field))
                return;

            Vector3 destWorld = gridSystem.GetCellCenterWorld(destination);
            float cellSize = gridSystem.CellSize;

            if (showBoundary)
            {
                DrawFlowFieldBoundary(field, destWorld, cellSize);
            }

            // Draw flow directions for each cell in the field using Handles.ArrowHandleCap.
            foreach (var kvp in field.flow)
            {
                Vector3 cellWorld = gridSystem.GetCellCenterWorld(kvp.Key);
                Vector2 direction2D = DirectionToVector(kvp.Value);
                Vector3 direction = new Vector3(direction2D.x, 0, direction2D.y);

                // Position arrow at cell center, pointing toward destination.
                Vector3 arrowPosition = cellWorld + new Vector3(0, 0.1f, 0);
                Quaternion arrowRotation = Quaternion.LookRotation(direction);

                Handles.color = new Color(0f, 0.5f, 1f, 0.8f);
                Handles.ArrowHandleCap(0, arrowPosition, arrowRotation, cellSize * 0.5f, EventType.Repaint);
            }

            // Highlight destination cell.
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(destWorld + new Vector3(0, 0.1f, 0), new Vector3(cellSize, 0.1f, cellSize));

            // Draw stored frontier if enabled.
            if (showStoredFrontier && field.storedFrontier != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                foreach (var cell in field.storedFrontier)
                {
                    Vector3 cellWorld = gridSystem.GetCellCenterWorld(cell);
                    Gizmos.DrawWireCube(cellWorld + new Vector3(0, 0.05f, 0), new Vector3(cellSize, 0.1f, cellSize));
                }
            }
        }

        private void DrawFlowFieldBoundary(FlowField field, Vector3 destWorld, float cellSize)
        {
            // Draw boundary square (Manhattan distance forms a diamond, drawn as 4 line segments).
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 destCenter = destWorld;

            for (int i = 0; i < field.boundaryRadius; i++)
            {
                Vector3 current = destCenter + new Vector3(i * cellSize, 0, (field.boundaryRadius - i) * cellSize);
                Vector3 next = destCenter + new Vector3((i + 1) * cellSize, 0, (field.boundaryRadius - i - 1) * cellSize);
                Gizmos.DrawLine(current, next);
            }

            for (int i = 0; i < field.boundaryRadius; i++)
            {
                Vector3 current = destCenter + new Vector3((field.boundaryRadius - i) * cellSize, 0, -i * cellSize);
                Vector3 next = destCenter + new Vector3((field.boundaryRadius - i - 1) * cellSize, 0, -(i + 1) * cellSize);
                Gizmos.DrawLine(current, next);
            }

            for (int i = 0; i < field.boundaryRadius; i++)
            {
                Vector3 current = destCenter + new Vector3(-i * cellSize, 0, -(field.boundaryRadius - i) * cellSize);
                Vector3 next = destCenter + new Vector3(-(i + 1) * cellSize, 0, -(field.boundaryRadius - i - 1) * cellSize);
                Gizmos.DrawLine(current, next);
            }

            for (int i = 0; i < field.boundaryRadius; i++)
            {
                Vector3 current = destCenter + new Vector3(-(field.boundaryRadius - i) * cellSize, 0, i * cellSize);
                Vector3 next = destCenter + new Vector3(-(field.boundaryRadius - i - 1) * cellSize, 0, (i + 1) * cellSize);
                Gizmos.DrawLine(current, next);
            }
        }
#endif

        #endregion
    }
}
