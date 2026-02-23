using UnityEngine;

[CreateAssetMenu(fileName = "New Reel", menuName = "Fishing Game/Gear/Reel")]
public class Reel : ScriptableObject
{
    [Header("Identification")]
    public string reelID;
    public string reelName;
    public Sprite reelIcon;
    
    [Header("Minigame Stats")]
    [Tooltip("How many buttons are visible in the sequence preview (1-8)")]
    [Range(1, 8)]
    public int visibleButtonCount = 4;
    
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