using System;
using System.Collections.Generic;
using UnityEngine;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Manages the authoritative game grid for spatial reasoning.
    ///
    /// Responsibilities:
    /// - Track walls (blocking cells) for pathfinding
    /// - Track infrastructure placement for construction/occupancy
    /// - Provide spatial queries (validity, walkability, entity lookup)
    ///
    /// Design Notes:
    /// - Uses Unity's Grid component for coordinate conversion
    /// - Grid logic is authoritative; world positions are derived for visuals
    /// - NPCs do not occupy grid cells (no collision between NPCs)
    /// - Cells not in any dictionary are implicitly walkable and empty
    /// - 4-way orthogonal movement only (no diagonals at logic level)
    /// - Two data structures:
    ///   - walls: HashSet for O(1) pathfinding lookups
    ///   - infrastructure: Dictionary for construction/placement queries
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        private Grid grid;

        /// <summary>
        /// O(1) wall lookup for pathfinding.
        /// A cell is walkable if it is NOT in this set.
        /// </summary>
        private HashSet<Vector2Int> walls;

        /// <summary>
        /// Infrastructure placement for construction/occupancy.
        /// Includes all placed infrastructure (workstations, stations, etc.).
        /// </summary>
        private Dictionary<Vector2Int, InfrastructureType> infrastructureTypes;
        private Dictionary<Vector2Int, MonoBehaviour> infrastructureEntities;

        public float CellSize => grid != null ? grid.cellSize.x : 1f;

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            grid = GetComponent<Grid>();
            InitializeGrid();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes grid data structures.
        /// No explicit bounds - grid extends infinitely in all directions.
        /// </summary>
        private void InitializeGrid()
        {
            walls = new HashSet<Vector2Int>();
            infrastructureTypes = new Dictionary<Vector2Int, InfrastructureType>();
            infrastructureEntities = new Dictionary<Vector2Int, MonoBehaviour>();
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts world position to grid coordinate.
        /// Uses Grid component's WorldToCell for accurate conversion.
        /// </summary>
        /// <param name="worldPosition">World position to convert.</param>
        /// <returns>Grid coordinate.</returns>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3Int cell = grid.WorldToCell(worldPosition);
            return new Vector2Int(cell.x, cell.y);
        }

        /// <summary>
        /// Gets the world position at the center of a cell.
        /// </summary>
        public Vector3 GetCellCenterWorld(Vector2Int coordinate)
        {
            return grid.GetCellCenterWorld(new Vector3Int(coordinate.x, coordinate.y, 0));
        }

        #endregion

        #region Walkability

        /// <summary>
        /// Checks if a grid cell is walkable for pathfinding.
        /// A cell is walkable if it is NOT in the walls set.
        /// </summary>
        /// <param name="coordinate">Grid coordinate to check.</param>
        /// <returns>True if the cell can be traversed.</returns>
        public bool IsWalkable(Vector2Int coordinate)
        {
            return !walls.Contains(coordinate);
        }

        /// <summary>
        /// Adds a wall at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">Grid coordinate.</param>
        public void AddWall(Vector2Int coordinate)
        {
            if (!walls.Contains(coordinate))
            {
                walls.Add(coordinate);
                OnWalkabilityChanged?.Invoke(coordinate);
            }
        }

        /// <summary>
        /// Removes a wall at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">Grid coordinate.</param>
        public void RemoveWall(Vector2Int coordinate)
        {
            if (walls.Contains(coordinate))
            {
                walls.Remove(coordinate);
                OnWalkabilityChanged?.Invoke(coordinate);
            }
        }

        /// <summary>
        /// Gets the four orthogonal neighbors of a coordinate.
        /// Diagonal movement is not supported at the logic level.
        /// </summary>
        /// <param name="coordinate">Center coordinate.</param>
        /// <returns>Array of neighbor coordinates.</returns>
        public Vector2Int[] GetNeighbors(Vector2Int coordinate)
        {
            return new Vector2Int[]
            {
                new Vector2Int(coordinate.x + 1, coordinate.y),
                new Vector2Int(coordinate.x - 1, coordinate.y),
                new Vector2Int(coordinate.x, coordinate.y + 1),
                new Vector2Int(coordinate.x, coordinate.y - 1)
            };
        }

        /// <summary>
        /// Calculates Manhattan distance between two grid coordinates.
        /// Used for rough distance estimates in pathfinding and job selection.
        /// </summary>
        public int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        #endregion

        #region Infrastructure (Placeholder)

        /// <summary>
        /// Checks if infrastructure can be placed at the coordinate.
        /// </summary>
        public bool CanPlaceInfrastructure(Vector2Int coordinate, InfrastructureType type)
        {
            if (infrastructureTypes.ContainsKey(coordinate))
                return false;
            return true;
        }

        /// <summary>
        /// Places infrastructure at the specified coordinate.
        /// </summary>
        public void PlaceInfrastructure(Vector2Int coordinate, InfrastructureType type, MonoBehaviour entity)
        {
            infrastructureTypes[coordinate] = type;
            infrastructureEntities[coordinate] = entity;
        }

        /// <summary>
        /// Removes infrastructure at the specified coordinate.
        /// </summary>
        public void RemoveInfrastructure(Vector2Int coordinate)
        {
            infrastructureTypes.Remove(coordinate);
            infrastructureEntities.Remove(coordinate);
        }

        /// <summary>
        /// Gets the infrastructure type at a coordinate.
        /// Returns None if no infrastructure exists there.
        /// </summary>
        public InfrastructureType GetInfrastructureType(Vector2Int coordinate)
        {
            return infrastructureTypes.TryGetValue(coordinate, out InfrastructureType type) ? type : InfrastructureType.None;
        }

        /// <summary>
        /// Gets the infrastructure entity at a coordinate.
        /// </summary>
        public T GetInfrastructureEntity<T>(Vector2Int coordinate) where T : MonoBehaviour
        {
            if (infrastructureEntities.TryGetValue(coordinate, out MonoBehaviour entity))
            {
                return entity as T;
            }
            return null;
        }

        /// <summary>
        /// Checks if infrastructure exists at the coordinate.
        /// </summary>
        public bool HasInfrastructure(Vector2Int coordinate)
        {
            return infrastructureTypes.ContainsKey(coordinate);
        }

        #endregion

        #region Grid Enumeration

        /// <summary>
        /// Enumerates all wall coordinates.
        /// </summary>
        public IEnumerable<Vector2Int> GetWallCoordinates()
        {
            foreach (var coord in walls)
            {
                yield return coord;
            }
        }

        /// <summary>
        /// Enumerates all infrastructure coordinates.
        /// </summary>
        public IEnumerable<Vector2Int> GetInfrastructureCoordinates()
        {
            foreach (var kvp in infrastructureTypes.Keys)
            {
                yield return kvp;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a cell's walkability changes.
        /// Used by PathfindingSystem for cache invalidation.
        /// </summary>
        public event Action<Vector2Int> OnWalkabilityChanged;

        #endregion
    }
}
