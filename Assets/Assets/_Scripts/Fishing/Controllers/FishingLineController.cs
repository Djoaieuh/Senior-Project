using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FishingLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rodTipTransform;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Line States")]
    [SerializeField] private int slackLinePoints = 5;
    [SerializeField] private float slackCurveAmount = 2f;
    [SerializeField] private float slackSwaySpeed = 1f;
    [SerializeField] private float slackSwayAmount = 0.2f;
    
    [Header("Tension Visual")]
    [SerializeField] private Color relaxedColor = Color.white;
    [SerializeField] private Color tenseColor = Color.red;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private bool isTense = false;
    private Transform bobberTransform;
    private float swayTime = 0f;
    private float currentTension = 0f;
    
    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
    }
    
    private void Update()
    {
        if (bobberTransform == null || rodTipTransform == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        lineRenderer.enabled = true;
        
        if (isTense)
            UpdateTenseLine();
        else
            UpdateSlackLine();
    }
    
    /// <summary>
    /// Set the line state (slack or tense)
    /// </summary>
    public void SetLineState(bool tense)
    {
        isTense = tense;
        
        if (isTense)
            lineRenderer.positionCount = 2;
        else
        {
            lineRenderer.positionCount = slackLinePoints;
            swayTime = 0f;
        }
        
        if (showDebugLogs)
            Debug.Log($"[FishingLine] State: {(isTense ? "TENSE" : "SLACK")}");
    }
    
    /// <summary>
    /// Set the bobber transform to track
    /// </summary>
    public void SetBobberTransform(Transform bobber)
    {
        bobberTransform = bobber;
    }
    
    /// <summary>
    /// Set tension visual (0-1)
    /// </summary>
    public void SetTensionVisual(float tension)
    {
        currentTension = Mathf.Clamp01(tension);
        Color targetColor = Color.Lerp(relaxedColor, tenseColor, currentTension);
        lineRenderer.startColor = targetColor;
        lineRenderer.endColor = targetColor;
    }
    
    /// <summary>
    /// Update tense line (straight, 2 points)
    /// </summary>
    private void UpdateTenseLine()
    {
        lineRenderer.SetPosition(0, rodTipTransform.position);
        lineRenderer.SetPosition(1, bobberTransform.position);
    }
    
    /// <summary>
    /// Update slack line (curved, multiple points)
    /// First and last points stay locked to rod tip and bobber
    /// </summary>
    private void UpdateSlackLine()
    {
        Vector3 start = rodTipTransform.position;
        Vector3 end = bobberTransform.position;
        
        swayTime += Time.deltaTime * slackSwaySpeed;
        
        for (int i = 0; i < slackLinePoints; i++)
        {
            float t = i / (float)(slackLinePoints - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            
            if (i > 0 && i < slackLinePoints - 1)
            {
                float curveAmount = slackCurveAmount * Mathf.Sin(t * Mathf.PI);
                point.y -= curveAmount;
                
                float sway = Mathf.Sin(swayTime + t * Mathf.PI) * slackSwayAmount;
                Vector3 perpendicular = Vector3.Cross((end - start).normalized, Vector3.up);
                point += perpendicular * sway;
            }
            
            lineRenderer.SetPosition(i, point);
        }
        
        // Ensure endpoints are exactly at rod tip and bobber
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(slackLinePoints - 1, end);
    }
    
    /// <summary>
    /// Hide the fishing line
    /// </summary>
    public void HideLine()
    {
        lineRenderer.enabled = false;
        bobberTransform = null;
    }
}