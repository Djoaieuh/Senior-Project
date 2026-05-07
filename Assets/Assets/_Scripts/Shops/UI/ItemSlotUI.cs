using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single icon + quantity label.
/// Used for both item slots and money slots inside a trade row.
/// Prefab: ItemSlotPrefab
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [Header("References")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    /// <summary>
    /// Sets the icon and label.
    /// label examples: "x3", "30", "x1"
    /// </summary>
    public void Setup(Sprite sprite, string label)
    {
        if (sprite != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.color  = Color.white;
        }
        else
        {
            // No sprite found — show a blank/question-mark tint so it's obvious
            itemIcon.color = new Color(1f, 1f, 1f, 0.25f);
        }

        quantityText.text = label;
    }
}