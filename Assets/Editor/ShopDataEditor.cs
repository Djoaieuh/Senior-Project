#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom inspector for ShopData.
/// Gives designers a clean, readable interface with clear sections,
/// auto-generated trade IDs, and helpful validation warnings.
/// </summary>
[CustomEditor(typeof(ShopData))]
public class ShopDataEditor : Editor
{
    // Foldouts
    private bool showShopSettings    = true;
    private bool showVisibility      = true;
    private bool showTrades          = true;
    private readonly List<bool> tradeFoldouts = new List<bool>();

    // Styles (lazy init)
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle tradeBoxStyle;
    private GUIStyle warningStyle;
    private GUIStyle sectionStyle;

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = new Color(0.9f, 0.85f, 0.6f);

        subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11
        };
        subHeaderStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);

        tradeBoxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        warningStyle = new GUIStyle(EditorStyles.helpBox);

        sectionStyle = new GUIStyle(EditorStyles.foldoutHeader)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 12
        };
    }

    public override void OnInspectorGUI()
    {
        InitStyles();
        ShopData shop = (ShopData)target;

        serializedObject.Update();

        // ── Header banner ──────────────────────────────────────────────
        DrawBanner();

        EditorGUILayout.Space(6);

        // ── Shop Settings ──────────────────────────────────────────────
        showShopSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showShopSettings, "⚙  Shop Settings");
        if (showShopSettings)
        {
            EditorGUI.indentLevel++;
            DrawShopSettings(shop);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ── Visibility / Map Unlock ────────────────────────────────────
        showVisibility = EditorGUILayout.BeginFoldoutHeaderGroup(showVisibility, "👁  Shop Visibility on Map");
        if (showVisibility)
        {
            EditorGUI.indentLevel++;
            DrawConditionGroup(serializedObject.FindProperty("shopVisibilityConditions"),
                "When should this shop appear on the map?");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ── Trades ────────────────────────────────────────────────────
        showTrades = EditorGUILayout.BeginFoldoutHeaderGroup(showTrades, $"🤝  Trades  ({shop.trades.Count})");
        if (showTrades)
        {
            EditorGUI.indentLevel++;
            DrawTradesList(shop);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ── Validation ────────────────────────────────────────────────
        DrawValidation(shop);

        serializedObject.ApplyModifiedProperties();
    }

    // ══════════════════════════════════════════════════════════════════
    // BANNER
    // ══════════════════════════════════════════════════════════════════

    private void DrawBanner()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 36);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.2f));
        GUI.Label(new Rect(rect.x + 10, rect.y + 6, rect.width, rect.height),
            "🏪  SHOP DATA", headerStyle);
    }

    // ══════════════════════════════════════════════════════════════════
    // SHOP SETTINGS
    // ══════════════════════════════════════════════════════════════════

    private void DrawShopSettings(ShopData shop)
    {
        // LocationData base fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("locationID"),   new GUIContent("Location ID", "Unique ID — must match the MapPin setup"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("locationName"), new GUIContent("Location Name"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"),  new GUIContent("Map Description", "Short text shown on the map info panel"));

        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("shopName"),        new GUIContent("Shop Name",        "Header shown inside the shop UI"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shopDescription"), new GUIContent("Shop Description", "Subtitle inside the shop UI"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shopIcon"),        new GUIContent("Shop Icon",        "Portrait shown in the shop UI header"));

        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("locationIcon"),  new GUIContent("Map Pin Icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewImage"),  new GUIContent("Map Preview Image"));
    }

    // ══════════════════════════════════════════════════════════════════
    // TRADES LIST
    // ══════════════════════════════════════════════════════════════════

    private void DrawTradesList(ShopData shop)
    {
        while (tradeFoldouts.Count < shop.trades.Count)
            tradeFoldouts.Add(true);

        for (int i = 0; i < shop.trades.Count; i++)
        {
            TradeData trade = shop.trades[i];
            DrawTradeEntry(shop, trade, i);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("＋  Add New Trade", GUILayout.Height(30)))
        {
            Undo.RecordObject(target, "Add Trade");
            TradeData newTrade = new TradeData
            {
                tradeName = "New Trade",
                tradeID   = $"{shop.locationID}_trade_{shop.trades.Count}"
            };
            shop.trades.Add(newTrade);
            tradeFoldouts.Add(true);
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawTradeEntry(ShopData shop, TradeData trade, int index)
    {
        // Auto-generate trade ID if missing
        if (string.IsNullOrEmpty(trade.tradeID))
        {
            trade.tradeID = $"{shop.locationID}_{trade.tradeName}".Replace(" ", "_").ToLower();
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.BeginVertical(tradeBoxStyle);

        // ── Foldout header ────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        tradeFoldouts[index] = EditorGUILayout.Foldout(tradeFoldouts[index],
            $"  [{index + 1}]  {trade.tradeName}", true, sectionStyle);

        if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
        {
            if (EditorUtility.DisplayDialog("Remove Trade",
                $"Remove trade '{trade.tradeName}'?", "Remove", "Cancel"))
            {
                Undo.RecordObject(target, "Remove Trade");
                shop.trades.RemoveAt(index);
                tradeFoldouts.RemoveAt(index);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!tradeFoldouts[index])
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.indentLevel++;

        // ── Basic info ────────────────────────────────────────────────
        EditorGUILayout.Space(2);
        trade.tradeName        = EditorGUILayout.TextField(new GUIContent("Trade Name"), trade.tradeName);
        trade.tradeDescription = EditorGUILayout.TextField(new GUIContent("Description"), trade.tradeDescription);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Trade ID (auto): " + trade.tradeID, EditorStyles.miniLabel);

        EditorGUILayout.Space(6);

        // ── Give ──────────────────────────────────────────────────────
        DrawTradeSection("⬅  PLAYER GIVES", ref trade.giveMoney, trade.giveItems, "giveItems", index);

        EditorGUILayout.Space(4);

        // ── Receive ───────────────────────────────────────────────────
        DrawTradeSection("➡  PLAYER RECEIVES", ref trade.receiveMoney, trade.receiveItems, "receiveItems", index);

        EditorGUILayout.Space(6);

        // ── Unlock Conditions ─────────────────────────────────────────
        DrawTradeConditionGroup(trade, index);

        EditorGUILayout.Space(4);

        // ── Stock Limits ──────────────────────────────────────────────
        DrawStockLimits(trade);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawTradeSection(string label, ref int money, List<TradeItemEntry> items, string fieldName, int tradeIndex)
    {
        EditorGUILayout.LabelField(label, subHeaderStyle);

        EditorGUI.indentLevel++;

        money = EditorGUILayout.IntField(new GUIContent("💰 Coins"), money);
        if (money < 0) money = 0;

        EditorGUILayout.LabelField("Items:", EditorStyles.miniLabel);

        for (int i = 0; i < items.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            items[i].item     = (Item)EditorGUILayout.ObjectField(items[i].item, typeof(Item), false);
            items[i].quantity = EditorGUILayout.IntField(items[i].quantity, GUILayout.Width(50));
            if (items[i].quantity < 1) items[i].quantity = 1;
            EditorGUILayout.LabelField("x", GUILayout.Width(14));

            if (GUILayout.Button("✕", GUILayout.Width(20)))
            {
                Undo.RecordObject(target, "Remove Item");
                items.RemoveAt(i);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Item", EditorStyles.miniButton))
        {
            Undo.RecordObject(target, "Add Item");
            items.Add(new TradeItemEntry { quantity = 1 });
            EditorUtility.SetDirty(target);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawTradeConditionGroup(TradeData trade, int tradeIndex)
    {
        EditorGUILayout.LabelField("🔒  Unlock Conditions", subHeaderStyle);
        EditorGUI.indentLevel++;
        DrawConditionGroupDirect(trade.unlockConditions, "When is this trade visible?");
        EditorGUI.indentLevel--;
    }

    private void DrawStockLimits(TradeData trade)
    {
        EditorGUILayout.LabelField("📦  Stock Limits", subHeaderStyle);
        EditorGUI.indentLevel++;

        trade.hasStockLimit = EditorGUILayout.Toggle(
            new GUIContent("Has Stock Limit", "Enable if this trade can run out of stock"),
            trade.hasStockLimit);

        if (trade.hasStockLimit)
        {
            trade.stockLimit = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Stock Amount", "How many times before it runs out"),
                trade.stockLimit));

            trade.restockCooldownSeconds = Mathf.Max(1f, EditorGUILayout.FloatField(
                new GUIContent("Restock After (seconds)",
                    "3600 = 1 hour, 86400 = 1 day. Timer starts on first purchase."),
                trade.restockCooldownSeconds));

            float hours = trade.restockCooldownSeconds / 3600f;
            EditorGUILayout.LabelField($"  = {hours:F1} hours", EditorStyles.miniLabel);
        }

        EditorGUI.indentLevel--;
    }

    // ══════════════════════════════════════════════════════════════════
    // CONDITION GROUP DRAWER
    // ══════════════════════════════════════════════════════════════════

    private void DrawConditionGroup(SerializedProperty groupProp, string hint)
    {
        if (groupProp == null) return;

        EditorGUILayout.HelpBox(hint, MessageType.None);

        SerializedProperty logicProp      = groupProp.FindPropertyRelative("logic");
        SerializedProperty conditionsList = groupProp.FindPropertyRelative("conditions");

        EditorGUILayout.PropertyField(logicProp, new GUIContent("Combine With"));

        for (int i = 0; i < conditionsList.arraySize; i++)
        {
            DrawConditionElement(conditionsList, i);
        }

        if (GUILayout.Button("+ Add Condition", EditorStyles.miniButton))
            conditionsList.InsertArrayElementAtIndex(conditionsList.arraySize);

        if (conditionsList.arraySize == 0)
            EditorGUILayout.LabelField("  (no conditions — always visible)", EditorStyles.miniLabel);
    }

    /// <summary>Direct draw version for non-serialized objects (TradeData is [Serializable] not SO).</summary>
    private void DrawConditionGroupDirect(TradeUnlockConditionGroup group, string hint)
    {
        EditorGUILayout.HelpBox(hint, MessageType.None);

        group.logic = (ConditionGroupLogic)EditorGUILayout.EnumPopup("Combine With", group.logic);

        for (int i = 0; i < group.conditions.Count; i++)
        {
            DrawConditionDirect(group.conditions, i);
        }

        if (GUILayout.Button("+ Add Condition", EditorStyles.miniButton))
        {
            Undo.RecordObject(target, "Add Condition");
            group.conditions.Add(new ShopUnlockCondition());
            EditorUtility.SetDirty(target);
        }

        if (group.conditions.Count == 0)
            EditorGUILayout.LabelField("  (no conditions — always visible)", EditorStyles.miniLabel);
    }

    private void DrawConditionElement(SerializedProperty list, int i)
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
        if (GUILayout.Button("✕", GUILayout.Width(20)))
            list.DeleteArrayElementAtIndex(i);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawConditionDirect(List<ShopUnlockCondition> list, int i)
    {
        EditorGUILayout.BeginVertical("box");
        ShopUnlockCondition cond = list[i];

        cond.type = (ShopConditionType)EditorGUILayout.EnumPopup("Type", cond.type);

        switch (cond.type)
        {
            case ShopConditionType.LocationUnlocked:
                cond.requiredLocationID = EditorGUILayout.TextField("Location ID", cond.requiredLocationID);
                break;
            case ShopConditionType.HasItem:
                cond.requiredItem         = (Item)EditorGUILayout.ObjectField("Item", cond.requiredItem, typeof(Item), false);
                cond.requiredItemQuantity = Mathf.Max(1, EditorGUILayout.IntField("Quantity", cond.requiredItemQuantity));
                break;
            case ShopConditionType.TotalTradesInShop:
                cond.requiredTradeCount = Mathf.Max(1, EditorGUILayout.IntField("Min Trades Done", cond.requiredTradeCount));
                break;
            case ShopConditionType.SpecificTradeCount:
                cond.requiredTradeID            = EditorGUILayout.TextField("Trade ID", cond.requiredTradeID);
                cond.requiredSpecificTradeCount = Mathf.Max(1, EditorGUILayout.IntField("Min Times Done", cond.requiredSpecificTradeCount));
                break;
            case ShopConditionType.QuestFlag:
                cond.questFlagLocationID = EditorGUILayout.TextField("Location ID (blank=global)", cond.questFlagLocationID);
                cond.questFlagName       = EditorGUILayout.TextField("Flag Name", cond.questFlagName);
                break;
            case ShopConditionType.HasMinimumMoney:
                cond.requiredMoney = Mathf.Max(0, EditorGUILayout.IntField("Min Coins", cond.requiredMoney));
                break;
        }

        EditorGUILayout.LabelField("→ " + cond.GetDescription(), EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            Undo.RecordObject(target, "Remove Condition");
            list.RemoveAt(i);
            EditorUtility.SetDirty(target);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ══════════════════════════════════════════════════════════════════
    // VALIDATION
    // ══════════════════════════════════════════════════════════════════

    private void DrawValidation(ShopData shop)
    {
        List<string> warnings = new List<string>();

        if (string.IsNullOrEmpty(shop.locationID))
            warnings.Add("⚠ Location ID is empty! Set a unique ID.");

        if (string.IsNullOrEmpty(shop.shopName))
            warnings.Add("⚠ Shop Name is empty.");

        foreach (var trade in shop.trades)
        {
            if (!trade.IsValid())
                warnings.Add($"⚠ Trade '{trade.tradeName}' has nothing on one side — check give/receive.");
        }

        if (warnings.Count > 0)
        {
            EditorGUILayout.Space(4);
            foreach (var w in warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
        }
    }
}
#endif