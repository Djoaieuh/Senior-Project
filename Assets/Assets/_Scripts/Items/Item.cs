using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class JournalSettings
{
    [Tooltip("Does this item create a journal entry when obtained?")]
    public bool createsJournalEntry = true;
    
    [Tooltip("Does this item track weight? (Fish only)")]
    public bool tracksWeight = false;
    
    [Tooltip("Does this item track acquisition count? (times caught/gathered/bought)")]
    public bool tracksAcquisitionCount = true;
    
    [Tooltip("Custom journal page layout prefab (leave null for default)")]
    public GameObject customPageLayout;
}

public abstract class Item : ScriptableObject
{
    [Header("Identification")]
    public string itemID;
    public string itemName;

    [Header("Item Classification")]
    public ItemType itemType1;
    public ItemType itemType2 = ItemType.Null;
    public ItemRarity rarity;

    [Header("Visual & Description")]
    public Sprite icon;
    [TextArea(3, 6)]
    public string description;

    [Header("Journal Settings")]
    public JournalSettings journalSettings = new JournalSettings();

    // Helpers
    public bool HasType(ItemType type) => itemType1 == type || itemType2 == type;
    public bool IsDualType() => itemType2 != ItemType.Null;

    public abstract AcquisitionMethod GetPrimaryAcquisitionMethod();
}