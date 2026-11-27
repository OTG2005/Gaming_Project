using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Shows Erik's lives as "x 3" and/or a row of icons.
/// Put on the Lives object in the Canvas.
public class LivesUI : MonoBehaviour
{
    public TMP_Text countText;             // a TextMeshPro text, shows "x 3"
    public Image[] lifeIcons;              // optional: one Image per life (icons hide when lost)
    public float popScale = 1.4f;

    int shown = -1; float pop;

    public void SetLives(int lives)
    {
        if (countText != null) countText.text = "x " + lives;
        if (lifeIcons != null)
            for (int i = 0; i < lifeIcons.Length; i++)
                if (lifeIcons[i] != null) lifeIcons[i].enabled = i < lives;
        if (shown != -1 && lives != shown) pop = 1f;   // little bounce when it changes
        shown = lives;
    }

    void Update()
    {
        if (pop > 0f)
        {
            pop -= Time.unscaledDeltaTime * 3f;
            float s = 1f + (popScale - 1f) * Mathf.Max(pop, 0f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
