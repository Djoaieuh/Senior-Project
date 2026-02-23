using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Special screen for gear management
/// AUTO-FINDS: Equipped cells by name, containers, prefabs from Resources
/// NAMING CONVENTIONS: Cells must be named "RodBaseCell", "ReelCell", etc.
/// </summary>
public class GearScreen : MonoBehaviour
{
    [Header("Prefabs (Auto-loaded from Resources if not assigned)")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject noBaitButtonPrefab;
    
    [Header("Auto-Found Equipped Cells (Can Override in Inspector)")]
    [SerializeField] private InventoryCell rodBaseCell;
    [SerializeField] private InventoryCell reelCell;
    [SerializeField] private InventoryCell lineCell;
    [SerializeField] private InventoryCell hookCell;
    [SerializeField] private InventoryCell baitCell;
    
    [Header("Auto-Found Containers (Can Override in Inspector)")]
    [SerializeField] private Transform selectionGridContainer;
    
    [Header("Visual Config")]
    [SerializeField] private InventoryVisualConfig visualConfig;
    
    [Header("Gear Icons (Designer assigns these)")]
    [SerializeField] private Sprite defaultRodIcon;
    [SerializeField] private Sprite defaultReelIcon;
    [SerializeField] private Sprite defaultLineIcon;
    [SerializeField] private Sprite defaultHookIcon;
    [SerializeField] private Sprite defaultBaitIcon;
    
    [Header("Debug")]
    [SerializeField] private bool showAutoFindLogs = true;
    
    private GearType? selectedGearType = null;
    private List<InventoryCell> selectionCells = new List<InventoryCell>();
    private GameObject noBaitButton;
    
    public enum GearType
    {
        RodBase,
        Reel,
        Line,
        Hook,
        Bait
    }
    
    private void Awake()
    {
        AutoFindReferences();
        SetupClickHandlers();
    }
    
    /// <summary>
    /// Auto-find all references if not manually assigned
    /// </summary>
    private void AutoFindReferences()
    {
        // Auto-find equipped cells by name
        AutoFindCell(ref rodBaseCell, "RodBaseCell");
        AutoFindCell(ref reelCell, "ReelCell");
        AutoFindCell(ref lineCell, "LineCell");
        AutoFindCell(ref hookCell, "HookCell");
        AutoFindCell(ref baitCell, "BaitCell");
        
        // Auto-find selection grid container
        if (selectionGridContainer == null)
        {
            Transform found = transform.Find("SelectionGridContainer");
            if (found != null)
            {
                selectionGridContainer = found;
                if (showAutoFindLogs)
                    Debug.Log("[GearScreen] Auto-found SelectionGridContainer");
            }
            else
            {
                Debug.LogWarning("[GearScreen] Could not find 'SelectionGridContainer' child!");
            }
        }
        
        // Auto-load prefabs from Resources
        if (cellPrefab == null)
        {
            cellPrefab = Resources.Load<GameObject>("Prefabs/InventoryCell");
            if (showAutoFindLogs && cellPrefab != null)
                Debug.Log("[GearScreen] Auto-loaded InventoryCell prefab from Resources");
            else if (cellPrefab == null)
                Debug.LogWarning("[GearScreen] Could not load 'Resources/Prefabs/InventoryCell'!");
        }
        
        if (noBaitButtonPrefab == null)
        {
            noBaitButtonPrefab = Resources.Load<GameObject>("Prefabs/NoBaitButton");
            if (showAutoFindLogs && noBaitButtonPrefab != null)
                Debug.Log("[GearScreen] Auto-loaded NoBaitButton prefab from Resources");
        }
    }
    
    /// <summary>
    /// Auto-find a cell by name
    /// </summary>
    private void AutoFindCell(ref InventoryCell cell, string cellName)
    {
        if (cell != null) return; // Already assigned
        
        InventoryCell[] allCells = GetComponentsInChildren<InventoryCell>(true);
        foreach (var c in allCells)
        {
            if (c.name == cellName)
            {
                cell = c;
                if (showAutoFindLogs)
                    Debug.Log($"[GearScreen] Auto-found cell: {cellName}");
                return;
            }
        }
        
        if (showAutoFindLogs)
            Debug.LogWarning($"[GearScreen] Could not find InventoryCell named '{cellName}'");
    }
    
    /// <summary>
    /// Setup click handlers for equipped cells
    /// </summary>
    private void SetupClickHandlers()
    {
        if (rodBaseCell != null) rodBaseCell.OnCellClicked += (cell, data) => SelectGearType(GearType.RodBase);
        if (reelCell != null) reelCell.OnCellClicked += (cell, data) => SelectGearType(GearType.Reel);
        if (lineCell != null) lineCell.OnCellClicked += (cell, data) => SelectGearType(GearType.Line);
        if (hookCell != null) hookCell.OnCellClicked += (cell, data) => SelectGearType(GearType.Hook);
        if (baitCell != null) baitCell.OnCellClicked += (cell, data) => SelectGearType(GearType.Bait);
    }
    
    public void RefreshGear()
    {
        EquippedGearInventory equippedGear = GameManager._instance.EquippedGear;
        
        UpdateEquippedCell(rodBaseCell, equippedGear.RodBase, defaultRodIcon);
        UpdateEquippedCell(reelCell, equippedGear.Reel, defaultReelIcon);
        UpdateEquippedCell(lineCell, equippedGear.Line, defaultLineIcon);
        UpdateEquippedCell(hookCell, equippedGear.Hook, defaultHookIcon);
        UpdateBaitCell();
        
        if (selectedGearType.HasValue)
        {
            SelectGearType(selectedGearType.Value);
        }
    }
    
    private void UpdateEquippedCell(InventoryCell cell, object gearData, Sprite defaultIcon)
    {
        if (cell == null) return;
        
        if (gearData == null)
        {
            cell.SetEmpty();
            return;
        }
        
        Sprite icon = GetGearIcon(gearData) ?? defaultIcon;
        cell.SetGearItem(gearData, icon, 1, true, visualConfig);
    }
    
    private void UpdateBaitCell()
    {
        if (baitCell == null) return;
        
        EquippedGearInventory equippedGear = GameManager._instance.EquippedGear;
        
        if (equippedGear.Bait == null)
        {
            baitCell.SetEmpty();
            return;
        }
        
        Sprite icon = equippedGear.Bait.icon ?? defaultBaitIcon;
        int quantity = equippedGear.BaitQuantity;
        
        baitCell.SetGearItem(equippedGear.Bait, icon, quantity, true, visualConfig);
    }
    
    private Sprite GetGearIcon(object gearData)
    {
        if (gearData is RodBaseInventoryItem rod) return rod.icon;
        if (gearData is ReelInventoryItem reel) return reel.icon;
        if (gearData is FishingLineInventoryItem line) return line.icon;
        if (gearData is FishingHookInventoryItem hook) return hook.icon;
        if (gearData is BaitInventoryItem bait) return bait.icon;
        return null;
    }
    
    private void SelectGearType(GearType gearType)
    {
        selectedGearType = gearType;
        
        ClearSelectionIndicators();
        HighlightSelectedCell(gearType);
        PopulateSelectionGrid(gearType);
        
        Debug.Log($"[GearScreen] Selected gear type: {gearType}");
    }
    
    private void ClearSelectionIndicators()
    {
        if (rodBaseCell != null) rodBaseCell.SetSelected(false);
        if (reelCell != null) reelCell.SetSelected(false);
        if (lineCell != null) lineCell.SetSelected(false);
        if (hookCell != null) hookCell.SetSelected(false);
        if (baitCell != null) baitCell.SetSelected(false);
    }
    
    private void HighlightSelectedCell(GearType gearType)
    {
        switch (gearType)
        {
            case GearType.RodBase:
                if (rodBaseCell != null) rodBaseCell.SetSelected(true);
                break;
            case GearType.Reel:
                if (reelCell != null) reelCell.SetSelected(true);
                break;
            case GearType.Line:
                if (lineCell != null) lineCell.SetSelected(true);
                break;
            case GearType.Hook:
                if (hookCell != null) hookCell.SetSelected(true);
                break;
            case GearType.Bait:
                if (baitCell != null) baitCell.SetSelected(true);
                break;
        }
    }
    
    private void PopulateSelectionGrid(GearType gearType)
    {
        ClearSelectionGrid();
        
        if (selectionGridContainer == null)
        {
            Debug.LogError("[GearScreen] Selection grid container is null!");
            return;
        }
        
        PlayerInventory inventory = GameManager._instance.Inventory;
        
        List<object> availableItems = new List<object>();
        
        switch (gearType)
        {
            case GearType.RodBase:
                availableItems.AddRange(inventory.GetAllRodBases());
                break;
            case GearType.Reel:
                availableItems.AddRange(inventory.GetAllReels());
                break;
            case GearType.Line:
                availableItems.AddRange(inventory.GetAllFishingLines());
                break;
            case GearType.Hook:
                availableItems.AddRange(inventory.GetAllFishingHooks());
                break;
            case GearType.Bait:
                availableItems.AddRange(inventory.GetAllBait());
                CreateNoBaitButton();
                break;
        }
        
        if (availableItems.Count == 0)
        {
            Debug.Log($"[GearScreen] No available items for {gearType}");
            return;
        }
        
        int itemsPerRow = visualConfig != null ? visualConfig.maxItemsPerRow : 7;
        int rowsNeeded = Mathf.CeilToInt((float)availableItems.Count / itemsPerRow);
        
        for (int row = 0; row < rowsNeeded; row++)
        {
            GameObject rowObj = new GameObject($"SelectionRow_{row}");
            rowObj.transform.SetParent(selectionGridContainer, false);
            
            HorizontalLayoutGroup layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = visualConfig != null ? visualConfig.cellSpacing : 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            
            int startIdx = row * itemsPerRow;
            int endIdx = Mathf.Min(startIdx + itemsPerRow, availableItems.Count);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                CreateSelectionCell(availableItems[i], gearType, rowObj.transform);
            }
        }
        
        Debug.Log($"[GearScreen] Populated {availableItems.Count} items for {gearType}");
    }
    
