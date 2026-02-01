using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Input Actions")]
        [Tooltip("Input action for placing Station A.")]
        public InputAction TestPlaceAction1;

        [Tooltip("Input action for placing Station B.")]
        public InputAction TestPlaceAction2;

        [Tooltip("Input action for toggling input enable/disable.")]
        public InputAction ToggleInputAction;

        [Header("Input Settings")]
        [Tooltip("Enable or disable workstation placement input.")]
        public bool InputEnabled = true;

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

        private void OnEnable()
        {
            if (TestPlaceAction1 != null)
            {
                TestPlaceAction1.Enable();
                TestPlaceAction1.performed += OnTestPlaceAction1;
            }

            if (TestPlaceAction2 != null)
            {
                TestPlaceAction2.Enable();
                TestPlaceAction2.performed += OnTestPlaceAction2;
            }

            if (ToggleInputAction != null)
            {
                ToggleInputAction.Enable();
                ToggleInputAction.performed += OnToggleInputAction;
            }
        }

        private void OnDisable()
        {
            if (TestPlaceAction1 != null)
            {
                TestPlaceAction1.performed -= OnTestPlaceAction1;
                TestPlaceAction1.Disable();
            }

            if (TestPlaceAction2 != null)
            {
                TestPlaceAction2.performed -= OnTestPlaceAction2;
                TestPlaceAction2.Disable();
            }

            if (ToggleInputAction != null)
            {
                ToggleInputAction.performed -= OnToggleInputAction;
                ToggleInputAction.Disable();
            }
        }

        private void OnTestPlaceAction1(InputAction.CallbackContext context)
        {
            if (InputEnabled)
            {
                PlaceWorkStation(StationAKey);
            }
        }

        private void OnTestPlaceAction2(InputAction.CallbackContext context)
        {
            if (InputEnabled)
            {
                PlaceWorkStation(StationBKey);
            }
        }

        private void OnToggleInputAction(InputAction.CallbackContext context)
        {
            InputEnabled = !InputEnabled;
        }

        private void OnGUI()
        {
            if (InputEnabled && mainCamera != null && GridSystem.Instance != null)
            {
                Vector2Int gridPosition = GetCursorGridPosition();
                if (gridPosition != Vector2Int.zero)
                {
                    // Get world position of the grid cell center
                    Vector3 worldPosition = GridSystem.Instance.GetCellCenterWorld(gridPosition);
                    
                    // Convert world position to screen position
                    Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
                    
                    // Get cell size
                    float cellSize = GridSystem.Instance.CellSize;
                    
                    // Calculate screen coordinates for the grid cell
                    float halfSize = cellSize * 0.5f;
                    Vector3 topLeft = mainCamera.WorldToScreenPoint(worldPosition + new Vector3(-halfSize, 0, halfSize));
                    Vector3 bottomRight = mainCamera.WorldToScreenPoint(worldPosition + new Vector3(halfSize, 0, -halfSize));
                    
                    // Convert screen coordinates to GUI coordinates (y is inverted)
                    topLeft.y = Screen.height - topLeft.y;
                    bottomRight.y = Screen.height - bottomRight.y;
                    
                    // Draw red rectangle
                    GUI.color = Color.red;
                    GUI.DrawTexture(new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y), 
                                   Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        private void InitializeRegistry()
        {
            if (Registry != null)
            {
                Registry.Initialize();
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

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0f));

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
