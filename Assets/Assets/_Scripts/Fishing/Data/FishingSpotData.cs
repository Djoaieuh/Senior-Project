using UnityEngine;

public class FishingSpotData : MonoBehaviour
{
    // ============================================
    // STATIC CURRENT - no searching needed
    // ============================================
    
    public static FishingSpotData Current { get; private set; }

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // ============================================
    // SCENE / LOCATION IDENTITY
    // ============================================

    [Header("Scene Info")]
    [Tooltip("Must exactly match the Unity scene name in Build Settings")]
    [SerializeField] private string sceneName;
    public string SceneName => sceneName;

    [Header("Location Info")]
    [Tooltip("Unique location ID used by save system and journal. e.g. 'lake_mirror'")]
    [SerializeField] private string locationID;
    public string LocationID => locationID;

    [Tooltip("Display name shown in journal and UI. e.g. 'Mirror Lake'")]
    [SerializeField] private string locationName = "Unknown Waters";
    public string LocationName => locationName;

    // ============================================
    // FISHING
    // ============================================

    [Header("Fishing")]
    [Tooltip("The fishing pool available at this location")]
    [SerializeField] private FishingPool fishingPool;
    public FishingPool FishingPool => fishingPool;

    // ============================================
    // REQUIRED TRANSFORMS
    // ============================================

    [Header("Required Transforms")]
    [Tooltip("Where the bobber lands on the water surface")]
    public Transform bobberLandingPoint;

    [Tooltip("Underwater position when fish is almost caught (close to shore)")]
    public Transform underwaterStartPoint;

    [Tooltip("Underwater position when fish is escaping (far from shore)")]
    public Transform underwaterEndPoint;

    // ============================================
    // ANIMATION TIMINGS
    // ============================================

    [Header("Animation Timings")]
    [Tooltip("Duration of the bobber throw animation")]
    public float throwDuration = 1.5f;

    [Tooltip("Duration of the bobber submerge animation")]
    public float submergeDuration = 0.8f;

    // ============================================
    // OPTIONAL
    // ============================================

    [Header("Optional")]
    [Tooltip("Reference to water surface for effects")]
    public GameObject waterSurface;

    // ============================================
    // VALIDATION
    // ============================================

    private void OnValidate()
    {
        if (bobberLandingPoint == null)
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Bobber Landing Point not assigned!", this);

        if (underwaterStartPoint == null)
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Underwater Start Point not assigned!", this);

        if (underwaterEndPoint == null)
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Underwater End Point not assigned!", this);

        if (string.IsNullOrEmpty(sceneName))
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Scene Name not set! Save system needs this.", this);

        if (string.IsNullOrEmpty(locationID))
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Location ID not set!", this);

        if (fishingPool == null)
            Debug.LogWarning($"[FishingSpotData] {gameObject.name}: Fishing Pool not assigned!", this);
    }

    // ============================================
    // GIZMOS
    // ============================================

    private void OnDrawGizmos()
    {
        if (bobberLandingPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(bobberLandingPoint.position, 0.3f);
            Gizmos.DrawLine(bobberLandingPoint.position, bobberLandingPoint.position + Vector3.up * 2f);
        }

        if (underwaterStartPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(underwaterStartPoint.position, 0.3f);
            Gizmos.DrawLine(underwaterStartPoint.position, underwaterStartPoint.position + Vector3.up * 0.5f);
        }

        if (underwaterEndPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(underwaterEndPoint.position, 0.3f);
            Gizmos.DrawLine(underwaterEndPoint.position, underwaterEndPoint.position + Vector3.up * 0.5f);
        }

        if (underwaterStartPoint != null && underwaterEndPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(underwaterStartPoint.position, underwaterEndPoint.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (bobberLandingPoint != null)
            UnityEditor.Handles.Label(bobberLandingPoint.position + Vector3.up * 2.2f, "Bobber Landing");

        if (underwaterStartPoint != null)
            UnityEditor.Handles.Label(underwaterStartPoint.position + Vector3.up * 0.7f, "Fish Close (0%)");

        if (underwaterEndPoint != null)
            UnityEditor.Handles.Label(underwaterEndPoint.position + Vector3.up * 0.7f, "Fish Far (100%)");

        // Show location info in scene view when selected
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
            $"{locationName}\nID: {locationID}\nScene: {sceneName}");
#endif
    }
}