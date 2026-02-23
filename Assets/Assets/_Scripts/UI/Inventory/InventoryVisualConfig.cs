using UnityEngine;
using TMPro;

/// <summary>
/// Visual configuration for inventory UI
/// Designer can create different themes without touching code
/// </summary>
[CreateAssetMenu(fileName = "InventoryVisualConfig", menuName = "Fishing/UI/Inventory Visual Config")]
public class InventoryVisualConfig : ScriptableObject
{
    [Header("Cell Settings")]
    [Tooltip("Preferred width of each cell")]
    public float cellWidth = 80f;
    
    [Tooltip("Preferred height of each cell")]
    public float cellHeight = 80f;
    
    [Tooltip("Spacing between cells")]
    public float cellSpacing = 10f;
    
    [Tooltip("Maximum items per row")]
    [Range(1, 20)]
    public int maxItemsPerRow = 7;
    
    [Header("Cell Visuals")]
    [Tooltip("Background sprite for empty cells")]
    public Sprite emptyCellBackground;
    
    [Tooltip("Background sprite for occupied cells")]
    public Sprite occupiedCellBackground;
    
    [Tooltip("Border sprite for selected cell")]
    public Sprite selectedBorder;
    
    [Tooltip("Icon showing item is currently equipped")]
    public Sprite equippedIcon;
    
    [Header("Rarity Colors")]
    public Color commonColor      = new Color(0.8f, 0.8f, 0.8f);
    public Color uncommonColor    = new Color(0.3f, 1f,   0.3f);
    public Color rareColor        = new Color(0.3f, 0.5f, 1f);
    public Color extraordinaryColor = new Color(0.8f, 0.3f, 1f);
    public Color mythicalColor    = new Color(1f,   0.8f, 0.2f);
    
    [Header("Text Settings")]
    public int quantityFontSize = 14;
    public Color quantityTextColor = Color.white;
    
    /// <summary>
    /// Get color for item rarity
    /// </summary>
    public Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:       return commonColor;
            case ItemRarity.Uncommon:     return uncommonColor;
            case ItemRarity.Rare:         return rareColor;
            case ItemRarity.Extraordinary:return extraordinaryColor;
            case ItemRarity.Mythical:     return mythicalColor;
            default:                      return Color.white;
        }
    }
}