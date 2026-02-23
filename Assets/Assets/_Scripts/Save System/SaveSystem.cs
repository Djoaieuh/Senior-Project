using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

/// <summary>
/// Handles all saving and loading to disk.
/// Does not inherit MonoBehaviour - called directly by GameManager.
/// </summary>
public static class SaveSystem
{
    private static readonly string SaveFileName = "savegame.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    // ============================================
    // SAVE
    // ============================================

    public static void Save(PlayerData playerData, string currentLocationID, string currentSceneName)
    {
        try
        {
            SaveData data = new SaveData
            {
                schemaVersion = 1,
                lastSaved     = DateTime.Now,
                money         = playerData.Money,

                currentLocationID = currentLocationID,
                currentSceneName  = currentSceneName,
            };

            // Serialize inventory
            foreach (var item in playerData.Inventory.Items)
            {
                InventoryItemSaveData saveItem = ConvertToSaveData(item);
                if (saveItem != null)
                    data.inventoryItems.Add(saveItem);
            }

            // Serialize equipped gear
            data.equippedGear = ConvertEquippedGear(playerData.EquippedGear);

            // Serialize journal
            foreach (var entry in playerData.Journal.GetAllEntries())
            {
                data.journalEntries.Add(ConvertJournalEntry(entry));
            }

            // Serialize map
            var (ids, flags) = playerData.Map.ToSaveData();
            data.unlockedLocationIDs = ids;
            data.locationFlags       = flags;

            string json = JsonConvert.SerializeObject(data, JsonSettings);
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"[SaveSystem] Game saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    // ============================================
    // LOAD
    // ============================================

    public static SaveData Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveSystem] No save file found - fresh start.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonConvert.DeserializeObject<SaveData>(json, JsonSettings);

            Debug.Log($"[SaveSystem] Loaded save from {data.lastSaved}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    // ============================================
    // APPLY LOADED DATA TO RUNTIME
    // ============================================

    public static void ApplyToPlayerData(SaveData data, PlayerData playerData)
    {
        if (data == null) return;

        // Money
        playerData.SetMoney(data.money);

        // Inventory
        playerData.Inventory.Clear();
        foreach (var saveItem in data.inventoryItems)
        {
            InventoryItem runtimeItem = ConvertToRuntimeItem(saveItem);
            if (runtimeItem != null)
                playerData.Inventory.AddRawItem(runtimeItem);
        }

        // Equipped gear - resolve by instanceID from inventory
        ApplyEquippedGear(data.equippedGear, playerData);

        // Journal
        playerData.Journal.LoadFromSaveData(data.journalEntries);

        // Map
        playerData.Map.LoadFromSaveData(data.unlockedLocationIDs, data.locationFlags);
    }

    private static void ApplyEquippedGear(EquippedGearSaveData saveData, PlayerData playerData)
    {
        if (saveData == null) return;

        var inventory   = playerData.Inventory;
        var equipped    = playerData.EquippedGear;

        // Resolve each piece by instanceID
        if (!string.IsNullOrEmpty(saveData.rodBaseInstanceID))
        {
            var rod = inventory.GetAllRodBases().Find(r => r.instanceID == saveData.rodBaseInstanceID);
            if (rod != null) equipped.EquipRodBase(rod);
        }

        if (!string.IsNullOrEmpty(saveData.reelInstanceID))
        {
            var reel = inventory.GetAllReels().Find(r => r.instanceID == saveData.reelInstanceID);
            if (reel != null) equipped.EquipReel(reel);
        }

        if (!string.IsNullOrEmpty(saveData.lineInstanceID))
        {
            var line = inventory.GetAllFishingLines().Find(l => l.instanceID == saveData.lineInstanceID);
            if (line != null) equipped.EquipLine(line);
        }

        if (!string.IsNullOrEmpty(saveData.hookInstanceID))
        {
            var hook = inventory.GetAllFishingHooks().Find(h => h.instanceID == saveData.hookInstanceID);
            if (hook != null) equipped.EquipHook(hook);
        }

        // Bait by itemID
        if (!string.IsNullOrEmpty(saveData.baitItemID) && saveData.baitQuantity > 0)
        {
            var bait = inventory.GetAllBait().Find(b => b.itemID == saveData.baitItemID);
            if (bait != null) equipped.EquipBait(bait, saveData.baitQuantity);
        }
    }

    // ============================================
    // CONVERSION - RUNTIME → SAVE DATA
    // ============================================

    private static InventoryItemSaveData ConvertToSaveData(InventoryItem item)
    {
        InventoryItemSaveData save = item switch
        {
            FishInventoryItem fish => new FishItemSaveData
            {
                weightClassString = fish.weightClass.ToString(),
                speciesID         = fish.speciesID
            },
            MaterialInventoryItem => new MaterialItemSaveData(),
            RodBaseInventoryItem rod => new RodBaseItemSaveData
            {
                greenZoneEnd       = rod.greenZoneEnd,
                redZoneStart       = rod.redZoneStart,
                resistanceBonus    = rod.resistanceBonus,
                reelingPowerBonus  = rod.reelingPowerBonus,
                lineStabilityBonus = rod.lineStabilityBonus,
                luck               = rod.luck
            },
            ReelInventoryItem reel => new ReelItemSaveData
            {
                visibleButtonCount = reel.visibleButtonCount,
                resistanceBonus    = reel.resistanceBonus,
                reelingPowerBonus  = reel.reelingPowerBonus,
                lineStabilityBonus = reel.lineStabilityBonus,
                luck               = reel.luck
            },
            FishingLineInventoryItem line => new FishingLineItemSaveData
            {
                lineLength         = line.lineLength,
                resistanceBonus    = line.resistanceBonus,
                reelingPowerBonus  = line.reelingPowerBonus,
                lineStabilityBonus = line.lineStabilityBonus,
                luck               = line.luck
            },
            FishingHookInventoryItem hook => new FishingHookItemSaveData
            {
                preferredTypeStrings = hook.preferredTypes.ConvertAll(t => t.ToString()),
                typeWeightBonus      = hook.typeWeightBonus,
                baitSlots            = hook.baitSlots,
                resistanceBonus      = hook.resistanceBonus,
                reelingPowerBonus    = hook.reelingPowerBonus,
                lineStabilityBonus   = hook.lineStabilityBonus,
                luck                 = hook.luck
            },
            BaitInventoryItem bait => new BaitItemSaveData
            {
                baitTypeString = bait.baitType.ToString()
            },
            ConsumableInventoryItem consumable => new ConsumableItemSaveData
            {
                effectDescription = consumable.effectDescription
            },
            _ => null
        };

        if (save == null) return null;

        // Fill shared base fields
        save.itemID       = item.itemID;
        save.itemName     = item.itemName;
        save.instanceID   = item.instanceID;
        save.quantity     = item.quantity;
        save.rarityString     = item.rarity.ToString();
        save.itemType1String  = item.itemType1.ToString();
        save.itemType2String  = item.itemType2.ToString();
        save.description  = item.description;
        save.customName   = item.customName;
        save.isFavorite   = item.isFavorite;
        save.playerNotes  = item.playerNotes;
        save.dateObtained = item.dateObtained;

        return save;
    }

    private static EquippedGearSaveData ConvertEquippedGear(EquippedGearInventory equipped)
    {
        return new EquippedGearSaveData
        {
            rodBaseInstanceID = equipped.RodBase?.instanceID,
            reelInstanceID    = equipped.Reel?.instanceID,
            lineInstanceID    = equipped.Line?.instanceID,
            hookInstanceID    = equipped.Hook?.instanceID,
            baitItemID        = equipped.Bait?.itemID,
            baitQuantity      = equipped.BaitQuantity
        };
    }

    private static JournalEntrySaveData ConvertJournalEntry(JournalEntry entry)
    {
        var save = new JournalEntrySaveData
        {
            journalKey           = entry.journalKey,
            itemID               = entry.itemID,
            itemName             = entry.itemName,
            description          = entry.description,
            itemType1String      = entry.itemType1.ToString(),
            itemType2String      = entry.itemType2.ToString(),
            rarityString         = entry.rarity.ToString(),
            discovered           = entry.discovered,
            discoveredDate       = entry.discoveredDate,
            totalAcquired        = entry.totalAcquired,
            tracksWeightClass    = entry.tracksWeightClass,
            tracksCount          = entry.tracksCount,
            hasBiggestWeightClass = entry.hasBiggestWeightClass,
            biggestWeightClassString = entry.hasBiggestWeightClass
                ? entry.biggestWeightClass.ToString()
                : null
        };

        foreach (var acq in entry.acquisitions)
        {
            save.acquisitions.Add(new AcquisitionRecordSaveData
            {
                locationID       = acq.locationID,
                locationName     = acq.locationName,
                methodString     = acq.method.ToString(),
                timestamp        = acq.timestamp,
                hasWeightClass   = acq.hasWeightClass,
                weightClassString = acq.hasWeightClass ? acq.weightClass.ToString() : null
            });
        }

        return save;
    }

    // ============================================
    // CONVERSION - SAVE DATA → RUNTIME
    // ============================================

    private static InventoryItem ConvertToRuntimeItem(InventoryItemSaveData save)
    {
        // Parse shared enums
        Enum.TryParse(save.rarityString,    out ItemRarity rarity);
        Enum.TryParse(save.itemType1String, out ItemType type1);
        Enum.TryParse(save.itemType2String, out ItemType type2);

        InventoryItem item = save switch
        {
            FishItemSaveData fish => CreateFishItem(fish, rarity, type1, type2),
            MaterialItemSaveData => new MaterialInventoryItem(save.itemID, save.itemName,
                rarity, type1, type2, save.description, save.quantity),
            RodBaseItemSaveData rod => new RodBaseInventoryItem(save.itemID, save.itemName,
                rod.resistanceBonus, rod.reelingPowerBonus, rod.lineStabilityBonus, rod.luck,
                rod.greenZoneEnd, rod.redZoneStart),
            ReelItemSaveData reel => new ReelInventoryItem(save.itemID, save.itemName,
                reel.resistanceBonus, reel.reelingPowerBonus, reel.lineStabilityBonus, reel.luck,
                reel.visibleButtonCount),
            FishingLineItemSaveData line => new FishingLineInventoryItem(save.itemID, save.itemName,
                line.resistanceBonus, line.reelingPowerBonus, line.lineStabilityBonus, line.luck,
                line.lineLength),
            FishingHookItemSaveData hook => CreateHookItem(hook),
            BaitItemSaveData bait => CreateBaitItem(bait, save),
            ConsumableItemSaveData consumable => new ConsumableInventoryItem(save.itemID,
                save.itemName, null, save.description, consumable.effectDescription, save.quantity),
            _ => null
        };

        if (item == null) return null;

        // Restore shared runtime fields
        item.instanceID  = save.instanceID;
        item.customName  = save.customName;
        item.isFavorite  = save.isFavorite;
        item.playerNotes = save.playerNotes;
        item.dateObtained = save.dateObtained;
        // Note: icon (Sprite) is NOT restored here - UI must look it up from the item SO

        return item;
    }

    private static FishInventoryItem CreateFishItem(FishItemSaveData save, 
        ItemRarity rarity, ItemType type1, ItemType type2)
    {
        Enum.TryParse(save.weightClassString, out FishWeightClass weightClass);
        return new FishInventoryItem(save.itemID, save.itemName, rarity, type1, type2,
            save.description, save.quantity, weightClass, save.speciesID);
    }

    private static FishingHookInventoryItem CreateHookItem(FishingHookItemSaveData save)
    {
        List<ItemType> preferredTypes = new List<ItemType>();
        foreach (var t in save.preferredTypeStrings)
        {
            if (Enum.TryParse(t, out ItemType type))
                preferredTypes.Add(type);
        }

        return new FishingHookInventoryItem(save.itemID, save.itemName,
            save.resistanceBonus, save.reelingPowerBonus, save.lineStabilityBonus, save.luck,
            preferredTypes, save.typeWeightBonus, save.baitSlots);
    }

    private static BaitInventoryItem CreateBaitItem(BaitItemSaveData save, InventoryItemSaveData baseData)
    {
        Enum.TryParse(save.baitTypeString, out BaitType baitType);
        return new BaitInventoryItem(baseData.itemID, baseData.itemName, baitType,
            null, baseData.quantity, baseData.description);
    }

    // ============================================
    // HELPERS
    // ============================================

    public static bool SaveExists() => File.Exists(SaveFilePath);

    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveSystem] Save file deleted.");
        }
    }
}