    private void CreateSelectionCell(object gearData, GearType gearType, Transform parent)
    {
        if (cellPrefab == null)
        {
            Debug.LogError("[GearScreen] Cell prefab is null! Cannot create cell.");
            return;
        }
        
        GameObject cellObj = Instantiate(cellPrefab, parent);
        InventoryCell cell = cellObj.GetComponent<InventoryCell>();
        
        if (cell != null)
        {
            LayoutElement layout = cellObj.GetComponent<LayoutElement>();
            if (layout == null) layout = cellObj.AddComponent<LayoutElement>();
            
            if (visualConfig != null)
            {
                layout.preferredWidth = visualConfig.cellWidth;
                layout.preferredHeight = visualConfig.cellHeight;
            }
            
            bool isEquipped = IsGearEquipped(gearData, gearType);
            Sprite icon = GetGearIcon(gearData);
            int quantity = (gearData is BaitInventoryItem bait) ? bait.quantity : 1;
            
            cell.SetGearItem(gearData, icon, quantity, isEquipped, visualConfig);
            cell.OnCellClicked += (c, data) => HandleSelectionCellClicked(data, gearType);
            selectionCells.Add(cell);
        }
    }
    
    private bool IsGearEquipped(object gearData, GearType gearType)
    {
        EquippedGearInventory equippedGear = GameManager._instance.EquippedGear;
        
        switch (gearType)
        {
            case GearType.RodBase:
                return gearData is RodBaseInventoryItem rod && equippedGear.RodBase?.itemID == rod.itemID;
            case GearType.Reel:
                return gearData is ReelInventoryItem reel && equippedGear.Reel?.itemID == reel.itemID;
            case GearType.Line:
                return gearData is FishingLineInventoryItem line && equippedGear.Line?.itemID == line.itemID;
            case GearType.Hook:
                return gearData is FishingHookInventoryItem hook && equippedGear.Hook?.itemID == hook.itemID;
            case GearType.Bait:
                return gearData is BaitInventoryItem bait && equippedGear.Bait?.baitType == bait.baitType;
            default:
                return false;
        }
    }
    
