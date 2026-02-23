using UnityEngine;
using DG.Tweening;
using System;
using Bitgem.VFX.StylisedWater;
using Random = UnityEngine.Random;

public class BobberController : MonoBehaviour
{
    [Header("Throw Settings")]
    [Tooltip("How high the bobber arcs relative to distance (0.3 = 30% of distance)")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float arcHeightMultiplier = 0.5f;

    [Tooltip("Minimum arc height (prevents too shallow throws)")]
    [SerializeField] private float minArcHeight = 0.5f;

    [Tooltip("Maximum arc height (prevents too high throws)")]
    [SerializeField] private float maxArcHeight = 5.0f;

    [Tooltip("How 'floaty' or 'heavy' the bobber feels (higher = falls faster)")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float gravityEffect = 1.0f;

    [Tooltip("Adds slight randomness to make throws feel natural")]
    [Range(0f, 0.2f)]
    [SerializeField] private float throwRandomness = 0.05f;
    
    [Header("Shake Settings")]
    [Tooltip("Tension % at which shaking starts (0-100)")]
    [Range(0f, 100f)]
    [SerializeField] private float shakeStartTension = 70f;

    [Tooltip("How much to shake at minimum (when tension = shakeStartTension)")]
    [Range(0f, 1f)]
    [SerializeField] private float minShakeAmount = 0.1f;

    [Tooltip("How much to shake at maximum (when tension = 100%)")]
    [Range(0f, 2f)]
    [SerializeField] private float maxShakeAmount = 0.5f;

    [Tooltip("Speed of positional shake")]
    [SerializeField] private float positionShakeSpeed = 8f;

    [Tooltip("Speed of rotational shake")]
    [SerializeField] private float rotationShakeSpeed = 12f;

    [Tooltip("Maximum position offset when shaking at full intensity")]
    [SerializeField] private float maxPositionShake = 0.3f;

    [Tooltip("Maximum rotation shake in degrees when at full intensity")]
    [SerializeField] private float maxRotationShake = 15f;
    
    public enum BobberState
    {
        Idle,
        Throwing,
        Floating,
        Submerged,
        ReelingIn,
        Surfacing
    }
    
    [Header("References")]
    [SerializeField] private ParticleSystem splashEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private WateverVolumeFloater waterFloater;
    
    [Header("Submerged Movement")]
    [Tooltip("How fast bobber moves underwater (higher = more responsive)")]
    [SerializeField] private float underwaterMoveSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private BobberState currentState = BobberState.Idle;
    private Tween currentMoveTween;
    
    // Underwater position tracking
    private Vector3 targetUnderwaterPosition;
    
    // Initial position tracking
    private Vector3 initialPosition;
    
    private float currentShakeIntensity = 0f;
    private Vector3 basePosition;
    
    public BobberState CurrentState => currentState;
    
    public event Action OnThrowComplete;
    public event Action OnSubmergeComplete;
    public event Action OnReelInComplete;
    
    private void Awake()
    {
        initialPosition = transform.position;
        
        if (waterFloater != null)
        {
            waterFloater.enabled = false;
        }
        
        if (showDebugLogs)
            Debug.Log($"[BobberController] Initial position stored: {initialPosition}");
    }
    
    private void Update()
    {
        if (currentState == BobberState.Submerged)
        {
            basePosition = Vector3.Lerp(
                transform.position,
                targetUnderwaterPosition,
                Time.deltaTime * underwaterMoveSpeed
            );
        
            if (currentShakeIntensity > 0.01f)
            {
                float shakeX = Mathf.Sin(Time.time * positionShakeSpeed) * 
                               maxPositionShake * currentShakeIntensity;
                float shakeY = Mathf.Sin(Time.time * positionShakeSpeed * 1.7f) * 
                               maxPositionShake * 0.6f * currentShakeIntensity;
                float shakeZ = Mathf.Cos(Time.time * positionShakeSpeed * 0.8f) * 
                               maxPositionShake * 0.4f * currentShakeIntensity;
            
                float rotZ = Mathf.Cos(Time.time * rotationShakeSpeed) * 
                             maxRotationShake * currentShakeIntensity;
            
                transform.position = basePosition + new Vector3(shakeX, shakeY, shakeZ);
                transform.localEulerAngles = new Vector3(0, 0, rotZ);
            }
            else
            {
                transform.position = basePosition;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
    
    /// <summary>
    /// Throw bobber from current position to target point
    /// </summary>
    public void ThrowToPoint(Transform target, float duration)
    {
        if (target == null)
        {
            Debug.LogError("[BobberController] Target transform is null!");
            return;
        }
    
        currentState = BobberState.Throwing;
        SetWaterFloaterEnabled(false);
    
        Vector3 startPos = transform.position;
        Vector3 endPos = target.position;
    
        float distance = Vector3.Distance(startPos, endPos);
        float baseArcHeight = distance * arcHeightMultiplier;
        float randomVariation = 1f + Random.Range(-throwRandomness, throwRandomness);
        baseArcHeight *= randomVariation;
        float finalArcHeight = Mathf.Clamp(baseArcHeight, minArcHeight, maxArcHeight);
    
        currentMoveTween?.Kill();
    
        currentMoveTween = DOTween.To(() => 0f, progress =>
            {
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, progress);
                float verticalOffset = Mathf.Sin(progress * Mathf.PI) * finalArcHeight * gravityEffect;
                transform.position = horizontalPos + Vector3.up * verticalOffset;
            }, 1f, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.position = endPos;
                OnLandInWater();
        
                if (showDebugLogs)
                    Debug.Log($"[BobberController] Throw: Arc = {finalArcHeight:F2}m, Distance = {distance:F2}m");
            });
    
        if (showDebugLogs)
            Debug.Log($"[BobberController] Throwing with arc: {finalArcHeight:F2}m");
    }
    
    private void OnLandInWater()
    {
        currentState = BobberState.Floating;
        SetWaterFloaterEnabled(true);
        PlaySplash();
        OnThrowComplete?.Invoke();
        
        if (showDebugLogs)
            Debug.Log("[BobberController] Landed in water, now floating");
    }
    
    /// <summary>
    /// Submerge bobber underwater
    /// </summary>
    public void SubmergeToPoint(Transform target, float duration)
    {
        if (target == null)
        {
            Debug.LogError("[BobberController] Submerge target is null!");
            return;
        }
        
        currentState = BobberState.Submerged;
        SetWaterFloaterEnabled(false);
        currentMoveTween?.Kill();
        
        currentMoveTween = transform.DOMove(target.position, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                PlaySplash();
                targetUnderwaterPosition = transform.position;
                OnSubmergeComplete?.Invoke();
                
                if (showDebugLogs)
                    Debug.Log("[BobberController] Submerged underwater");
            });
    }
    
    /// <summary>
    /// Update submerged position based on minigame progress.
    /// Sets target position; Update() smoothly lerps to it.
    /// </summary>
    public void UpdateSubmergedPosition(Vector3 startPoint, Vector3 endPoint, float progress)
    {
        if (currentState != BobberState.Submerged) return;
        targetUnderwaterPosition = Vector3.Lerp(startPoint, endPoint, progress);
    }
    
    /// <summary>
    /// Reel bobber back to initial rest position
    /// </summary>
    public void ReelIn(float duration)
    {
        currentState = BobberState.ReelingIn;
        SetWaterFloaterEnabled(false);
        currentMoveTween?.Kill();
        
        currentMoveTween = transform.DOMove(initialPosition, duration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                ReturnToIdle();
                OnReelInComplete?.Invoke();
                
                if (showDebugLogs)
                    Debug.Log("[BobberController] Reeled in to initial position");
            });
    }
    
    /// <summary>
    /// Surface bobber back to floating position
    /// </summary>
    public void SurfaceToPoint(Transform target, float duration)
    {
        currentState = BobberState.Surfacing;
        SetWaterFloaterEnabled(true);
        currentMoveTween?.Kill();
        
        currentMoveTween = transform.DOMove(target.position, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PlaySplash();
                
                if (showDebugLogs)
                    Debug.Log("[BobberController] Surfaced");
            });
    }
    
