using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Fishing Game/Items/Shop Item")]
public class ShopItem : Item
{
    [Header("Shop Data")]
    [Tooltip("Base purchase price")]
    public int buyPrice = 100;
    
    [Tooltip("Sell value (if player can sell it back)")]
    public int sellPrice = 50;
    
    [Tooltip("Which shop(s) sell this item? (leave empty if all shops)")]
    public List<string> availableAtShops = new List<string>();
    
    [Tooltip("Can this item be sold by the player?")]
    public bool canSell = true;
    
    public override AcquisitionMethod GetPrimaryAcquisitionMethod()
    {
        return AcquisitionMethod.Bought;
    }
}