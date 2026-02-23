using System;
using UnityEngine;

/// <summary>
/// Central event hub for all fishing-related events
/// Uses static events for loose coupling between systems
/// </summary>
public static class FishingEvents
{
    // ============================================
    // FISHING SETUP & CASTING
    // ============================================
    
    /// <summary>
    /// Fired when player casts their fishing line
    /// Payload: fishing spot data, cast position
    /// </summary>
    public static event Action OnLineCast;
    
    /// <summary>
    /// Fired when bobber lands on water surface
    /// </summary>
    public static event Action OnBobberLanded;
    
    // ============================================
    // FISH BITE & MINIGAME START
    // ============================================
    
    /// <summary>
    /// Fired when a fish bites the bait
    /// Payload: The caught fish data
    /// </summary>
    public static event Action<CatchableItem> OnFishBite;
    
    /// <summary>
    /// Fired when the minigame starts
    /// Payload: fish, rod, pool data
    /// </summary>
    public static event Action<CatchableItem, EquippedGearInventory, FishingPool> OnMinigameStarted;
    
    // ============================================
    // DURING MINIGAME
    // ============================================
    
    /// <summary>
    /// Fired every frame during minigame when progress changes
    /// Payload: current fish progress (0 = caught, maxDistance = escaped)
    /// </summary>
    public static event Action<float, float> OnMinigameProgressChanged; // (currentProgress, maxProgress)
    
    /// <summary>
    /// Fired when player successfully reels (completes full rotation)
    /// </summary>
    public static event Action OnReelSuccess;
    
    /// <summary>
    /// Fired when player presses correct button in sequence
    /// Payload: button pressed, slot index
    /// </summary>
    public static event Action<ReelButton, int> OnCorrectInput;
    
    /// <summary>
    /// Fired when player presses wrong button
    /// Payload: button pressed, expected button
    /// </summary>
    public static event Action<ReelButton, ReelButton> OnWrongInput;
    
    // ============================================
    // MINIGAME END
    // ============================================
    
    /// <summary>
    /// Fired when player successfully catches the fish
    /// Payload: caught fish data
    /// </summary>
    public static event Action<CatchableItem> OnFishCaught;
    
    /// <summary>
    /// Fired when fish escapes (line breaks)
    /// Payload: escaped fish data
    /// </summary>
    public static event Action<CatchableItem> OnFishEscaped;
    
    /// <summary>
    /// Fired when minigame ends (regardless of outcome)
    /// </summary>
    public static event Action OnMinigameEnded;
    
    // ============================================
    // BAIT SYSTEM
    // ============================================
    
    /// <summary>
    /// Fired when player changes bait
    /// Payload: old bait, new bait
    /// </summary>
    public static event Action<BaitType, BaitType> OnBaitChanged;
    
    // ============================================
    // GEAR SYSTEM
    // ============================================

    /// <summary>
    /// Fired when player's equipped gear changes (need to rebuild fishing table)
    /// </summary>
    public static event Action OnEquippedGearChanged;
    
    // ============================================
    // INVOKE METHODS (Called by publishers)
    // ============================================
    
    public static void LineCast()
    {
        OnLineCast?.Invoke();
    }
    
    public static void EquippedGearChanged()
    {
        OnEquippedGearChanged?.Invoke();
    }
    
    public static void BobberLanded()
    {
        OnBobberLanded?.Invoke();
    }
    
    public static void FishBite(CatchableItem fish)
    {
        OnFishBite?.Invoke(fish);
    }
    
    public static void MinigameStarted(CatchableItem fish, EquippedGearInventory gear, FishingPool pool)
    {
        OnMinigameStarted?.Invoke(fish, gear, pool);
    }
    
    public static void MinigameProgressChanged(float currentProgress, float maxProgress)
    {
        OnMinigameProgressChanged?.Invoke(currentProgress, maxProgress);
    }
    
    public static void ReelSuccess()
    {
        OnReelSuccess?.Invoke();
    }
    
    public static void CorrectInput(ReelButton button, int slotIndex)
    {
        OnCorrectInput?.Invoke(button, slotIndex);
    }
    
    public static void WrongInput(ReelButton pressed, ReelButton expected)
    {
        OnWrongInput?.Invoke(pressed, expected);
    }
    
    public static void FishCaught(CatchableItem fish)
    {
        OnFishCaught?.Invoke(fish);
    }
    
    public static void FishEscaped(CatchableItem fish)
    {
        OnFishEscaped?.Invoke(fish);
    }
    
    public static void MinigameEnded()
    {
        OnMinigameEnded?.Invoke();
    }
    
    public static void BaitChanged(BaitType oldBait, BaitType newBait)
    {
        OnBaitChanged?.Invoke(oldBait, newBait);
    }
    
    // ============================================
    // UTILITY - Clear all events (for cleanup/testing)
    // ============================================
    
    /// <summary>
    /// Clear all event subscriptions (use carefully, mainly for cleanup)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnLineCast = null;
        OnBobberLanded = null;
        OnFishBite = null;
        OnMinigameStarted = null;
        OnMinigameProgressChanged = null;
        OnReelSuccess = null;
        OnCorrectInput = null;
        OnWrongInput = null;
        OnFishCaught = null;
        OnFishEscaped = null;
        OnMinigameEnded = null;
        OnBaitChanged = null;
    }
}