    /// <summary>
    /// Return bobber to idle state at initial position
    /// </summary>
    public void ReturnToIdle()
    {
        currentState = BobberState.Idle;
        currentMoveTween?.Kill();
        transform.position = initialPosition;
        currentShakeIntensity = 0f;
        SetWaterFloaterEnabled(false);
        
        if (showDebugLogs)
            Debug.Log("[BobberController] Returned to idle at initial position");
    }
    
    private void SetWaterFloaterEnabled(bool enabled)
    {
        if (waterFloater != null)
        {
            waterFloater.enabled = enabled;
            
            if (showDebugLogs)
                Debug.Log($"[BobberController] Water floater {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }
    
    private void PlaySplash()
    {
        if (splashEffect != null)
            splashEffect.Play();
        
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
    
    private void OnDestroy()
    {
        currentMoveTween?.Kill();
    }
    
    public void Shake(float intensity)
    {
        currentShakeIntensity = Mathf.Clamp01(intensity);
    
        if (showDebugLogs && intensity > 0)
            Debug.Log($"[Bobber] Shake intensity set to: {intensity:F2}");
    }
    
    public void StopShake()
    {
        currentShakeIntensity = 0f;
    }
    
    /// <summary>
    /// Set tension value (0-100%) — bobber calculates its own shake intensity
    /// </summary>
    public void SetTension(float tension)
    {
        if (currentState != BobberState.Submerged)
        {
            currentShakeIntensity = 0f;
            return;
        }
    
        if (tension >= shakeStartTension)
        {
            float tensionRange = 100f - shakeStartTension;
            float normalizedTension = (tension - shakeStartTension) / tensionRange;
            currentShakeIntensity = Mathf.Lerp(minShakeAmount, maxShakeAmount, normalizedTension);
        
            if (showDebugLogs)
                Debug.Log($"[Bobber] Tension: {tension:F1}% → Shake: {currentShakeIntensity:F2}");
        }
        else
        {
            currentShakeIntensity = 0f;
        }
    }
}