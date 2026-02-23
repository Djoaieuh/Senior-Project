using UnityEngine;

[CreateAssetMenu(fileName = "New Rod Base", menuName = "Fishing Game/Gear/Rod Base")]
public class RodBase : ScriptableObject
{
    [Header("Identification")]
    public string rodID;
    public string rodName;
    public Sprite rodIcon;
    
    [Header("Stats")]
    [Tooltip("Bonus to rod resistance")]
    public float resistanceBonus = 0f;
    [Tooltip("How stable the line is (affects button scrambling)")]
    public float lineStabilityBonus = 0f;
    [Tooltip("Bonus to reeling power")]
    public float reelingPowerBonus = 0f;
    
    [Header("Zone Boundaries")]
    [Tooltip("End of green zone (0-100) - larger green zone = easier")]
    public float greenZoneEnd = 30f;
    
    [Tooltip("Start of red zone (0-100) - smaller red zone = easier")]
    public float redZoneStart = 70f;
    
    [Header("Luck")]
    [Tooltip("Bonus to luck stat")]
    public float luck = 0f;
    
    [Header("Visual/Flavor")]
    [TextArea(3, 5)]
    public string description;
}