using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Confirmation popup shown before a trade is executed.
///
/// NAMING CONVENTIONS for child objects:
///   ConfirmTitle    — TextMeshProUGUI — "Confirm Trade"
///   ConfirmDetails  — TextMeshProUGUI — trade summary text
///   ConfirmButton   — Button          — "Yes, Trade"
///   CancelButton    — Button          — "Cancel"
/// </summary>
public class ShopConfirmPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI confirmTitle;
    [SerializeField] private TextMeshProUGUI confirmDetails;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private Button          cancelButton;

    private Action onConfirm;
    private Action onCancel;

    private void Awake()
    {
        AutoFindTMP(ref confirmTitle,   "ConfirmTitle");
        AutoFindTMP(ref confirmDetails, "ConfirmDetails");
        AutoFindBtn(ref confirmButton,  "ConfirmButton");
        AutoFindBtn(ref cancelButton,   "CancelButton");

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancelClicked);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the panel for the given trade state.
    /// </summary>
    public void Show(TradeRuntimeState state, Action confirm, Action cancel)
    {
        onConfirm = confirm;
        onCancel  = cancel;

        if (confirmTitle != null)
            confirmTitle.text = $"Confirm: {state.trade.tradeName}";

        if (confirmDetails != null)
            confirmDetails.text = BuildSummary(state.trade);

        gameObject.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        gameObject.SetActive(false);
        onConfirm?.Invoke();
    }

    private void OnCancelClicked()
    {
        gameObject.SetActive(false);
        onCancel?.Invoke();
    }

    private string BuildSummary(TradeData trade)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You give:");
        if (trade.giveMoney > 0) sb.AppendLine($"  • {trade.giveMoney}g");
        foreach (var e in trade.giveItems)
            sb.AppendLine($"  • {e.quantity}x {e.item?.itemName ?? "?"}");

        sb.AppendLine("\nYou receive:");
        if (trade.receiveMoney > 0) sb.AppendLine($"  • {trade.receiveMoney}g");
        foreach (var e in trade.receiveItems)
            sb.AppendLine($"  • {e.quantity}x {e.item?.itemName ?? "?"}");

        return sb.ToString();
    }

    private void AutoFindTMP(ref TextMeshProUGUI f, string n)
    {
        if (f != null) return;
        Transform t = transform.Find(n);
        if (t != null) f = t.GetComponent<TextMeshProUGUI>();
    }

    private void AutoFindBtn(ref Button f, string n)
    {
        if (f != null) return;
        Transform t = transform.Find(n);
        if (t != null) f = t.GetComponent<Button>();
    }
}