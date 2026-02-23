using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Catchable Item", menuName = "Fishing Game/Items/Catchable Item")]
public class CatchableItem : Item
{
    [Header("Catch Probability")]
    [Tooltip("Probability weight within its rarity tier. NOT physical size.")]
    public float spawnProbabilityWeight = 50f;

    [Header("Fish Identity (Fish type only)")]
    [Tooltip("Human readable species name. e.g. 'Carp', 'Rainbow Trout'. ID fields are auto-generated.")]
    public string speciesDisplayName;

    [Tooltip("Auto-generated from speciesDisplayName + weightClass + rarity. Do not edit manually.")]
    public FishWeightClass weightClass = FishWeightClass.Modest;

    [Header("Catch Conditions")]
    public List<BaitType> baitConditions = new List<BaitType>();

    [Header("Fish Stats (Fish type only)")]
    public float fishStrength = 10f;
    public float fishResistance = 10f;
    public float fishAgitation = 10f;

    [Header("Reel Sequence")]
    [SerializeField] private List<ReelButton> reelSequence = new List<ReelButton>();
    [SerializeField] private List<ReelButton> agitatedButtons = new List<ReelButton>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(speciesDisplayName)) return;

        // e.g. "Rainbow Trout" → "rainbow_trout"
        string slug = speciesDisplayName.Trim().ToLower().Replace(" ", "_");

        // Auto-fill all ID/name fields
        speciesID   = slug;
        itemID      = $"{slug}_{weightClass.ToString().ToLower()}_{rarity.ToString().ToLower()}";
        itemName    = speciesDisplayName.Trim();

        // Rename the asset file itself to match (optional but very handy)
        string expectedAssetName = $"{speciesDisplayName} ({weightClass}, {rarity})";
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(assetPath) && name != expectedAssetName)
        {
            name = expectedAssetName;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    // --- everything below unchanged ---

    [HideInInspector] public string speciesID;

    public override AcquisitionMethod GetPrimaryAcquisitionMethod()
    {
        if (HasType(ItemType.Fish)) return AcquisitionMethod.Fished;
        if (HasType(ItemType.Plant)) return AcquisitionMethod.Gathered;
        return AcquisitionMethod.Found;
    }

    public IReadOnlyList<ReelButton> GetReelSequence() => reelSequence.AsReadOnly();
    public int GetSequenceLength() => reelSequence.Count;
    public IReadOnlyList<ReelButton> GetAgitatedButtons() => agitatedButtons.AsReadOnly();
}