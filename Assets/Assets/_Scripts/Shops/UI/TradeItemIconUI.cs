using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a single item/money slot icon inside a trade row.
///
/// NAMING CONVENTIONS for child objects:
///   ItemIcon     — Image           — the item sprite
///   QuantityText — TextMeshProUGUI — e.g. "x3" or "15g"
///   TooltipText  — TextMeshProUGUI — item name shown on hover (optional)
/// </summary>
public class TradeItemIconUI : MonoBehaviour
{
    [SerializeField] private Image           itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Tooltip("Sprite used for coin/money entries")]
    [SerializeField] private Sprite coinSprite;

    private void Awake()
    {
        AutoFind(ref itemIcon,     "ItemIcon");
        AutoFind(ref quantityText, "QuantityText");
        AutoFind(ref tooltipText,  "TooltipText");

        if (coinSprite == null)
            coinSprite = Resources.Load<Sprite>("Icons/CoinIcon");
    }

    /// <summary>Setup for an item slot.</summary>
    public void Setup(Sprite icon, string itemName, int qty)
    {
        if (itemIcon     != null) { itemIcon.sprite = icon; itemIcon.gameObject.SetActive(icon != null); }
        if (quantityText != null) quantityText.text = qty > 1 ? $"x{qty}" : "";
        if (tooltipText  != null) tooltipText.text  = itemName;
    }

    /// <summary>Setup for a money slot.</summary>
    public void SetupMoney(int amount)
    {
        if (itemIcon     != null) { itemIcon.sprite = coinSprite; itemIcon.gameObject.SetActive(true); }
        if (quantityText != null) quantityText.text = $"{amount}g";
        if (tooltipText  != null) tooltipText.text  = "Coins";
    }

    private void AutoFind<T>(ref T field, string childName) where T : Component
    {
        if (field != null) return;
        Transform t = transform.Find(childName);
        if (t != null) field = t.GetComponent<T>();
    }
}