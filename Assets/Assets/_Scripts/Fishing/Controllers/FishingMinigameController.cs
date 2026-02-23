using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Single distance system - fish naturally drifts away, correct inputs pull fish closer
/// </summary>
public class FishingMinigameController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject minigameCanvas;
    
    [Header("References")]
    [SerializeField] private RectTransform distanceBar;
    [SerializeField] private RectTransform fishIcon;
    [SerializeField] private RectTransform reelHandle;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Button Display")]
    [SerializeField] private Image[] buttonDisplaySlots = new Image[8];
    [SerializeField] private Sprite button1Sprite;
    [SerializeField] private Sprite button2Sprite;
    [SerializeField] private Sprite button3Sprite;
    [SerializeField] private Sprite button4Sprite;
    
    [Header("Button Layout Settings")]
    [SerializeField] private float buttonCircleRadius = 150f;
    
    [Header("Visual Feedback Settings")]
    [SerializeField] private float inactiveButtonScale = 0.8f;
    [SerializeField] private float inactiveButtonAlpha = 0.5f;
    
    // Scrambling system
    private Queue<ReelButton> scrambledQueue = new Queue<ReelButton>();
    private float scrambleChance = 0f;
    private int rhythmIndex = 0;
    
    private ButtonLayoutManager layoutManager;
    private int correctInputsInRotation = 0;
    
    [Header("Handle Rotation Settings")]
    [SerializeField] private float baseHandleRotationDuration = 0.2f;
    [SerializeField] private float speedBoostMultiplier = 1.2f;
    [SerializeField] private float speedResetDelay = 0.5f;
    
    private Queue<int> bufferedSlots = new Queue<int>();
    private float currentSpeedMultiplier = 1f;
    private float speedResetTimer = 0f;
    private bool isWaitingToResetSpeed = false;
    
    [Header("Input")]
    [SerializeField] private InputActionReference button1Action;
    [SerializeField] private InputActionReference button2Action;
    [SerializeField] private InputActionReference button3Action;
    [SerializeField] private InputActionReference button4Action;
    
    [Header("Pull Mechanic")]
    [Tooltip("TRUE = Pull on every correct input | FALSE = Pull only on full rotation")]
    [SerializeField] private bool pullOnEveryInput = false;
    
    [Tooltip("How much distance decreases per pull")]
    [SerializeField] private float pullAmount = 8f;
    
    [Tooltip("How long the pull tween takes (seconds)")]
    [SerializeField] private float pullTweenDuration = 0.15f;
    
    [Header("Wrong Input Penalty")]
    [Tooltip("Duration player is locked out after wrong input (seconds)")]
    [SerializeField] private float wrongInputLockoutDuration = 1f;
    
    private bool isLockedOut = false;
    private float lockoutTimer = 0f;
    
    [Header("Drift Settings")]
    [Tooltip("Base drift speed when fish strength equals rod resistance")]
    [SerializeField] private float baseDriftSpeed = 10f;
    
    [Tooltip("Multiplier applied to the strength/resistance ratio")]
    [SerializeField] private float driftSpeedMultiplier = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool enableTweening = true;
    
    // GAME STATE
    private float fishDistance;
    private float visualFishDistance;
    
    // Current minigame data
    private CatchableItem currentFish;
    private EquippedGearInventory currentEquipment;
    private FishingPool currentPool;
    
    // Reel sequence and slot tracking
    private ReelSequence reelSequence;
    private int currentSlotIndex = 0;
    private int visibleSlotCount = 0;
    
    // Calculated values
    private float calculatedDriftSpeed;
    private float maxFishingDistance;
    
    // Cached values
    private float distanceBarWidth;
    private RectTransform distanceBarRect;
    
    // State
    private bool isMinigameActive = false;
    
    // Tween tracking
    private Tween currentPullTween;
    private Tween currentHandleRotationTween;
    
    // Progress event throttle
    private float lastNotifiedDistance = -1f;
    private const float progressNotifyThreshold = 0.5f;
    
    private void Awake()
    {
        if (distanceBar != null)
            distanceBarRect = distanceBar;
        else
            distanceBarRect = fishIcon.parent.GetComponent<RectTransform>();

        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);
    
        if (buttonDisplaySlots != null)
        {
            foreach (Image slot in buttonDisplaySlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }
    
        RectTransform[] slotTransforms = new RectTransform[buttonDisplaySlots.Length];
        for (int i = 0; i < buttonDisplaySlots.Length; i++)
        {
            if (buttonDisplaySlots[i] != null)
                slotTransforms[i] = buttonDisplaySlots[i].rectTransform;
        }
    
        layoutManager = new ButtonLayoutManager(slotTransforms, buttonCircleRadius);
    }
    
    private void OnEnable()
    {
        if (button1Action != null) { button1Action.action.Enable(); button1Action.action.performed += OnButton1Performed; }
        if (button2Action != null) { button2Action.action.Enable(); button2Action.action.performed += OnButton2Performed; }
        if (button3Action != null) { button3Action.action.Enable(); button3Action.action.performed += OnButton3Performed; }
        if (button4Action != null) { button4Action.action.Enable(); button4Action.action.performed += OnButton4Performed; }
    }
    
    private void OnDisable()
    {
        if (button1Action != null) { button1Action.action.performed -= OnButton1Performed; button1Action.action.Disable(); }
        if (button2Action != null) { button2Action.action.performed -= OnButton2Performed; button2Action.action.Disable(); }
        if (button3Action != null) { button3Action.action.performed -= OnButton3Performed; button3Action.action.Disable(); }
        if (button4Action != null) { button4Action.action.performed -= OnButton4Performed; button4Action.action.Disable(); }
        
        currentPullTween?.Kill();
        currentHandleRotationTween?.Kill();
    }
    
    private void OnButton1Performed(InputAction.CallbackContext context) { if (isMinigameActive && !isLockedOut) CheckReelInput(ReelButton.Button1); }
    private void OnButton2Performed(InputAction.CallbackContext context) { if (isMinigameActive && !isLockedOut) CheckReelInput(ReelButton.Button2); }
    private void OnButton3Performed(InputAction.CallbackContext context) { if (isMinigameActive && !isLockedOut) CheckReelInput(ReelButton.Button3); }
    private void OnButton4Performed(InputAction.CallbackContext context) { if (isMinigameActive && !isLockedOut) CheckReelInput(ReelButton.Button4); }
    
    private void CheckReelInput(ReelButton pressedButton)
    {
        if (scrambledQueue.Count == 0)
        {
            Debug.LogWarning("Scrambled queue is empty!");
            return;
        }
        
        ReelButton expectedButton = GetButtonForSlot(currentSlotIndex);
        
        if (pressedButton == expectedButton)
        {
            if (isWaitingToResetSpeed)
            {
                isWaitingToResetSpeed = false;
                speedResetTimer = 0f;
            }
            
            if (showDebugLogs)
                Debug.Log($"[CORRECT!] Pressed {pressedButton} in Slot {currentSlotIndex + 1}");
            
            FishingEvents.CorrectInput(pressedButton, currentSlotIndex);
            
            scrambledQueue.Dequeue();
            
            IReadOnlyList<ReelButton> baseRhythm = currentFish.GetReelSequence();
            ReelButton baseButton = baseRhythm[rhythmIndex % baseRhythm.Count];
            ReelButton newScrambled = ApplyScrambling(baseButton);
            scrambledQueue.Enqueue(newScrambled);
            rhythmIndex++;
            
            int nextSlotIndex = (currentSlotIndex + 1) % visibleSlotCount;
            RotateHandleToSlot(nextSlotIndex);
            
            correctInputsInRotation++;
            
            if (pullOnEveryInput)
            {
                PullFish();
                
                if (showDebugLogs)
                    Debug.Log($"[PULL!] Pulled fish closer ({correctInputsInRotation}/{visibleSlotCount} in rotation)");
            }
            else
            {
                if (correctInputsInRotation >= visibleSlotCount)
                {
                    PullFish();
                    correctInputsInRotation = 0;
                    FishingEvents.ReelSuccess();
                    
                    if (showDebugLogs)
                        Debug.Log("[FULL ROTATION COMPLETE!] Pulled fish closer!");
                }
            }
            
            currentSlotIndex = nextSlotIndex;
            
            int filledSlotIndex = (currentSlotIndex - 1 + visibleSlotCount) % visibleSlotCount;
            UpdateSingleSlot(filledSlotIndex);
            UpdateSlotVisualFeedback();
            
            if (showDebugLogs)
                Debug.Log($"[Next Slot] Slot {currentSlotIndex + 1} - Press: {GetButtonForSlot(currentSlotIndex)} ({correctInputsInRotation}/{visibleSlotCount})");
        }
        else
        {
            FishingEvents.WrongInput(pressedButton, expectedButton);
            
            isLockedOut = true;
            lockoutTimer = wrongInputLockoutDuration;
            
            if (showDebugLogs)
                Debug.Log($"[WRONG!] Pressed {pressedButton}, expected {expectedButton}. Locked out for {wrongInputLockoutDuration}s");
        }
    }
    
    private ReelButton GetButtonForSlot(int slotIndex)
    {
        ReelButton[] queueArray = scrambledQueue.ToArray();
        int queueIndex = (slotIndex - currentSlotIndex + visibleSlotCount) % visibleSlotCount;
    
        if (queueIndex >= 0 && queueIndex < queueArray.Length)
            return queueArray[queueIndex];
    
        Debug.LogWarning($"Queue index {queueIndex} out of range!");
        return ReelButton.Button1;
    }
    
    private void UpdateAllSlotDisplays()
    {
        for (int i = 0; i < visibleSlotCount; i++)
            UpdateSingleSlot(i);
    }
    
    private void RotateHandleToSlot(int slotIndex)
    {
        if (reelHandle == null || buttonDisplaySlots[slotIndex] == null) return;
        
        if (!enableTweening)
        {
            float targetAngle = GetSlotAngle(slotIndex);
            reelHandle.rotation = Quaternion.Euler(0, 0, targetAngle);
            return;
        }
        
        if (currentHandleRotationTween != null && currentHandleRotationTween.IsActive())
        {
            bufferedSlots.Enqueue(slotIndex);
            currentSpeedMultiplier *= speedBoostMultiplier;
            isWaitingToResetSpeed = false;
            speedResetTimer = 0f;
            
            if (showDebugLogs)
                Debug.Log($"[Buffer] Slot {slotIndex} buffered | Speed: {currentSpeedMultiplier:F2}x");
            
            return;
        }
        
        StartRotationToSlot(slotIndex);
    }

    private void StartRotationToSlot(int slotIndex)
    {
        float currentAngle = reelHandle.localEulerAngles.z;
        float targetAngle = GetSlotAngle(slotIndex);
    
        currentAngle = currentAngle % 360f;
        if (currentAngle < 0) currentAngle += 360f;
    
        targetAngle = targetAngle % 360f;
        if (targetAngle < 0) targetAngle += 360f;
    
        float clockwiseDistance = targetAngle - currentAngle;
        if (clockwiseDistance > 0)
            clockwiseDistance -= 360f;
    
        float finalTarget = currentAngle + clockwiseDistance;
        float duration = baseHandleRotationDuration / currentSpeedMultiplier;
    
        currentHandleRotationTween?.Kill();
    
        currentHandleRotationTween = reelHandle.DORotate(
                new Vector3(0, 0, finalTarget),
                duration,
                RotateMode.Fast
            )
            .SetEase(Ease.Linear)
            .OnComplete(OnRotationComplete);
    }

    private void OnRotationComplete()
    {
        if (bufferedSlots.Count > 0)
        {
            int nextSlot = bufferedSlots.Dequeue();
            StartRotationToSlot(nextSlot);
        }
        else
        {
            isWaitingToResetSpeed = true;
            speedResetTimer = speedResetDelay;
        }
    }

    private float GetSlotAngle(int slotIndex)
    {
        switch (visibleSlotCount)
        {
            case 2: return slotIndex == 0 ? 0f : -180f;
            case 3: float[] angles3 = { 0f, -120f, -240f }; return angles3[slotIndex];
            case 4: float[] angles4 = { 0f, -90f, -180f, -270f }; return angles4[slotIndex];
            case 5: float[] angles5 = { 0f, -72f, -144f, -216f, -288f }; return angles5[slotIndex];
            case 6: float[] angles6 = { 0f, -60f, -120f, -180f, -240f, -300f }; return angles6[slotIndex];
            case 7: float[] angles7 = { 0f, -51.43f, -102.86f, -154.29f, -205.71f, -257.14f, -308.57f }; return angles7[slotIndex];
            case 8: float[] angles8 = { 0f, -45f, -90f, -135f, -180f, -225f, -270f, -315f }; return angles8[slotIndex];
            default: return 0f;
        }
    }
    
    private void ApplyButtonLayout(int slotCount)
    {
        layoutManager.ApplyLayout(slotCount);
    }

    private Sprite GetSpriteForButton(ReelButton button)
    {
        switch (button)
        {
            case ReelButton.Button1: return button1Sprite;
            case ReelButton.Button2: return button2Sprite;
            case ReelButton.Button3: return button3Sprite;
            case ReelButton.Button4: return button4Sprite;
            default: return null;
        }
    }
    
    public void StartMinigame(CatchableItem caughtFish, EquippedGearInventory equippedGear, FishingPool fishingPool)
    {
        if (caughtFish == null) { Debug.LogError("No fish data provided!"); return; }
        if (equippedGear == null || !equippedGear.IsComplete()) { Debug.LogError("No fishing gear equipped or gear is incomplete!"); return; }
        if (fishingPool == null) { Debug.LogError("No fishing pool data provided!"); return; }
        
        currentFish = caughtFish;
        currentEquipment = equippedGear;
        currentPool = fishingPool;
        
        maxFishingDistance = fishingPool.maxFishingDistance;
        float startingDistance = maxFishingDistance * 0.5f;
        
        visibleSlotCount = Mathf.Clamp(currentEquipment.GetVisibleButtonCount(), 1, 8);
        
        IReadOnlyList<ReelButton> fishSequence = caughtFish.GetReelSequence();
        if (fishSequence == null || fishSequence.Count == 0)
        {
            Debug.LogError($"Fish {caughtFish.itemName} has no reel sequence defined!");
            return;
        }
        
        reelSequence = new ReelSequence(new List<ReelButton>(fishSequence));
        currentSlotIndex = 0;
        
        CalculateMinigameStats();
        
        bufferedSlots.Clear();
        currentSpeedMultiplier = 1f;
        isWaitingToResetSpeed = false;
        speedResetTimer = 0f;
        
        fishDistance = startingDistance;
        visualFishDistance = startingDistance;
        lastNotifiedDistance = -1f;
        isMinigameActive = true;
        isLockedOut = false;
        lockoutTimer = 0f;
        
        scrambledQueue.Clear();
        rhythmIndex = 0;
        
        if (currentFish.fishAgitation > currentEquipment.GetTotalLineStability())
        {
            scrambleChance = (currentFish.fishAgitation / currentEquipment.GetTotalLineStability()) - 1f;
            scrambleChance = Mathf.Clamp01(scrambleChance);
        }
        else
        {
            scrambleChance = 0f;
        }

        IReadOnlyList<ReelButton> baseRhythm = currentFish.GetReelSequence();
        for (int i = 0; i < visibleSlotCount; i++)
        {
            ReelButton baseButton = baseRhythm[rhythmIndex % baseRhythm.Count];
            ReelButton scrambled = ApplyScrambling(baseButton);
            scrambledQueue.Enqueue(scrambled);
            rhythmIndex++;
        }
        
        if (showDebugLogs)
            Debug.Log($"[Scramble] Chance: {scrambleChance * 100f:F1}%");
        
        ApplyButtonLayout(visibleSlotCount);
        
        if (minigameCanvas != null)
            minigameCanvas.SetActive(true);
        
        distanceBarWidth = distanceBarRect.rect.width;
        
        UpdateAllSlotDisplays();
        UpdateSlotVisualFeedback();
        correctInputsInRotation = 0;
        
        if (reelHandle != null)
        {
            float initialAngle = GetSlotAngle(0);
            reelHandle.rotation = Quaternion.Euler(0, 0, initialAngle);
        }
        
        FishingEvents.MinigameStarted(caughtFish, equippedGear, fishingPool);
        UpdateFishPosition();
        
        if (showDebugLogs)
        {
            Debug.Log($"[Minigame Started]");
            Debug.Log($"Fish: {caughtFish.itemName}");
            Debug.Log($"Drift Speed: {calculatedDriftSpeed:F2} | Max Distance: {maxFishingDistance} | Starting: {startingDistance}");
            Debug.Log($"Pull Mode: {(pullOnEveryInput ? "Every Input" : "Full Rotation")} | Pull Amount: {pullAmount}");
            Debug.Log($"Visible Slots: {visibleSlotCount}");
        }
    }
    
    private void CalculateMinigameStats()
    {
        float strengthRatio = currentFish.fishStrength / currentEquipment.GetTotalRodResistance();
        calculatedDriftSpeed = baseDriftSpeed * strengthRatio * driftSpeedMultiplier;
    }
    
    /// <summary>
    /// Pull fish closer (decreases distance)
    /// </summary>
    private void PullFish()
    {
        float adjustedPullAmount = (currentEquipment.GetTotalReelingPower() / currentFish.fishResistance) * pullAmount;
    
        fishDistance -= adjustedPullAmount;
        fishDistance = Mathf.Clamp(fishDistance, 0f, maxFishingDistance);
    
        if (enableTweening)
        {
            currentPullTween?.Kill();
        
            float targetVisualDistance = visualFishDistance - adjustedPullAmount;
            targetVisualDistance = Mathf.Clamp(targetVisualDistance, 0f, maxFishingDistance);
        
            currentPullTween = DOTween.To(
                () => visualFishDistance,
                x => visualFishDistance = x,
                targetVisualDistance,
                pullTweenDuration
            ).SetEase(Ease.OutQuad);
        }
        else
        {
            visualFishDistance = fishDistance;
        }
    
        if (showDebugLogs)
            Debug.Log($"[Pull!] Distance: {fishDistance:F1} | Visual: {visualFishDistance:F1}");
    }
    
    public void StopMinigame()
    {
        isMinigameActive = false;
        currentFish = null;
        currentEquipment = null;
        currentPool = null;
        reelSequence = null;
        currentSlotIndex = 0;
        visibleSlotCount = 0;
        currentPullTween?.Kill();
        currentHandleRotationTween?.Kill();
        
        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);
    }
    
    void Update()
    {
        if (!isMinigameActive) return;
    
        // Check win/lose BEFORE drift
        CheckWinLoseConditions();
        if (!isMinigameActive) return;
    
        UpdateDistanceDrift();
        UpdateLockoutTimer();
        UpdateSpeedResetTimer();
    
        // Smooth visual toward actual
        visualFishDistance = Mathf.Lerp(visualFishDistance, fishDistance, Time.deltaTime * 10f);
    
        // Fire progress event (throttled)
        if (Mathf.Abs(fishDistance - lastNotifiedDistance) >= progressNotifyThreshold)
        {
            FishingEvents.MinigameProgressChanged(fishDistance, maxFishingDistance);
            lastNotifiedDistance = fishDistance;
        }
    
        UpdateFishPosition();
        UpdateStatusText();
    }
    
    /// <summary>
    /// Fish naturally drifts away (increases distance)
    /// </summary>
    private void UpdateDistanceDrift()
    {
        fishDistance += calculatedDriftSpeed * Time.deltaTime;
        fishDistance = Mathf.Clamp(fishDistance, 0f, maxFishingDistance);
    }
    
    /// <summary>
    /// Handle wrong input lockout timer
    /// </summary>
    private void UpdateLockoutTimer()
    {
        if (!isLockedOut) return;
    
        lockoutTimer -= Time.deltaTime;
    
        if (lockoutTimer <= 0f)
        {
            isLockedOut = false;
            
            if (showDebugLogs)
                Debug.Log("[Lockout] Player can input again");
        }
    }
    
    /// <summary>
    /// Reset reel speed multiplier after combo window expires
    /// </summary>
    private void UpdateSpeedResetTimer()
    {
        if (!isWaitingToResetSpeed) return;
    
        speedResetTimer -= Time.deltaTime;
    
        if (speedResetTimer <= 0f)
        {
            currentSpeedMultiplier = 1f;
            isWaitingToResetSpeed = false;
        }
    }
    
    private void CheckWinLoseConditions()
    {
        if (showDebugLogs)
            Debug.Log($"[CHECK] Distance: {fishDistance:F3} | Max: {maxFishingDistance:F3} | Active: {isMinigameActive}");
    
        if (fishDistance <= 0f)
        {
            Debug.LogWarning($"[WIN CONDITION TRIGGERED!] fishDistance = {fishDistance}");
            isMinigameActive = false;
            
            Debug.Log($"[Minigame Win] {currentFish.itemName} successfully caught!");
            
            FishingEvents.FishCaught(currentFish);
            FishingEvents.MinigameEnded();
            StopMinigame();
            return;
        }
        else if (fishDistance >= maxFishingDistance)
        {
            Debug.LogWarning($"[LOSE CONDITION TRIGGERED!] fishDistance = {fishDistance} >= {maxFishingDistance}");
            isMinigameActive = false;
            
            Debug.Log($"[Minigame Lose] {currentFish.itemName} escaped!");
            
            FishingEvents.FishEscaped(currentFish);
            FishingEvents.MinigameEnded();
            StopMinigame();
            return;
        }
    }
    
    /// <summary>
    /// Update fish icon position on distance bar.
    /// LEFT (0) = caught, MIDDLE (50%) = start, RIGHT (max) = escaped
    /// </summary>
    private void UpdateFishPosition()
    {
        if (fishIcon == null) return;
    
        float fishIconWidth = fishIcon.rect.width;
        float usableWidth = distanceBarWidth - fishIconWidth;
        float normalizedPosition = visualFishDistance / maxFishingDistance;
        float xPosition = Mathf.Lerp(-usableWidth / 2f, usableWidth / 2f, normalizedPosition);
    
        fishIcon.anchoredPosition = new Vector2(xPosition, fishIcon.anchoredPosition.y);
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null) return;
    
        if (!isMinigameActive || visibleSlotCount == 0)
        {
            statusText.text = "Minigame Ended";
            return;
        }
    
        string nextButton = GetButtonForSlot(currentSlotIndex).ToString();
        string lockoutStatus = isLockedOut ? $" [LOCKED {lockoutTimer:F1}s]" : "";
    
        statusText.text = $"Distance: {fishDistance:F1} / {maxFishingDistance} (Visual: {visualFishDistance:F1})\n" +
                          $"Drift Speed: {calculatedDriftSpeed:F1}/s\n" +
                          $"Next: {nextButton}{lockoutStatus}\n" +
                          $"Rotation: {correctInputsInRotation}/{visibleSlotCount}";
    }
    
    public float GetFishDistance() => fishDistance;
    public float GetMaxFishingDistance() => maxFishingDistance;
    public bool IsActive() => isMinigameActive;
    
    private void UpdateSlotVisualFeedback()
    {
        for (int i = 0; i < visibleSlotCount && i < buttonDisplaySlots.Length; i++)
        {
            if (buttonDisplaySlots[i] == null) continue;
        
            if (i == currentSlotIndex)
            {
                buttonDisplaySlots[i].transform.parent.GetComponent<RectTransform>().localScale = Vector3.one;
                Color color = buttonDisplaySlots[i].color;
                color.a = 1f;
                buttonDisplaySlots[i].color = color;
            }
            else
            {
                buttonDisplaySlots[i].transform.parent.GetComponent<RectTransform>().localScale = Vector3.one * inactiveButtonScale;
                Color color = buttonDisplaySlots[i].color;
                color.a = inactiveButtonAlpha;
                buttonDisplaySlots[i].color = color;
            }
        }
    }
    
    private ReelButton ApplyScrambling(ReelButton baseButton)
    {
        if (Random.value < scrambleChance)
        {
            IReadOnlyList<ReelButton> agitatedList = currentFish.GetAgitatedButtons();
        
            if (agitatedList != null && agitatedList.Count > 0)
            {
                ReelButton scrambled = agitatedList[Random.Range(0, agitatedList.Count)];
            
                if (showDebugLogs && scrambled != baseButton)
                    Debug.Log($"[Scrambled!] {baseButton} → {scrambled}");
            
                return scrambled;
            }
        }
    
        return baseButton;
    }
    
    private void UpdateSingleSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= buttonDisplaySlots.Length) return;
        if (buttonDisplaySlots[slotIndex] == null) return;
    
        ReelButton buttonToShow = GetButtonForSlot(slotIndex);
    
        buttonDisplaySlots[slotIndex].gameObject.SetActive(true);
        buttonDisplaySlots[slotIndex].sprite = GetSpriteForButton(buttonToShow);
    }
}