using UnityEngine;
using UnityEngine.UI;

/// <summary>Put on the health bar. Drag the red "Fill" Image (Image Type = Filled, Horizontal) into fill.</summary>
public class HealthBarUI : MonoBehaviour
{
    public Image fill;
    public float smoothSpeed = 3f;
    float target = 1f;

    public void SetValue(float value01) { target = Mathf.Clamp01(value01); }

    void Update()
    {
        if (fill != null)
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, smoothSpeed * Time.unscaledDeltaTime);
    }
}
