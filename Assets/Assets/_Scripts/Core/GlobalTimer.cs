using UnityEngine;
using System;

/// <summary>
/// Tracks "active fishing time" — the in-game clock that only ticks while the
/// player has their line in the water (cast → caught/escaped).
///
/// Any system that needs a cooldown tied to fishing activity reads
/// GlobalTimer.Instance.ElapsedSeconds instead of DateTime.UtcNow.
///
/// Lives as a component on the GameManager prefab.
/// </summary>
public class GlobalTimer : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static GlobalTimer Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // ── Persistent state ───────────────────────────────────────────────────
    /// <summary>
    /// Total accumulated active-fishing seconds across all sessions.
    /// Saved/loaded via SaveSystem.
    /// </summary>
    private double totalElapsedSeconds = 0.0;

    // ── Runtime state ──────────────────────────────────────────────────────
    private bool isTicking = false;

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Fired every second (approximately) while the timer is ticking.</summary>
    public static event Action<double> OnTimerTick;

    // ══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        FishingEvents.OnLineCast      += HandleLineCast;
        FishingEvents.OnFishCaught    += HandleFishCaught;
        FishingEvents.OnFishEscaped   += HandleFishEscaped;
        FishingEvents.OnMinigameEnded += HandleMinigameEnded;
    }

    private void OnDisable()
    {
        FishingEvents.OnLineCast      -= HandleLineCast;
        FishingEvents.OnFishCaught    -= HandleFishCaught;
        FishingEvents.OnFishEscaped   -= HandleFishEscaped;
        FishingEvents.OnMinigameEnded -= HandleMinigameEnded;
    }

    // ── Tick accumulation ──────────────────────────────────────────────────
    private double tickAccumulator = 0.0;

    private void Update()
    {
        if (!isTicking) return;

        totalElapsedSeconds += Time.deltaTime;
        tickAccumulator     += Time.deltaTime;

        // Fire the event approximately once per second
        if (tickAccumulator >= 1.0)
        {
            tickAccumulator -= 1.0;
            OnTimerTick?.Invoke(totalElapsedSeconds);

            if (showDebugLogs)
                Debug.Log($"[GlobalTimer] Active fishing time: {FormatTime(totalElapsedSeconds)}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // FISHING EVENT HOOKS
    // ══════════════════════════════════════════════════════════════════════

    private void HandleLineCast()
    {
        StartTicking();
    }

    private void HandleFishCaught(CatchableItem _) => StopTicking();
    private void HandleFishEscaped(CatchableItem _) => StopTicking();
    private void HandleMinigameEnded() => StopTicking();

    private void StartTicking()
    {
        if (isTicking) return;
        isTicking = true;
        if (showDebugLogs) Debug.Log("[GlobalTimer] ▶ Started ticking");
    }

    private void StopTicking()
    {
        if (!isTicking) return;
        isTicking = false;
        if (showDebugLogs) Debug.Log($"[GlobalTimer] ■ Stopped. Total: {FormatTime(totalElapsedSeconds)}");
    }

    // ══════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Total accumulated active-fishing seconds (ever, across sessions).</summary>
    public double ElapsedSeconds => totalElapsedSeconds;

    /// <summary>Is the timer currently counting?</summary>
    public bool IsTicking => isTicking;

    /// <summary>Human-readable formatted time string.</summary>
    public string FormattedTime => FormatTime(totalElapsedSeconds);

    // ── Save / Load ────────────────────────────────────────────────────────

    /// <summary>Called by SaveSystem to persist elapsed time.</summary>
    public double GetSaveValue() => totalElapsedSeconds;

    /// <summary>Called by SaveSystem after loading a save file.</summary>
    public void LoadSaveValue(double savedSeconds)
    {
        totalElapsedSeconds = savedSeconds;
        if (showDebugLogs) Debug.Log($"[GlobalTimer] Loaded: {FormatTime(totalElapsedSeconds)}");
    }

    // ── Utility ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if at least <paramref name="seconds"/> of active fishing
    /// time has passed since <paramref name="startSnapshot"/>.
    /// Use this for any cooldown that ticks with the GlobalTimer.
    ///
    /// Example:
    ///   bool restocked = GlobalTimer.Instance.HasElapsed(trade.restockStartSnapshot, trade.restockCooldown);
    /// </summary>
    public bool HasElapsed(double startSnapshot, double seconds)
    {
        return (totalElapsedSeconds - startSnapshot) >= seconds;
    }

    /// <summary>How many seconds remain on a cooldown. 0 if already done.</summary>
    public double SecondsRemaining(double startSnapshot, double durationSeconds)
    {
        double elapsed = totalElapsedSeconds - startSnapshot;
        double remaining = durationSeconds - elapsed;
        return remaining > 0 ? remaining : 0;
    }

    private static string FormatTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}h {t.Minutes:D2}m {t.Seconds:D2}s";
        return $"{t.Minutes:D2}m {t.Seconds:D2}s";
    }
}