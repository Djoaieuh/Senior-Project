using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Lives on the GameManager prefab (DontDestroyOnLoad).
/// Manages showing/hiding the map and handles travel between locations.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager _instance;

    [Header("Map UI")]
    [Tooltip("The root GameObject of the map UI (starts disabled)")]
    [SerializeField] private GameObject mapRoot;

    [Tooltip("Reference to the MapUI controller inside the map prefab")]
    [SerializeField] private MapUI mapUI;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private FishingActions inputActions;

    public bool IsOpen => mapRoot != null && mapRoot.activeSelf;

    // ============================================
    // LIFECYCLE
    // ============================================

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;

        inputActions = new FishingActions();

        if (mapRoot != null)
            mapRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // NOTE: Replace "Fishing" with whatever you renamed your action map to
        inputActions.PlayerInputs.OpenCloseMap.performed += OnOpenCloseMapPerformed;
        inputActions.PlayerInputs.Enable();
    }

    private void OnDisable()
    {
        inputActions.PlayerInputs.OpenCloseMap.performed -= OnOpenCloseMapPerformed;
        inputActions.PlayerInputs.Disable();
    }

    private void OnOpenCloseMapPerformed(InputAction.CallbackContext ctx)
    {
        Toggle();
    }

    // ============================================
    // OPEN / CLOSE
    // ============================================

    public void OpenMap()
    {
        if (mapRoot == null) return;
        mapRoot.SetActive(true);
        mapUI?.RefreshAll();
        if (showDebugLogs) Debug.Log("[MapManager] Map opened");
    }

    public void CloseMap()
    {
        if (mapRoot == null) return;
        mapRoot.SetActive(false);
        if (showDebugLogs) Debug.Log("[MapManager] Map closed");
    }

    public void Toggle()
    {
        if (IsOpen) CloseMap();
        else        OpenMap();
    }

    // ============================================
    // TRAVEL
    // ============================================

    public void TravelTo(LocationData location)
    {
        if (location == null) return;

        MapData mapData = GameManager._instance.Map;

        if (!location.IsUnlocked(mapData))
        {
            bool unlocked = location.TryUnlock(mapData);
            if (!unlocked)
            {
                Debug.LogWarning($"[MapManager] Cannot travel to {location.locationName} - requirements not met!");
                return;
            }
            if (showDebugLogs) Debug.Log($"[MapManager] Unlocked: {location.locationName}");
        }

        switch (location.locationType)
        {
            case LocationType.FishingSpot:
            case LocationType.Shop:
                TravelToScene(location);
                break;

            case LocationType.Basic:
                Debug.Log($"[MapManager] Basic location '{location.locationName}' - not yet implemented");
                break;
        }
    }

    private void TravelToScene(LocationData location)
    {
        if (string.IsNullOrEmpty(location.sceneName))
        {
            Debug.LogError($"[MapManager] Location '{location.locationName}' has no sceneName set!");
            return;
        }

        if (showDebugLogs) Debug.Log($"[MapManager] Travelling to: {location.locationName} ({location.sceneName})");

        GameManager._instance.SaveGame();
        CloseMap();
        SceneManager.LoadScene(location.sceneName);
    }

    // ============================================
    // EDITOR HELPERS
    // ============================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Open Map")]
    private void DebugOpenMap() => OpenMap();

    [ContextMenu("Debug: Close Map")]
    private void DebugCloseMap() => CloseMap();
#endif
}