    private void CreateNoBaitButton()
    {
        if (noBaitButtonPrefab == null) return;
        
        noBaitButton = Instantiate(noBaitButtonPrefab, selectionGridContainer);
        Button button = noBaitButton.GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(EquipNoBait);
        }
    }
    
    private void HandleSelectionCellClicked(object gearData, GearType gearType)
    {
        switch (gearType)
        {
            case GearType.RodBase:
                if (gearData is RodBaseInventoryItem rod)
                    GameManager._instance.EquipRodBase(rod.itemID);
                break;
            
            case GearType.Reel:
                if (gearData is ReelInventoryItem reel)
                    GameManager._instance.EquipReel(reel.itemID);
                break;
            
            case GearType.Line:
                if (gearData is FishingLineInventoryItem line)
                    GameManager._instance.EquipLine(line.itemID);
                break;
            
            case GearType.Hook:
                if (gearData is FishingHookInventoryItem hook)
                    GameManager._instance.EquipHook(hook.itemID);
                break;
            
            case GearType.Bait:
                if (gearData is BaitInventoryItem bait)
                    GameManager._instance.EquipBait(bait.baitType, bait.quantity);
                break;
        }
        
        RefreshGear();
    }
    
    private void EquipNoBait()
    {
        GameManager._instance.EquippedGear.UnequipBait();
        FishingEvents.EquippedGearChanged();
        RefreshGear();
        
        Debug.Log("[GearScreen] Unequipped bait");
    }
    
    private void ClearSelectionGrid()
    {
        foreach (var cell in selectionCells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        selectionCells.Clear();
        
        if (noBaitButton != null)
        {
            Destroy(noBaitButton);
            noBaitButton = null;
        }
        
        if (selectionGridContainer != null)
        {
            foreach (Transform child in selectionGridContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}