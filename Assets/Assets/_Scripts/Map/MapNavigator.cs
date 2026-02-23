using UnityEngine;

/// <summary>
/// Pure logic class for map pan/zoom math.
/// No MonoBehaviour - driven by MapNavigationController.
/// </summary>
public class MapNavigator
{
    public float MinZoom  { get; set; } = 1f;
    public float MaxZoom  { get; set; } = 3.0f;
    public float ZoomStep { get; set; } = 0.15f;

    public Vector2 PanOffset { get; private set; }
    public float   ZoomScale { get; private set; } = 1f;

    private bool    isDragging;
    private Vector2 dragStartPointer;
    private Vector2 dragStartOffset;

    public void Reset(Vector2 initialOffset, float initialZoom)
    {
        PanOffset  = initialOffset;
        ZoomScale  = Mathf.Clamp(initialZoom, MinZoom, MaxZoom);
        isDragging = false;
    }

    public void BeginDrag(Vector2 pointerPosition)
    {
        isDragging       = true;
        dragStartPointer = pointerPosition;
        dragStartOffset  = PanOffset;
    }

    public void UpdateDrag(Vector2 pointerPosition)
    {
        if (!isDragging) return;
        PanOffset = dragStartOffset + (pointerPosition - dragStartPointer);
    }

    public void EndDrag()
    {
        isDragging = false;
    }

    public void ClampToBounds(Vector2 viewportSize, Vector2 contentOriginalSize)
    {
        Vector2 scaledSize = contentOriginalSize * ZoomScale;

        float maxX = Mathf.Max(0f, (scaledSize.x - viewportSize.x) / 2f);
        float maxY = Mathf.Max(0f, (scaledSize.y - viewportSize.y) / 2f);

        PanOffset = new Vector2(
            Mathf.Clamp(PanOffset.x, -maxX, maxX),
            Mathf.Clamp(PanOffset.y, -maxY, maxY)
        );
    }

    public void ApplyScrollZoom(float scrollDelta, Vector2 pointerViewportLocal, Vector2 viewportSize)
    {
        if (Mathf.Approximately(scrollDelta, 0f)) return;

        float oldZoom = ZoomScale;
        float newZoom = Mathf.Clamp(ZoomScale + scrollDelta * ZoomStep, MinZoom, MaxZoom);

        if (Mathf.Approximately(oldZoom, newZoom)) return;

        // Keep the point under the cursor fixed while zooming
        Vector2 pointerOnContent = (pointerViewportLocal - PanOffset) / oldZoom;
        PanOffset = pointerViewportLocal - pointerOnContent * newZoom;

        ZoomScale = newZoom;
    }
}