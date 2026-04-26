using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a shop location. Extend LocationData so it plugs into the map system automatically.
/// Create via: Right-click → Fishing Game/Shop/Shop Data
/// </summary>
[CreateAssetMenu(fileName = "New Shop", menuName = "Fishing Game/Shop/Shop Data")]
public class ShopData : LocationData
{
    [Header("Shop Identity")]
    [Tooltip("Name shown in the shop UI header")]
    public string shopName = "Shop";

    [Tooltip("Short description shown beneath the shop name")]
    [TextArea(2, 3)]
    public string shopDescription = "";

    [Tooltip("Icon shown in the shop UI header")]
    public Sprite shopIcon;

    [Header("Trades")]
    [Tooltip("All trades available in this shop. Add as many as you like!")]
    public List<TradeData> trades = new List<TradeData>();

    [Header("Shop Unlock Conditions")]
    [Tooltip("Leave empty = always visible on map. Add conditions to hide until requirements met.")]
    public ShopUnlockConditionGroup shopVisibilityConditions = new ShopUnlockConditionGroup();
}