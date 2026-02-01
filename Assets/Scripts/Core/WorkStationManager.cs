using UnityEngine;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Manages workstation placement and definitions.
    /// Handles keyboard input to place workstations at cursor grid position.
    /// </summary>
    public class WorkStationManager : MonoBehaviour
    {
        public static WorkStationManager Instance { get; private set; }

        [Header("Workstation Registry")]
        [Tooltip("Central registry containing all workstation presets.")]
        public WorkStationRegistry Registry;

        [Header("Placement Settings")]
        [Tooltip("Default prefab to use if registry lookup fails.")]
        public GameObject DefaultWorkStationPrefab;

        [Header("Quick Placement Keys")]
        [Tooltip("Workstation type to place when pressing J key.")]
        public WorkstationType StationAKey = WorkstationType.Farm;

        [Tooltip("Workstation type to place when pressing K key.")]
        public WorkstationType StationBKey = WorkstationType.Stove;

        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeRegistry();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("WorkStationManager: No main camera found!");
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void InitializeRegistry()
        {
            if (Registry != null)
            {
                Registry.Initialize();
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                PlaceWorkStation(StationAKey);
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                PlaceWorkStation(StationBKey);
            }
        }

        private void PlaceWorkStation(WorkstationType type)
        {
            if (Registry == null)
            {
                Debug.LogWarning("WorkStationManager: Cannot place station - Registry is null!");
                return;
            }

            WorkStationPreset preset = Registry.GetPreset(type);
            if (preset == null)
            {
                Debug.LogWarning($"WorkStationManager: No preset found for workstation type {type}!");
                return;
            }

            Vector2Int gridPosition = GetCursorGridPosition();

            if (gridPosition == Vector2Int.zero && GridSystem.Instance == null)
            {
                Debug.LogWarning("WorkStationManager: GridSystem not available!");
                return;
            }

            if (!CanPlaceAt(gridPosition))
            {
                Debug.Log($"WorkStationManager: Cannot place {preset.DisplayName} at {gridPosition} - cell occupied!");
                return;
            }

            CreateWorkStation(gridPosition, preset);
        }

        private Vector2Int GetCursorGridPosition()
        {
            if (mainCamera == null || GridSystem.Instance == null)
                return Vector2Int.zero;

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return GridSystem.Instance.WorldToGrid(hit.point);
            }

            return Vector2Int.zero;
        }

        private bool CanPlaceAt(Vector2Int position)
        {
            if (GridSystem.Instance == null)
                return false;

            return GridSystem.Instance.CanPlaceInfrastructure(position, InfrastructureType.Workstation);
        }

        private void CreateWorkStation(Vector2Int gridPosition, WorkStationPreset preset)
        {
            GameObject prefab = preset.Prefab ?? DefaultWorkStationPrefab;

            if (prefab == null)
            {
                Debug.LogError($"WorkStationManager: No prefab available for {preset.DisplayName}!");
                return;
            }

            Vector3 worldPosition = GridSystem.Instance.GetCellCenterWorld(gridPosition);
            GameObject stationObject = Instantiate(prefab, worldPosition, Quaternion.identity);

            WorkStation workStation = stationObject.GetComponent<WorkStation>();
            if (workStation == null)
            {
                workStation = stationObject.AddComponent<WorkStation>();
            }

            workStation.SetDefinition(preset.Definition);
            workStation.GridPosition = gridPosition;

            Debug.Log($"WorkStationManager: Placed {preset.DisplayName} at {gridPosition}");
        }
    }
}
