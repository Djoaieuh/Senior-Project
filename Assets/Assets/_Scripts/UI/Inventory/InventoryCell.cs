using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Individual inventory cell
/// AUTO-FINDS: All visual elements by name
/// PRESERVES: Initial sprites from prefab if VisualConfig is not assigned
/// </summary>
public class InventoryCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Auto-Found Visual Elements (Can Override in Inspector)")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image rarityBorderImage;
    [SerializeField] private Image selectionIndicatorImage;
    [SerializeField] private Image equippedIndicatorImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    
    [Header("Visual Config")]
    [SerializeField] private InventoryVisualConfig visualConfig;
    
    [Header("Debug")]
    [SerializeField] private bool showAutoFindLogs = false;
    
    // CACHE initial sprites from prefab
    private Sprite initialBackgroundSprite;
    private Sprite initialRarityBorderSprite;
    private Sprite initialSelectionSprite;
    private Sprite initialEquippedSprite;
    
    private object itemData;
    private int quantity;
    private bool isEquipped;
    private bool isSelected;
    
    public System.Action<InventoryCell, object> OnCellClicked;
    
    private void Awake()
    {
        AutoFindReferences();
        CacheInitialSprites(); // CACHE BEFORE ANYTHING ELSE
        InitializeVisuals();
    }
    
    /// <summary>
    /// Cache the initial sprites from the prefab
    /// These will be used as fallbacks if VisualConfig is not assigned
    /// </summary>
    private void CacheInitialSprites()
    {
        if (backgroundImage != null) 
            initialBackgroundSprite = backgroundImage.sprite;
        if (rarityBorderImage != null) 
            initialRarityBorderSprite = rarityBorderImage.sprite;
        if (selectionIndicatorImage != null) 
            initialSelectionSprite = selectionIndicatorImage.sprite;
        if (equippedIndicatorImage != null) 
            initialEquippedSprite = equippedIndicatorImage.sprite;
    }
    
    /// <summary>
    /// Auto-find all visual elements by name
    /// </summary>
    private void AutoFindReferences()
    {
        AutoFindImage(ref backgroundImage, "Background");
        AutoFindImage(ref itemIconImage, "ItemIcon");
        AutoFindImage(ref rarityBorderImage, "RarityBorder");
        AutoFindImage(ref selectionIndicatorImage, "SelectionIndicator");
        AutoFindImage(ref equippedIndicatorImage, "EquippedIndicator");
        
        if (quantityText == null)
        {
            Transform found = transform.Find("QuantityText");
            if (found != null)
            {
                quantityText = found.GetComponent<TextMeshProUGUI>();
                if (showAutoFindLogs && quantityText != null)
                    Debug.Log($"[{name}] Auto-found QuantityText");
            }
        }
    }
    
    private void AutoFindImage(ref Image imageRef, string childName)
    {
        if (imageRef != null) return;
        
        Transform found = transform.Find(childName);
        if (found != null)
        {
            imageRef = found.GetComponent<Image>();
            if (showAutoFindLogs && imageRef != null)
                Debug.Log($"[{name}] Auto-found {childName}");
        }
    }
    
    private void InitializeVisuals()
    {
        if (selectionIndicatorImage != null) selectionIndicatorImage.gameObject.SetActive(false);
        if (equippedIndicatorImage != null) equippedIndicatorImage.gameObject.SetActive(false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
    }
    
    public void SetEmpty()
    {
        itemData = null;
        quantity = 0;
        isEquipped = false;
        isSelected = false;
        
        // FIXED: Use cached sprite or VisualConfig, preserve if neither
        if (backgroundImage != null)
        {
            if (visualConfig != null && visualConfig.emptyCellBackground != null)
                backgroundImage.sprite = visualConfig.emptyCellBackground;
            else if (initialBackgroundSprite != null)
                backgroundImage.sprite = initialBackgroundSprite;
            // else: keep whatever sprite is already there
        }
        
        if (itemIconImage != null)
            itemIconImage.gameObject.SetActive(false);
        
        if (rarityBorderImage != null)
            rarityBorderImage.gameObject.SetActive(false);
        
        if (quantityText != null)
            quantityText.gameObject.SetActive(false);
        
        if (selectionIndicatorImage != null)
            selectionIndicatorImage.gameObject.SetActive(false);
        
        if (equippedIndicatorImage != null)
            equippedIndicatorImage.gameObject.SetActive(false);
    }
    
    public void SetItem(InventoryItem item, InventoryVisualConfig config)
    {
        itemData = item;
        quantity = item.quantity;
        visualConfig = config;
        
        UpdateVisuals(item.icon, item.rarity);
    }
    
    public void SetGearItem(object gearItem, Sprite icon, int qty, bool equipped, InventoryVisualConfig config)
    {
        itemData = gearItem;
        quantity = qty;
        isEquipped = equipped;
        visualConfig = config;
        
        UpdateVisuals(icon, ItemRarity.Common);
        
        if (equippedIndicatorImage != null)
        {
            equippedIndicatorImage.gameObject.SetActive(equipped);
            
            // FIXED: Use cached sprite or VisualConfig
            if (visualConfig != null && visualConfig.equippedIcon != null)
                equippedIndicatorImage.sprite = visualConfig.equippedIcon;
            else if (initialEquippedSprite != null)
                equippedIndicatorImage.sprite = initialEquippedSprite;
        }
    }
    
    private void UpdateVisuals(Sprite icon, ItemRarity rarity)
    {
        // FIXED: Background - preserve initial sprite if no config
        if (backgroundImage != null)
        {
            if (visualConfig != null && visualConfig.occupiedCellBackground != null)
                backgroundImage.sprite = visualConfig.occupiedCellBackground;
            else if (initialBackgroundSprite != null)
                backgroundImage.sprite = initialBackgroundSprite;
        }
        
        // Item icon
        if (itemIconImage != null)
        {
            itemIconImage.sprite = icon;
            itemIconImage.gameObject.SetActive(icon != null);
        }
        
        // FIXED: Rarity border - preserve initial sprite if no config
        if (rarityBorderImage != null)
        {
            if (visualConfig != null)
            {
                rarityBorderImage.color = visualConfig.GetRarityColor(rarity);
            }
            else
            {
                // No config - keep initial sprite and color
                if (initialRarityBorderSprite != null)
                    rarityBorderImage.sprite = initialRarityBorderSprite;
            }
            rarityBorderImage.gameObject.SetActive(true);
        }
        
        // Quantity text
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
                
                if (visualConfig != null)
                {
                    quantityText.color = visualConfig.quantityTextColor;
                    quantityText.fontSize = visualConfig.quantityFontSize;
                }
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionIndicatorImage != null)
        {
            selectionIndicatorImage.gameObject.SetActive(selected);
            
            // FIXED: Use cached sprite or VisualConfig
            if (visualConfig != null && visualConfig.selectedBorder != null)
                selectionIndicatorImage.sprite = visualConfig.selectedBorder;
            else if (initialSelectionSprite != null)
                selectionIndicatorImage.sprite = initialSelectionSprite;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnCellClicked?.Invoke(this, itemData);
    }
    
    public object GetItemData() => itemData;
    public bool IsEmpty() => itemData == null;
}