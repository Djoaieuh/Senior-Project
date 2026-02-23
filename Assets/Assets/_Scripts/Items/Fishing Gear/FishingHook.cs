using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Fishing Hook", menuName = "Fishing Game/Gear/Fishing Hook")]
public class FishingHook : ScriptableObject
{
    [Header("Identification")]
    public string hookID;
    public string hookName;
    public Sprite hookIcon;
    
    [Header("Catch Type Bonuses")]
    [Tooltip("Item types this hook is good at catching")]
    public List<ItemType> preferredTypes = new List<ItemType>();
    
    [Tooltip("Weight bonus when catching preferred types (multiplicative)")]
    public float typeWeightBonus = 1.5f;
    
    [Header("Bait Slots")]
    [Tooltip("How many baits can be equipped at once on this hook")]
    [Range(1, 4)]
    public int baitSlots = 1;
    
    [Header("Stats")]
    [Tooltip("Bonus to rod resistance")]
    public float resistanceBonus = 0f;
    [Tooltip("How stable the line is (affects button scrambling)")]
    public float lineStabilityBonus = 0f;
    [Tooltip("Bonus to reeling power")]
    public float reelingPowerBonus = 0f;
    
    [Header("Luck")]
    [Tooltip("Bonus to luck stat")]
    public float luck = 0f;
    
    [Header("Visual/Flavor")]
    [TextArea(3, 5)]
    public string description;
}