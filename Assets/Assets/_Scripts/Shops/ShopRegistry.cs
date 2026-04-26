using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sits on the GameManager prefab.
/// Owns all shop save data, and is the single entry point for opening/closing any shop.
/// </summary>
public class ShopRegistry : MonoBehaviour
{
    public static ShopRegistry _instance { get; private set; }

    // ── Runtime state ──────────────────────────────────────────────────────
    /// <summary>Key = ShopData.locationID</summary>
    private Dictionary<string, ShopSaveData> shopSaveDatas = new Dictionary<string, ShopSaveData>();

    private ShopManager activeShopManager;

    // ── Events ─────────────────────────────────────────────────────────────
    public static event System.Action<ShopManager> OnShopOpened;
    public static event System.Action             OnShopClosed;

    // ══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
    }

    // ══════════════════════════════════════════════════════════════════════
    // OPEN / CLOSE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens a shop. Called by MapManager when the player clicks a shop waypoint.
    /// </summary>
    public void OpenShop(ShopData shopData)
    {
        if (shopData == null)
        {
            Debug.LogWarning("[ShopRegistry] Tried to open a null ShopData.");
            return;
        }

        // Check shop-level visibility conditions
        ShopSaveData save = GetOrCreateSaveData(shopData);
        if (!shopData.shopVisibilityConditions.IsMet(save))
        {
            Debug.Log($"[ShopRegistry] Shop '{shopData.shopName}' conditions not met — cannot open.");
            return;
        }

        activeShopManager = new ShopManager(shopData, save);
        Debug.Log($"[ShopRegistry] Opened shop: {shopData.shopName}");
        OnShopOpened?.Invoke(activeShopManager);
    }

    /// <summary>
    /// Closes the currently open shop and persists its state.
    /// </summary>
    public void CloseShop()
    {
        if (activeShopManager == null) return;

        // Persist state back into our dictionary
        ShopSaveData sd = activeShopManager.GetSaveData();
        shopSaveDatas[sd.shopID] = sd;

        activeShopManager = null;
        OnShopClosed?.Invoke();
        Debug.Log("[ShopRegistry] Shop closed.");
    }

    public bool IsShopOpen => activeShopManager != null;
    public ShopManager ActiveShop => activeShopManager;

    // ══════════════════════════════════════════════════════════════════════
    // VISIBILITY CHECK (used by MapPin to grey out / hide shop pins)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if the shop is visible/accessible on the map.
    /// </summary>
    public bool IsShopVisible(ShopData shopData)
    {
        if (shopData == null) return false;
        ShopSaveData save = GetOrCreateSaveData(shopData);
        return shopData.shopVisibilityConditions.IsMet(save);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SAVE / LOAD  (called by GameManager.SaveGame / ApplyToPlayerData)
    // ══════════════════════════════════════════════════════════════════════

    public List<ShopSaveData> GetAllSaveData()
    {
        // Flush active shop state first
        if (activeShopManager != null)
            shopSaveDatas[activeShopManager.GetSaveData().shopID] = activeShopManager.GetSaveData();

        return new List<ShopSaveData>(shopSaveDatas.Values);
    }

    public void LoadFromSaveData(List<ShopSaveData> savedShops)
    {
        shopSaveDatas.Clear();
        if (savedShops == null) return;
        foreach (var sd in savedShops)
            shopSaveDatas[sd.shopID] = sd;
        Debug.Log($"[ShopRegistry] Loaded {shopSaveDatas.Count} shop save states.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private ShopSaveData GetOrCreateSaveData(ShopData shopData)
    {
        if (!shopSaveDatas.TryGetValue(shopData.locationID, out ShopSaveData sd))
        {
            sd = new ShopSaveData { shopID = shopData.locationID };
            shopSaveDatas[shopData.locationID] = sd;
        }
        return sd;
    }
}