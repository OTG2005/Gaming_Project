using UnityEngine;
using TMPro;

/// <summary>Shows a speech bubble above Freya for a few seconds.
/// Put on Freya. Make a child object "Bubble" (bubble sprite + TextMeshPro text) and drag it in.</summary>
public class FreyaCallout : MonoBehaviour
{
    public GameObject bubble;
    public TMP_Text text;
    public float showTime = 1.8f;
    public AudioSource voice;              // optional recorded line
    float timer;

    void Start() { if (bubble != null) bubble.SetActive(false); }

    public void Say(string line)
    {
        if (bubble == null) return;
        if (text != null) text.text = line;
        bubble.SetActive(true);
        timer = showTime;
        if (voice != null) voice.Play();
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) bubble.SetActive(false);
        }
    }
}
