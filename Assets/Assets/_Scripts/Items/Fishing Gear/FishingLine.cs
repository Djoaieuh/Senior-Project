using UnityEngine;

[CreateAssetMenu(fileName = "New Fishing Line", menuName = "Fishing Game/Gear/Fishing Line")]
public class FishingLine : ScriptableObject
{
    [Header("Identification")]
    public string lineID;
    public string lineName;
    public Sprite lineIcon;
    
    [Header("Distance Stats")]
    [Tooltip("Maximum line length before it breaks")]
    public float lineLength = 100f;
    
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