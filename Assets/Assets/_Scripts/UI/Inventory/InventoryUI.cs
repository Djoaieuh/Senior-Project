using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Main inventory UI controller
/// AUTO-FINDS: Screens, buttons, cursor manager
/// NAMING CONVENTIONS: Buttons must be named "GearButton", "FishButton", etc.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference inventoryAction;
    
    [Header("Auto-Found References (Can Override in Inspector)")]
    [SerializeField] private CursorManager cursorManager;
    [SerializeField] private GameObject inventoryScreen;
    [SerializeField] private GearScreen gearScreen;
    [SerializeField] private InventoryGridScreen fishScreen;
    [SerializeField] private InventoryGridScreen materialsScreen;
    [SerializeField] private InventoryGridScreen consumablesScreen;
    
    [Header("Auto-Found Buttons (Can Override in Inspector)")]
    [SerializeField] private Button gearButton;
    [SerializeField] private Button fishButton;
    [SerializeField] private Button materialsButton;
    [SerializeField] private Button consumablesButton;
    [SerializeField] private Button gearBackButton;
    [SerializeField] private Button fishBackButton;
    [SerializeField] private Button materialsBackButton;
    [SerializeField] private Button consumablesBackButton;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showAutoFindLogs = true;
    
    private bool isInventoryOpen = false;
    private bool isFishing = false;
    
    private void Awake()
    {
        AutoFindReferences();
        CloseAllScreens();
        SetupButtonListeners();
    }
    
    private void AutoFindReferences()
    {
        if (cursorManager == null)
        {
            cursorManager = GetComponent<CursorManager>();
            if (showAutoFindLogs && cursorManager != null)
                Debug.Log("[InventoryUI] Auto-found CursorManager");
        }
        
        if (inventoryScreen == null)
        {
            Transform found = transform.Find("InventoryScreen");
            if (found != null)
            {
                inventoryScreen = found.gameObject;
                if (showAutoFindLogs)
                    Debug.Log("[InventoryUI] Auto-found InventoryScreen");
            }
            else
            {
                Debug.LogWarning("[InventoryUI] Could not find child named 'InventoryScreen'!");
            }
        }
        
        if (gearScreen == null)
        {
            gearScreen = GetComponentInChildren<GearScreen>(true);
            if (showAutoFindLogs && gearScreen != null)
                Debug.Log($"[InventoryUI] Auto-found GearScreen: {gearScreen.name}");
        }
        
        InventoryGridScreen[] gridScreens = GetComponentsInChildren<InventoryGridScreen>(true);
        foreach (var screen in gridScreens)
        {
            if (fishScreen == null && screen.name.Contains("Fish"))
            {
                fishScreen = screen;
                if (showAutoFindLogs) Debug.Log($"[InventoryUI] Auto-found FishScreen: {screen.name}");
            }
            else if (materialsScreen == null && screen.name.Contains("Material"))
            {
                materialsScreen = screen;
                if (showAutoFindLogs) Debug.Log($"[InventoryUI] Auto-found MaterialsScreen: {screen.name}");
            }
            else if (consumablesScreen == null && screen.name.Contains("Consumable"))
            {
                consumablesScreen = screen;
                if (showAutoFindLogs) Debug.Log($"[InventoryUI] Auto-found ConsumablesScreen: {screen.name}");
            }
        }
        
        AutoFindButton(ref gearButton, "GearButton");
        AutoFindButton(ref fishButton, "FishButton");
        AutoFindButton(ref materialsButton, "MaterialsButton");
        AutoFindButton(ref consumablesButton, "ConsumablesButton");
        AutoFindButton(ref gearBackButton, "GearBackButton");
        AutoFindButton(ref fishBackButton, "FishBackButton");
        AutoFindButton(ref materialsBackButton, "MaterialsBackButton");
        AutoFindButton(ref consumablesBackButton, "ConsumablesBackButton");
    }
    
    private void AutoFindButton(ref Button button, string buttonName)
    {
        if (button != null) return;
        
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn.name == buttonName)
            {
                button = btn;
                if (showAutoFindLogs)
                    Debug.Log($"[InventoryUI] Auto-found button: {buttonName}");
                return;
            }
        }
        
        if (showAutoFindLogs)
            Debug.LogWarning($"[InventoryUI] Could not find button named '{buttonName}'");
    }
    
    private void SetupButtonListeners()
    {
        if (gearButton != null) gearButton.onClick.AddListener(OpenGearScreen);
        if (fishButton != null) fishButton.onClick.AddListener(OpenFishScreen);
        if (materialsButton != null) materialsButton.onClick.AddListener(OpenMaterialsScreen);
        if (consumablesButton != null) consumablesButton.onClick.AddListener(OpenConsumablesScreen);
        if (gearBackButton != null) gearBackButton.onClick.AddListener(ReturnToInventoryScreen);
        if (fishBackButton != null) fishBackButton.onClick.AddListener(ReturnToInventoryScreen);
        if (materialsBackButton != null) materialsBackButton.onClick.AddListener(ReturnToInventoryScreen);
        if (consumablesBackButton != null) consumablesBackButton.onClick.AddListener(ReturnToInventoryScreen);
    }
    
    // Named handlers so OnEnable/OnDisable subscriptions match correctly
    private void OnLineCastHandler() => isFishing = true;
    private void OnMinigameStartedHandler(CatchableItem fish, EquippedGearInventory gear, FishingPool pool) => isFishing = true;
    private void OnMinigameEndedHandler() => isFishing = false;
    private void OnFishCaughtHandler(CatchableItem fish) => isFishing = false;
    private void OnFishEscapedHandler(CatchableItem fish) => isFishing = false;
    
    private void OnEnable()
    {
        if (inventoryAction != null)
        {
            inventoryAction.action.Enable();
            inventoryAction.action.performed += OnInventoryKeyPressed;
        }
        
        FishingEvents.OnLineCast += OnLineCastHandler;
        FishingEvents.OnMinigameStarted += OnMinigameStartedHandler;
        FishingEvents.OnMinigameEnded += OnMinigameEndedHandler;
        FishingEvents.OnFishCaught += OnFishCaughtHandler;
        FishingEvents.OnFishEscaped += OnFishEscapedHandler;
    }
    
    private void OnDisable()
    {
        if (inventoryAction != null)
        {
            inventoryAction.action.performed -= OnInventoryKeyPressed;
            inventoryAction.action.Disable();
        }
        
        FishingEvents.OnLineCast -= OnLineCastHandler;
        FishingEvents.OnMinigameStarted -= OnMinigameStartedHandler;
        FishingEvents.OnMinigameEnded -= OnMinigameEndedHandler;
        FishingEvents.OnFishCaught -= OnFishCaughtHandler;
        FishingEvents.OnFishEscaped -= OnFishEscapedHandler;
    }
    
    private void OnInventoryKeyPressed(InputAction.CallbackContext context)
    {
        if (isFishing)
        {
            if (showDebugLogs)
                Debug.Log("[InventoryUI] Cannot open inventory while fishing!");
            return;
        }
        
        if (isInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }
    
    public void OpenInventory()
    {
        if (isFishing)
        {
            if (showDebugLogs)
                Debug.Log("[InventoryUI] Cannot open inventory while fishing!");
            return;
        }
        
        CloseAllScreens();
        
        if (inventoryScreen != null)
            inventoryScreen.SetActive(true);
        
        isInventoryOpen = true;
        
        if (cursorManager != null)
            cursorManager.ShowCursor();
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Inventory opened");
    }
    
    public void CloseInventory()
    {
        CloseAllScreens();
        isInventoryOpen = false;
        
        if (cursorManager != null)
            cursorManager.HideCursor();
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Inventory closed");
    }
    
    private void CloseAllScreens()
    {
        if (inventoryScreen != null) inventoryScreen.SetActive(false);
        if (gearScreen != null) gearScreen.gameObject.SetActive(false);
        if (fishScreen != null) fishScreen.gameObject.SetActive(false);
        if (materialsScreen != null) materialsScreen.gameObject.SetActive(false);
        if (consumablesScreen != null) consumablesScreen.gameObject.SetActive(false);
    }
    
    public void OpenGearScreen()
    {
        CloseAllScreens();
        
        if (gearScreen != null)
        {
            gearScreen.gameObject.SetActive(true);
            gearScreen.RefreshGear();
        }
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Gear screen opened");
    }
    
    public void OpenFishScreen()
    {
        CloseAllScreens();
        
        if (fishScreen != null)
        {
            fishScreen.gameObject.SetActive(true);
            fishScreen.PopulateGrid(GameManager._instance.Inventory);
        }
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Fish screen opened");
    }
    
    public void OpenMaterialsScreen()
    {
        CloseAllScreens();
        
        if (materialsScreen != null)
        {
            materialsScreen.gameObject.SetActive(true);
            materialsScreen.PopulateGrid(GameManager._instance.Inventory);
        }
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Materials screen opened");
    }
    
    public void OpenConsumablesScreen()
    {
        CloseAllScreens();
        
        if (consumablesScreen != null)
        {
            consumablesScreen.gameObject.SetActive(true);
            consumablesScreen.PopulateGrid(GameManager._instance.Inventory);
        }
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Consumables screen opened");
    }
    
    public void ReturnToInventoryScreen()
    {
        CloseAllScreens();
        
        if (inventoryScreen != null)
            inventoryScreen.SetActive(true);
        
        if (showDebugLogs)
            Debug.Log("[InventoryUI] Returned to inventory screen");
    }
    
    public bool IsOpen() => isInventoryOpen;
}