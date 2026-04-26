using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles map pan and zoom using the New Input System.
/// Attach to the Viewport GameObject.
/// </summary>
public class MapNavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapContent;

    [Header("Zoom Config")]
    [SerializeField] private float maxZoom  = 3.0f;
    [SerializeField] private float zoomStep = 0.15f;

    [Header("Initial View")]
    [Tooltip("Zoom level applied when the map opens. " +
             "NOTE: MapUI also has an openZoom field — that is the one actually passed " +
             "to FocusOn() and controls the opening zoom. Keep both in sync.")]
    [SerializeField] public float openZoom = 2.0f;

    // ============================================
    // RUNTIME
    // ============================================

    private MapNavigator  navigator;
    private RectTransform viewportRect;
    private Vector2       contentOriginalSize;
    private bool          isInitialized = false;

    // Pending focus — applied in LateUpdate so the Canvas layout system
    // does not overwrite the transform values we set in the same frame.
    private bool    hasPendingFocus;
    private Vector2 pendingFocusPoint;
    private float   pendingFocusZoom;

    private FishingActions inputActions;
    private bool           isDragActive;

    // ============================================
    // LIFECYCLE
    // ============================================

    private void Awake()
    {
        viewportRect = GetComponent<RectTransform>();

        navigator = new MapNavigator
        {
            MinZoom  = 1f,
            MaxZoom  = maxZoom,
            ZoomStep = zoomStep
        };

        inputActions = new FishingActions();
    }

    private void OnEnable()
    {
        inputActions.PlayerInputs.MapDrag.started   += OnDragStarted;
        inputActions.PlayerInputs.MapDrag.canceled  += OnDragCanceled;
        inputActions.PlayerInputs.MapZoom.performed += OnZoom;
        inputActions.PlayerInputs.Enable();
    }

    private void OnDisable()
    {
        inputActions.PlayerInputs.MapDrag.started   -= OnDragStarted;
        inputActions.PlayerInputs.MapDrag.canceled  -= OnDragCanceled;
        inputActions.PlayerInputs.MapZoom.performed -= OnZoom;
        isDragActive = false;
    }

    private void LateUpdate()
    {
        // Apply pending focus here — LateUpdate runs after Unity's layout system
        // has settled for this frame, so our values won't get overwritten.
        if (hasPendingFocus)
        {
            hasPendingFocus = false;
            ApplyFocusInternal(pendingFocusPoint, pendingFocusZoom);
        }

        if (!isDragActive) return;

        EnsureInitialized();
        navigator.UpdateDrag(GetPointerCanvasPosition());
        navigator.ClampToBounds(viewportRect.rect.size, contentOriginalSize);
        ApplyToRectTransform();
    }

    // ============================================
    // INITIALIZATION
    // ============================================

    private void TryInitializeSize()
    {
        if (isInitialized) return;
        if (mapContent == null) return;

        Canvas.ForceUpdateCanvases();

        contentOriginalSize = mapContent.rect.size;

        if (contentOriginalSize.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning("[MapNav] mapContent rect size is still (0,0) after ForceUpdateCanvases. " +
                             "Check that MapContent has a non-zero size in the layout.");
            return;
        }

        isInitialized = true;
        Debug.Log($"[MapNav] Initialized — contentOriginalSize: {contentOriginalSize} | viewportSize: {viewportRect.rect.size}");
    }

    private void EnsureInitialized()
    {
        if (!isInitialized) TryInitializeSize();
    }

    // ============================================
    // PUBLIC API
    // ============================================

    /// <summary>
    /// Called by MapUI when the map opens — centers view on a pin.
    /// contentLocalPoint is the pin's anchoredPosition inside MapContent.
    /// The transform update is deferred to LateUpdate to prevent the Canvas
    /// layout system from overwriting the values in the same frame.
    /// </summary>
    public void FocusOn(Vector2 contentLocalPoint, float zoom)
    {
        if (mapContent == null) return;

        EnsureInitialized();

        if (!isInitialized)
        {
            Debug.LogWarning("[MapNav] FocusOn called but size could not be determined yet.");
            return;
        }

        pendingFocusPoint = contentLocalPoint;
        pendingFocusZoom  = zoom;
        hasPendingFocus   = true;

        Debug.Log($"[MapNav] FocusOn queued — point: {contentLocalPoint} | zoom: {zoom}");
    }

    private void ApplyFocusInternal(Vector2 contentLocalPoint, float zoom)
    {
        float   clampedZoom   = Mathf.Clamp(zoom, navigator.MinZoom, navigator.MaxZoom);
        Vector2 initialOffset = -contentLocalPoint * clampedZoom;

        navigator.Reset(initialOffset, clampedZoom);
        navigator.ClampToBounds(viewportRect.rect.size, contentOriginalSize);
        ApplyToRectTransform();

        Debug.Log($"[MapNav] FocusOn applied — zoom: {clampedZoom} | offset: {navigator.PanOffset} | mapContent.localScale: {mapContent.localScale}");
    }

    // ============================================
    // INPUT CALLBACKS
    // ============================================

    private void OnDragStarted(InputAction.CallbackContext ctx)
    {
        EnsureInitialized();
        navigator.BeginDrag(GetPointerCanvasPosition());
        isDragActive = true;
    }

    private void OnDragCanceled(InputAction.CallbackContext ctx)
    {
        navigator.EndDrag();
        isDragActive = false;
    }

    private void OnZoom(InputAction.CallbackContext ctx)
    {
        EnsureInitialized();
        if (!isInitialized) return;

        Vector2 scrollDelta = ctx.ReadValue<Vector2>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect,
            inputActions.PlayerInputs.MapPoint.ReadValue<Vector2>(),
            null,
            out Vector2 localPoint
        );

        navigator.ApplyScrollZoom(scrollDelta.y, localPoint, viewportRect.rect.size);
        navigator.ClampToBounds(viewportRect.rect.size, contentOriginalSize);
        ApplyToRectTransform();
    }

    // ============================================
    // HELPERS
    // ============================================

    private Vector2 GetPointerCanvasPosition()
    {
        Vector2 screenPos = inputActions.PlayerInputs.MapPoint.ReadValue<Vector2>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect,
            screenPos,
            null,
            out Vector2 localPoint
        );
        return localPoint;
    }

    private void ApplyToRectTransform()
    {
        if (mapContent == null) return;

        mapContent.localScale       = new Vector3(navigator.ZoomScale, navigator.ZoomScale, 1f);
        mapContent.anchoredPosition = navigator.PanOffset;
    }
}