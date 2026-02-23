using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generic grid screen for Fish, Materials, Consumables
/// AUTO-FINDS: Container, prefab from Resources
/// NAMING CONVENTIONS: Container must be named "GridContainer"
/// </summary>
public class InventoryGridScreen : MonoBehaviour
{
    [Header("Prefabs (Auto-loaded from Resources if not assigned)")]
    [SerializeField] private GameObject cellPrefab;
    
    [Header("Auto-Found Container (Can Override in Inspector)")]
    [SerializeField] private Transform gridContainer;
    
    [Header("Visual Config")]
    [SerializeField] private InventoryVisualConfig visualConfig;
    
    [Header("Category")]
    [SerializeField] private ItemCategory category;
    
    [Header("Debug")]
    [SerializeField] private bool showAutoFindLogs = true;
    
    private List<InventoryCell> activeCells = new List<InventoryCell>();
    
    public enum ItemCategory
    {
        Fish,
        Materials,
        Consumables
    }
    
    private void Awake()
    {
        AutoFindReferences();
    }
    
    /// <summary>
    /// Auto-find all references if not manually assigned
    /// </summary>
    private void AutoFindReferences()
    {
        // Auto-find grid container
        if (gridContainer == null)
        {
            Transform found = transform.Find("GridContainer");
            if (found != null)
            {
                gridContainer = found;
                if (showAutoFindLogs)
                    Debug.Log($"[{name}] Auto-found GridContainer");
            }
            else
            {
                Debug.LogWarning($"[{name}] Could not find 'GridContainer' child!");
            }
        }
        
        // Auto-load cell prefab from Resources
        if (cellPrefab == null)
        {
            cellPrefab = Resources.Load<GameObject>("Prefabs/InventoryCell");
            if (showAutoFindLogs && cellPrefab != null)
                Debug.Log($"[{name}] Auto-loaded InventoryCell prefab from Resources");
            else if (cellPrefab == null)
                Debug.LogWarning($"[{name}] Could not load 'Resources/Prefabs/InventoryCell'!");
        }
    }
    
    public void PopulateGrid(PlayerInventory inventory)
    {
        ClearGrid();
        
        if (gridContainer == null)
        {
            Debug.LogError($"[{name}] Grid container is null! Cannot populate.");
            return;
        }
        
        List<InventoryItem> items = GetItemsForCategory(inventory);
        
        if (items.Count == 0)
        {
            Debug.Log($"[{name}] No items in category: {category}");
            return;
        }
        
        int itemsPerRow = visualConfig != null ? visualConfig.maxItemsPerRow : 7;
        int rowsNeeded = Mathf.CeilToInt((float)items.Count / itemsPerRow);
        
        for (int row = 0; row < rowsNeeded; row++)
        {
            GameObject rowObj = new GameObject($"Row_{row}");
            rowObj.transform.SetParent(gridContainer, false);
            
            HorizontalLayoutGroup layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = visualConfig != null ? visualConfig.cellSpacing : 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            int startIdx = row * itemsPerRow;
            int endIdx = Mathf.Min(startIdx + itemsPerRow, items.Count);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                CreateCell(items[i], rowObj.transform);
            }
        }
        
        Debug.Log($"[{name}] Populated {items.Count} items in {rowsNeeded} rows for {category}");
    }
    
    private void CreateCell(InventoryItem item, Transform parent)
    {
        if (cellPrefab == null)
        {
            Debug.LogError($"[{name}] Cell prefab is null! Cannot create cell.");
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
            
            cell.SetItem(item, visualConfig);
            cell.OnCellClicked += HandleCellClicked;
            activeCells.Add(cell);
        }
    }
    
    private List<InventoryItem> GetItemsForCategory(PlayerInventory inventory)
    {
        List<InventoryItem> allItems = inventory.Items.ToList();
        
        switch (category)
        {
            case ItemCategory.Fish:
                return allItems.Where(i => 
                    i.itemType1 == ItemType.Fish || i.itemType2 == ItemType.Fish
                ).ToList();
            
            case ItemCategory.Materials:
                return allItems.Where(i => 
                    (i.itemType1 == ItemType.Plant || i.itemType2 == ItemType.Plant ||
                     i.itemType1 == ItemType.Metal || i.itemType2 == ItemType.Metal) &&
                    i.itemType1 != ItemType.Fish && i.itemType2 != ItemType.Fish
                ).ToList();
            
            case ItemCategory.Consumables:
                return allItems.Where(i => 
                    i.isConsumable && 
                    i.itemType1 == ItemType.Null && 
                    i.itemType2 == ItemType.Null
                ).ToList();
            
            default:
                return new List<InventoryItem>();
        }
    }
    
    private void ClearGrid()
    {
        foreach (var cell in activeCells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        activeCells.Clear();
        
        if (gridContainer != null)
        {
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void HandleCellClicked(InventoryCell cell, object itemData)
    {
        Debug.Log($"[{name}] Clicked item: {itemData}");
        // TODO: Show item details/tooltip
    }
}