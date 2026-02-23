using UnityEngine;
using System.Collections;

public class Fishing3DVisualManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BobberController bobberController;
    [SerializeField] private Transform rodTipTransform;
    [SerializeField] private FishingLineController fishingLine;
    [SerializeField] private FishingSpotData currentFishingSpot;
    
    [Header("Timing")]
    [SerializeField] private float reelInDuration = 2f;
    [SerializeField] private float surfaceDuration = 1.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private CatchableItem currentFish;
    private float lastProgressValue = -1f;
    
    private void Awake()
    {
        if (currentFishingSpot == null)
            Debug.LogWarning("[Fishing3DVisualManager] No FishingSpotData assigned!");
    
        if (bobberController == null)
            Debug.LogWarning("[Fishing3DVisualManager] No BobberController assigned!");
    }
    
    private void OnEnable()
    {
        FishingEvents.OnLineCast += HandleLineCast;
        FishingEvents.OnMinigameStarted += HandleMinigameStarted;
        FishingEvents.OnMinigameProgressChanged += HandleProgressChanged;
        FishingEvents.OnFishCaught += HandleFishCaught;
        FishingEvents.OnFishEscaped += HandleFishEscaped;
    }
    
    private void OnDisable()
    {
        FishingEvents.OnLineCast -= HandleLineCast;
        FishingEvents.OnMinigameStarted -= HandleMinigameStarted;
        FishingEvents.OnMinigameProgressChanged -= HandleProgressChanged;
        FishingEvents.OnFishCaught -= HandleFishCaught;
        FishingEvents.OnFishEscaped -= HandleFishEscaped;
    }
    
    private void HandleLineCast()
    {
        if (currentFishingSpot == null || bobberController == null)
        {
            Debug.LogWarning("[Fishing3DVisualManager] Cannot cast - missing references!");
            return;
        }
        
        bobberController.ThrowToPoint(
            currentFishingSpot.bobberLandingPoint,
            currentFishingSpot.throwDuration
        );
        
        if (fishingLine != null)
        {
            fishingLine.SetBobberTransform(bobberController.transform);
            fishingLine.SetLineState(false);
            fishingLine.SetTensionVisual(0f);
        }
        
        bobberController.OnThrowComplete += HandleBobberLanded;
        
        if (showDebugLogs)
            Debug.Log("[Fishing3DVisualManager] Bobber thrown");
    }
    
    private void HandleBobberLanded()
    {
        bobberController.OnThrowComplete -= HandleBobberLanded;
        FishingEvents.BobberLanded();
        
        if (showDebugLogs)
            Debug.Log("[Fishing3DVisualManager] Bobber landed on water");
    }
    
    private void HandleMinigameStarted(CatchableItem fish, EquippedGearInventory gear, FishingPool pool)
    {
        currentFish = fish;
        
        float startingDistance = pool.maxFishingDistance * 0.5f;
        lastProgressValue = startingDistance;
    
        if (currentFishingSpot == null || bobberController == null) return;
    
        float startingNormalizedProgress = 0.5f;
    
        Vector3 initialSubmergedPosition = Vector3.Lerp(
            currentFishingSpot.underwaterStartPoint.position,
            currentFishingSpot.underwaterEndPoint.position,
            startingNormalizedProgress
        );
    
        GameObject tempTarget = new GameObject("TempSubmergTarget");
        tempTarget.transform.position = initialSubmergedPosition;
    
        bobberController.SubmergeToPoint(
            tempTarget.transform,
            currentFishingSpot.submergeDuration
        );
    
        Destroy(tempTarget, currentFishingSpot.submergeDuration + 0.1f);
    
        if (fishingLine != null)
        {
            fishingLine.SetLineState(true);
            fishingLine.SetTensionVisual(startingNormalizedProgress);
        }
    
        if (showDebugLogs)
            Debug.Log("[Fishing3DVisualManager] Minigame started - bobber at middle (50%)");
    }
    
    /// <summary>
    /// Handle distance changes from minigame.
    /// Distance: 0 = close (caught), max = far (escaped)
    /// </summary>
    private void HandleProgressChanged(float currentDistance, float maxDistance)
    {
        if (currentFishingSpot == null || bobberController == null) return;
    
        const float changeThreshold = 0.01f;
        if (Mathf.Abs(currentDistance - lastProgressValue) < changeThreshold)
            return;
    
        lastProgressValue = currentDistance;
    
        float normalizedDistance = currentDistance / maxDistance;
    
        bobberController.UpdateSubmergedPosition(
            currentFishingSpot.underwaterStartPoint.position,
            currentFishingSpot.underwaterEndPoint.position,
            normalizedDistance
        );
    
        if (fishingLine != null)
            fishingLine.SetTensionVisual(normalizedDistance);
    
        if (showDebugLogs)
            Debug.Log($"[Distance Changed] {currentDistance:F1}/{maxDistance:F1} = {normalizedDistance:F2}");
    }
    
    private void HandleFishCaught(CatchableItem fish)
    {
        if (bobberController == null) return;
    
        bobberController.ReelIn(reelInDuration);
    
        if (fishingLine != null)
            fishingLine.SetTensionVisual(1f);
    
        bobberController.OnReelInComplete += HandleReelInComplete;
    
        if (showDebugLogs)
            Debug.Log($"[Fishing3DVisualManager] Fish caught: {fish.itemName}");
    }
    
    private void HandleReelInComplete()
    {
        bobberController.OnReelInComplete -= HandleReelInComplete;
    
        if (fishingLine != null)
        {
            fishingLine.SetLineState(false);
            fishingLine.SetTensionVisual(0f);
        }
    
        currentFish = null;
        lastProgressValue = -1f;
    
        if (showDebugLogs)
            Debug.Log("[Fishing3DVisualManager] Bobber returned to initial position");
    }
    
    private void HandleFishEscaped(CatchableItem fish)
    {
        if (currentFishingSpot == null || bobberController == null) return;
        
        bobberController.SurfaceToPoint(
            currentFishingSpot.bobberLandingPoint,
            surfaceDuration
        );
        
        StartCoroutine(ReelBackAfterEscape());
        
        if (fishingLine != null)
        {
            fishingLine.SetLineState(false);
            fishingLine.SetTensionVisual(0f);
        }
        
        if (showDebugLogs)
            Debug.Log($"[Fishing3DVisualManager] Fish escaped: {fish.itemName}");
    }
    
    private IEnumerator ReelBackAfterEscape()
    {
        yield return new WaitForSeconds(surfaceDuration);
    
        if (bobberController != null)
        {
            bobberController.ReelIn(reelInDuration);
        
            bobberController.OnReelInComplete += () =>
            {
                currentFish = null;
                lastProgressValue = -1f;
            };
        }
    }
    
    public void SetFishingSpot(FishingSpotData newSpot)
    {
        currentFishingSpot = newSpot;
        
        if (showDebugLogs)
            Debug.Log($"[Fishing3DVisualManager] Fishing spot changed to: {newSpot.name}");
    }
}