using UnityEngine;

public class ButtonLayoutManager
{
    private RectTransform[] buttonSlots;
    private float radius;
    
    public ButtonLayoutManager(RectTransform[] slots, float circleRadius)
    {
        buttonSlots = slots;
        radius = circleRadius;
    }
    
    public void ApplyLayout(int slotCount)
    {
        switch (slotCount)
        {
            case 2: ApplyLayout_2Slots(); break;
            case 3: ApplyLayout_3Slots(); break;
            case 4: ApplyLayout_4Slots(); break;
            case 5: ApplyLayout_5Slots(); break;
            case 6: ApplyLayout_6Slots(); break;
            case 7: ApplyLayout_7Slots(); break;
            case 8: ApplyLayout_8Slots(); break;
            default:
                Debug.LogWarning($"No layout defined for {slotCount} slots!");
                break;
        }
    }
    
    private void ApplyLayout_2Slots()
    {
        // Top (12 o'clock) = 90°, Bottom (6 o'clock) = -90° or 270°
        float[] angles = { 90f, -90f }; 
        ApplyCircularLayout(2, angles);
    }
    
    private void ApplyLayout_3Slots()
    {
        // Start at top, go clockwise: 12, 4, 8 o'clock
        float[] angles = { 90f, -30f, -150f }; 
        ApplyCircularLayout(3, angles);
    }
    
    private void ApplyLayout_4Slots()
    {
        // 12, 3, 6, 9 o'clock
        float[] angles = { 90f, 0f, -90f, 180f }; 
        ApplyCircularLayout(4, angles);
    }
    
    private void ApplyLayout_5Slots()
    {
        // Start at top, evenly spaced clockwise (72° apart)
        float[] angles = { 90f, 18f, -54f, -126f, -198f };
        ApplyCircularLayout(5, angles);
    }
    
    private void ApplyLayout_6Slots()
    {
        // 12, 2, 4, 6, 8, 10 o'clock (60° apart)
        float[] angles = { 90f, 30f, -30f, -90f, -150f, -210f }; 
        ApplyCircularLayout(6, angles);
    }
    
    private void ApplyLayout_7Slots()
    {
        // Start at top, evenly spaced clockwise (51.43° apart)
        float[] angles = { 90f, 38.57f, -12.86f, -64.29f, -115.71f, -167.14f, -218.57f };
        ApplyCircularLayout(7, angles);
    }
    
    private void ApplyLayout_8Slots()
    {
        // 12, 1:30, 3, 4:30, 6, 7:30, 9, 10:30 (45° apart)
        float[] angles = { 90f, 45f, 0f, -45f, -90f, -135f, -180f, -225f }; 
        ApplyCircularLayout(8, angles);
    }
    
    private void ApplyCircularLayout(int count, float[] angles)
    {
        for (int i = 0; i < count && i < buttonSlots.Length; i++)
        {
            if (buttonSlots[i] != null)
            {
                float angle = angles[i];
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                buttonSlots[i].anchoredPosition = new Vector2(x, y);
            }
        }
    }
}