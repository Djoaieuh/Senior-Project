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
    [SerializeField] private float openZoom = 2.0f;

    // ============================================
    // RUNTIME
    // ============================================

    private MapNavigator  navigator;
    private RectTransform viewportRect;
    private Vector2       contentOriginalSize;

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

    private void Start()
    {
        // MapContent stretches to fill viewport, so sizes match
        contentOriginalSize = viewportRect.rect.size;
        Debug.Log($"[MapNav] viewportRect size: {viewportRect.rect.size}");
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

    private void Update()
    {
        if (!isDragActive) return;

        navigator.UpdateDrag(GetPointerCanvasPosition());
        navigator.ClampToBounds(viewportRect.rect.size, contentOriginalSize);
        ApplyToRectTransform();
    }

    // ============================================
    // PUBLIC API
    // ============================================

    /// <summary>
    /// Called by MapUI when the map opens - centers view on a pin.
    /// contentLocalPoint is the pin's anchoredPosition inside MapContent.
    /// </summary>
    public void FocusOn(Vector2 contentLocalPoint, float zoom)
    {
        if (mapContent == null) return;

        float clampedZoom = Mathf.Clamp(zoom, navigator.MinZoom, navigator.MaxZoom);
        Vector2 initialOffset = -contentLocalPoint * clampedZoom;

        navigator.Reset(initialOffset, clampedZoom);
        navigator.ClampToBounds(viewportRect.rect.size, contentOriginalSize);
        ApplyToRectTransform();
    }

    // ============================================
    // INPUT CALLBACKS
    // ============================================

    private void OnDragStarted(InputAction.CallbackContext ctx)
    {
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