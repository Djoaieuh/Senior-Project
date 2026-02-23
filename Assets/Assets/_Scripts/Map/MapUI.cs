using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Drives the map canvas UI.
/// Finds all MapPin children automatically - just place pins in the hierarchy.
/// </summary>
public class MapUI : MonoBehaviour
{
    // ============================================
    // INSPECTOR REFERENCES
    // ============================================

    [Header("Pins Container")]
    [Tooltip("The parent GameObject that contains all MapPin children")]
    [SerializeField] private Transform pinsContainer;

    [Header("Location Info Panel")]
    [Tooltip("The panel shown when a pin is clicked")]
    [SerializeField] private GameObject infoPanel;

    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private TextMeshProUGUI locationTypeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI requirementsText;
    [SerializeField] private Image previewImage;

    [Header("Info Panel Buttons")]
    [SerializeField] private Button travelButton;
    [SerializeField] private TextMeshProUGUI travelButtonText;
    [SerializeField] private Button closeInfoButton;

    [Header("Map Close Button")]
    [SerializeField] private Button closeMapButton;

    [Header("Current Location Indicator")]
    [Tooltip("Text showing where the player currently is")]
    [SerializeField] private TextMeshProUGUI currentLocationText;

    // ============================================
    // RUNTIME STATE
    // ============================================

    private List<MapPin> allPins = new List<MapPin>();
    private MapPin selectedPin;

    // ============================================
    // LIFECYCLE
    // ============================================

    private void Awake()
    {
        DiscoverPins();
        SetupButtons();

        // Start with info panel hidden
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void DiscoverPins()
    {
        allPins.Clear();

        Transform container = pinsContainer != null ? pinsContainer : transform;
        MapPin[] found = container.GetComponentsInChildren<MapPin>(includeInactive: true);

        foreach (var pin in found)
        {
            pin.Initialize(OnPinClicked);
            allPins.Add(pin);
        }

        Debug.Log($"[MapUI] Found {allPins.Count} map pins");
    }

    private void SetupButtons()
    {
        if (travelButton != null)
            travelButton.onClick.AddListener(OnTravelClicked);

        if (closeInfoButton != null)
            closeInfoButton.onClick.AddListener(CloseInfoPanel);

        if (closeMapButton != null)
            closeMapButton.onClick.AddListener(() => MapManager._instance?.CloseMap());
    }

    // ============================================
    // REFRESH
    // ============================================

    /// <summary>
    /// Called by MapManager when the map is opened - refreshes all pin visuals.
    /// </summary>
    public void RefreshAll()
    {
        foreach (var pin in allPins)
            pin.Refresh();

        UpdateCurrentLocationText();

        // Close info panel on re-open
        CloseInfoPanel();
    }

    private void UpdateCurrentLocationText()
    {
        if (currentLocationText == null) return;

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string locationName = "Unknown";

        // Find which pin matches the current scene
        foreach (var pin in allPins)
        {
            if (pin.locationData != null && pin.locationData.sceneName == currentScene)
            {
                locationName = pin.locationData.locationName;
                break;
            }
        }

        currentLocationText.text = $"Currently at: {locationName}";
    }

    // ============================================
    // PIN CLICK
    // ============================================

    private void OnPinClicked(MapPin pin)
    {
        selectedPin = pin;
        ShowInfoPanel(pin.locationData);
    }

    private void ShowInfoPanel(LocationData location)
    {
        if (location == null || infoPanel == null) return;

        infoPanel.SetActive(true);

        MapData mapData = GameManager._instance?.Map;

        // Name
        if (locationNameText != null)
            locationNameText.text = location.locationName;

        // Type label
        if (locationTypeText != null)
        {
            locationTypeText.text = location.locationType switch
            {
                LocationType.FishingSpot => "⚓ Fishing Spot",
                LocationType.Shop        => "🛒 Shop",
                LocationType.Basic       => "💬 Location",
                _ => ""
            };
        }

        // Description
        if (descriptionText != null)
            descriptionText.text = location.description;

        // Preview image
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(location.previewImage != null);
            if (location.previewImage != null)
                previewImage.sprite = location.previewImage;
        }

        // Requirements text
        if (requirementsText != null)
        {
            bool isUnlocked = mapData != null && location.IsUnlocked(mapData);

            if (isUnlocked)
            {
                requirementsText.gameObject.SetActive(false);
            }
            else
            {
                requirementsText.gameObject.SetActive(true);
                var reqs = location.GetRequirementDescriptions();
                requirementsText.text = reqs.Count > 0
                    ? "Requires:\n" + string.Join("\n", reqs)
                    : "Locked";
            }
        }

        // Travel button
        UpdateTravelButton(location, mapData);
    }

    private void UpdateTravelButton(LocationData location, MapData mapData)
    {
        if (travelButton == null) return;

        bool isUnlocked  = mapData != null && location.IsUnlocked(mapData);
        bool reqsMet     = mapData != null && location.AreRequirementsMet(mapData);
        bool isCurrentScene = location.sceneName ==
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Button interactability
        bool canTravel = (isUnlocked || reqsMet) && !isCurrentScene;
        travelButton.interactable = canTravel;

        // Button label
        if (travelButtonText != null)
        {
            if (isCurrentScene)
                travelButtonText.text = "You are here";
            else if (isUnlocked)
                travelButtonText.text = GetTravelLabel(location.locationType);
            else if (reqsMet)
                travelButtonText.text = GetUnlockLabel(location);
            else
                travelButtonText.text = "Locked";
        }
    }

    private string GetTravelLabel(LocationType type) => type switch
    {
        LocationType.FishingSpot => "Travel",
        LocationType.Shop        => "Visit Shop",
        LocationType.Basic       => "Visit",
        _ => "Go"
    };

    private string GetUnlockLabel(LocationData location)
    {
        // Show cost if there's a money requirement
        foreach (var req in location.unlockRequirements)
        {
            if (req.type == UnlockRequirementType.MoneyCost && req.moneyCost > 0)
                return $"Unlock ({req.moneyCost}g)";
        }
        return "Unlock";
    }

    // ============================================
    // BUTTONS
    // ============================================

    private void OnTravelClicked()
    {
        if (selectedPin?.locationData == null) return;
        MapManager._instance?.TravelTo(selectedPin.locationData);
    }

    private void CloseInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        selectedPin = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Rediscover Pins")]
    private void DebugRediscoverPins()
    {
        DiscoverPins();
        Debug.Log($"[MapUI] Rediscovered {allPins.Count} pins");
    }
#endif
}