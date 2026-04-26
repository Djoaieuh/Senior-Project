using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MapNavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapContent;

    [Header("Zoom Config")]
    [SerializeField] private float maxZoom  = 4.0f;
    [SerializeField] private float zoomStep = 0.2f;

    [Header("Default Open View")]
    [SerializeField] private float defaultOpenZoom = 2.5f;

    private MapNavigator  navigator;
    private RectTransform viewportRect;
    private Vector2       mapImageSize;
    private float         computedMinZoom = 1f;

    private FishingActions inputActions;
    private bool           isDragActive;

    private bool    hasPendingFocus;
    private Vector2 pendingFocusPoint;
    private float   pendingFocusZoom;

    public float DefaultOpenZoom => defaultOpenZoom;

    // ══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        viewportRect = GetComponent<RectTransform>();
        navigator    = new MapNavigator { MaxZoom = maxZoom, ZoomStep = zoomStep };
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
        isDragActive    = false;
        hasPendingFocus = false;
    }

    private void Update()
    {
        if (!isDragActive) return;
        navigator.UpdateDrag(GetPointerCanvasPosition());
        navigator.ClampToBounds(viewportRect.rect.size, mapImageSize);
        ApplyToRectTransform();
    }

    // ══════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════

    public void FocusOn(Vector2 contentLocalPoint, float zoom)
    {
        pendingFocusPoint = contentLocalPoint;
        pendingFocusZoom  = zoom;
        hasPendingFocus   = true;
        StopAllCoroutines();
        StartCoroutine(ApplyFocusAfterLayout());
    }

    // ══════════════════════════════════════════════════════════════════════
    // COROUTINE
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator ApplyFocusAfterLayout()
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();

        Vector2 viewport = viewportRect.rect.size;
        mapImageSize = mapContent != null ? mapContent.rect.size : viewport;
        computedMinZoom = ComputeMinZoom(viewport, mapImageSize);

        navigator.MinZoom = computedMinZoom;
        navigator.MaxZoom = maxZoom;

        float targetZoom = Mathf.Clamp(pendingFocusZoom, computedMinZoom, maxZoom);

        // ── VERBOSE DEBUG — read this in the console ──────────────────
        Debug.Log($"[MapNav] === FOCUS DEBUG ===");
        Debug.Log($"[MapNav] viewportRect name: '{viewportRect.name}'");
        Debug.Log($"[MapNav] mapContent name:   '{(mapContent != null ? mapContent.name : "NULL")}'");
        Debug.Log($"[MapNav] viewport size:     {viewport}");
        Debug.Log($"[MapNav] mapImage size:     {mapImageSize}");
        Debug.Log($"[MapNav] computedMinZoom:   {computedMinZoom:F4}");
        Debug.Log($"[MapNav] pendingFocusZoom:  {pendingFocusZoom:F4}");
        Debug.Log($"[MapNav] targetZoom:        {targetZoom:F4}");
        Debug.Log($"[MapNav] pendingFocusPoint: {pendingFocusPoint}");
        // ─────────────────────────────────────────────────────────────

        if (!hasPendingFocus)
        {
            Debug.Log("[MapNav] hasPendingFocus was false — aborting");
            yield break;
        }
        hasPendingFocus = false;

        Vector2 centeredOffset = -pendingFocusPoint * targetZoom;
        navigator.Reset(centeredOffset, targetZoom);
        navigator.ClampToBounds(viewport, mapImageSize);
        ApplyToRectTransform();

        Debug.Log($"[MapNav] Applied — ZoomScale: {navigator.ZoomScale:F4} | PanOffset: {navigator.PanOffset} | localScale: {mapContent?.localScale}");
    }

    private float ComputeMinZoom(Vector2 viewport, Vector2 mapSize)
    {
        if (mapSize.x <= 0 || mapSize.y <= 0) return 1f;
        float scaleX = viewport.x / mapSize.x;
        float scaleY = viewport.y / mapSize.y;
        return Mathf.Max(scaleX, scaleY);
    }

    // ══════════════════════════════════════════════════════════════════════
    // INPUT
    // ══════════════════════════════════════════════════════════════════════

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
        Debug.Log($"[MapNav] Scroll input: {scrollDelta}");

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect,
            inputActions.PlayerInputs.MapPoint.ReadValue<Vector2>(),
            null,
            out Vector2 localPoint
        );

        navigator.ApplyScrollZoom(scrollDelta.y, localPoint, viewportRect.rect.size);
        navigator.ClampToBounds(viewportRect.rect.size, mapImageSize);
        ApplyToRectTransform();

        Debug.Log($"[MapNav] After scroll — ZoomScale: {navigator.ZoomScale:F4} | mapImageSize: {mapImageSize}");
    }

    private Vector2 GetPointerCanvasPosition()
    {
        Vector2 screenPos = inputActions.PlayerInputs.MapPoint.ReadValue<Vector2>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect, screenPos, null, out Vector2 localPoint);
        return localPoint;
    }

    private void ApplyToRectTransform()
    {
        if (mapContent == null) return;
        mapContent.localScale       = new Vector3(navigator.ZoomScale, navigator.ZoomScale, 1f);
        mapContent.anchoredPosition = navigator.PanOffset;
    }
}