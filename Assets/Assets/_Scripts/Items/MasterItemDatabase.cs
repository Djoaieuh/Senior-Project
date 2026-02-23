using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MasterItemDatabase", menuName = "Fishing Game/Master Item Database")]
public class MasterItemDatabase : ScriptableObject
{
    [Header("All Items")]
    [SerializeField] private List<CatchableItem> allCatchableItems = new List<CatchableItem>();
    [SerializeField] private List<RodBase> allRodBases = new List<RodBase>();
    [SerializeField] private List<Reel> allReels = new List<Reel>();
    [SerializeField] private List<FishingLine> allFishingLines = new List<FishingLine>();
    [SerializeField] private List<FishingHook> allFishingHooks = new List<FishingHook>();
    [SerializeField] private List<ShopItem> allShopItems = new List<ShopItem>(); // Bait, consumables, etc.

    // Runtime lookup - built on first access
    private Dictionary<string, CatchableItem> catchableLookup;
    private Dictionary<string, RodBase> rodLookup;
    private Dictionary<string, Reel> reelLookup;
    private Dictionary<string, FishingLine> lineLookup;
    private Dictionary<string, FishingHook> hookLookup;
    private Dictionary<string, ShopItem> shopLookup;

    // Singleton-style access - assign this in GameManager inspector
    public static MasterItemDatabase Instance { get; private set; }

    public static void SetInstance(MasterItemDatabase db)
    {
        Instance = db;
        Instance.BuildLookups();
    }

    // ============================================
    // LOOKUP METHODS
    // ============================================

    public CatchableItem GetCatchable(string itemID)
    {
        catchableLookup.TryGetValue(itemID, out CatchableItem item);
        if (item == null)
            Debug.LogWarning($"[MasterItemDatabase] CatchableItem not found: {itemID}");
        return item;
    }

    public RodBase GetRodBase(string itemID)
    {
        rodLookup.TryGetValue(itemID, out RodBase item);
        return item;
    }

    public Reel GetReel(string itemID)
    {
        reelLookup.TryGetValue(itemID, out Reel item);
        return item;
    }

    public FishingLine GetFishingLine(string itemID)
    {
        lineLookup.TryGetValue(itemID, out FishingLine item);
        return item;
    }

    public FishingHook GetFishingHook(string itemID)
    {
        hookLookup.TryGetValue(itemID, out FishingHook item);
        return item;
    }

    public ShopItem GetShopItem(string itemID)
    {
        shopLookup.TryGetValue(itemID, out ShopItem item);
        return item;
    }

    /// <summary>
    /// Get icon for any item by ID - checks all categories.
    /// Call this after loading from JSON to restore sprites.
    /// </summary>
    public Sprite GetIcon(string itemID)
    {
        if (catchableLookup.TryGetValue(itemID, out CatchableItem catchable))
            return catchable.icon;
        if (rodLookup.TryGetValue(itemID, out RodBase rod))
            return rod.rodIcon;
        if (reelLookup.TryGetValue(itemID, out Reel reel))
            return reel.reelIcon;
        if (lineLookup.TryGetValue(itemID, out FishingLine line))
            return line.lineIcon;
        if (hookLookup.TryGetValue(itemID, out FishingHook hook))
            return hook.hookIcon;
        if (shopLookup.TryGetValue(itemID, out ShopItem shopItem))
            return shopItem.icon;

        Debug.LogWarning($"[MasterItemDatabase] No icon found for: {itemID}");
        return null;
    }

    /// <summary>
    /// Get all catchable items of a specific species (all weight classes)
    /// </summary>
    public List<CatchableItem> GetSpecies(string speciesID)
    {
        return allCatchableItems.Where(i => i.speciesID == speciesID).ToList();
    }

    /// <summary>
    /// Get all catchable items of a specific rarity
    /// </summary>
    public List<CatchableItem> GetByRarity(ItemRarity rarity)
    {
        return allCatchableItems.Where(i => i.rarity == rarity).ToList();
    }

    // ============================================
    // INTERNAL
    // ============================================

    private void BuildLookups()
    {
        catchableLookup = allCatchableItems
            .Where(i => i != null)
            .ToDictionary(i => i.itemID, i => i);

        rodLookup = allRodBases
            .Where(i => i != null)
            .ToDictionary(i => i.rodID, i => i);

        reelLookup = allReels
            .Where(i => i != null)
            .ToDictionary(i => i.reelID, i => i);

        lineLookup = allFishingLines
            .Where(i => i != null)
            .ToDictionary(i => i.lineID, i => i);

        hookLookup = allFishingHooks
            .Where(i => i != null)
            .ToDictionary(i => i.hookID, i => i);

        shopLookup = allShopItems
            .Where(i => i != null)
            .ToDictionary(i => i.itemID, i => i);

        Debug.Log($"[MasterItemDatabase] Built lookups: " +
                  $"{catchableLookup.Count} catchables, " +
                  $"{rodLookup.Count} rods, " +
                  $"{reelLookup.Count} reels, " +
                  $"{lineLookup.Count} lines, " +
                  $"{hookLookup.Count} hooks, " +
                  $"{shopLookup.Count} shop items");
    }

    // ============================================
    // EDITOR AUTO-POPULATE
    // ============================================

#if UNITY_EDITOR
    [ContextMenu("Auto-Populate From Project")]
    private void AutoPopulate()
    {
        allCatchableItems = FindAllAssets<CatchableItem>();
        allRodBases       = FindAllAssets<RodBase>();
        allReels          = FindAllAssets<Reel>();
        allFishingLines   = FindAllAssets<FishingLine>();
        allFishingHooks   = FindAllAssets<FishingHook>();
        allShopItems      = FindAllAssets<ShopItem>();

        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"[MasterItemDatabase] Auto-populated: " +
                  $"{allCatchableItems.Count} catchables, " +
                  $"{allRodBases.Count} rods, " +
                  $"{allReels.Count} reels, " +
                  $"{allFishingLines.Count} lines, " +
                  $"{allFishingHooks.Count} hooks, " +
                  $"{allShopItems.Count} shop items");
    }

    private List<T> FindAllAssets<T>() where T : ScriptableObject
    {
        return UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();
    }
#endif
}