using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Placed on each location pin GameObject inside the map Canvas.
/// Just holds the LocationData reference and handles click.
/// Position is set by dragging in the Scene view - no code needed.
/// </summary>
public class MapPin : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Location")]
    [Tooltip("Drag the LocationData ScriptableObject for this pin here")]
    public LocationData locationData;

    [Header("Visuals")]
    [SerializeField] private Image pinIcon;
    [SerializeField] private Image pinBackground;
    [SerializeField] private GameObject lockedOverlay;     // Semi-transparent lock icon shown when locked
    [SerializeField] private GameObject unmetOverlay;      // Shown when requirements not met

    [Header("Colors")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor   = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color unavailableColor = new Color(0.6f, 0.3f, 0.3f, 1f);

    // Set by MapManager
    private System.Action<MapPin> onPinClicked;
    
    public void Initialize(System.Action<MapPin> clickCallback)
    {
        onPinClicked = clickCallback;
        Refresh();
    }

    /// <summary>
    /// Call this whenever map state changes to update visuals.
    /// </summary>
    public void Refresh()
    {
        if (locationData == null) return;

        MapData mapData = GameManager._instance?.Map;
        if (mapData == null) return;

        bool isUnlocked  = locationData.IsUnlocked(mapData);
        bool reqsMet     = locationData.AreRequirementsMet(mapData);

        // Update icon
        if (pinIcon != null && locationData.locationIcon != null)
            pinIcon.sprite = locationData.locationIcon;

        // Update color
        if (pinBackground != null)
        {
            if (isUnlocked)
                pinBackground.color = unlockedColor;
            else if (reqsMet)
                pinBackground.color = unlockedColor; // Can be unlocked - show normal
            else
                pinBackground.color = lockedColor;   // Requirements not met
        }

        // Overlays
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked && !reqsMet);

        if (unmetOverlay != null)
            unmetOverlay.SetActive(false); // reserved for future use
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onPinClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.15f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (locationData == null)
            Debug.LogWarning($"[MapPin] {gameObject.name} has no LocationData assigned!", this);
    }
#endif
}