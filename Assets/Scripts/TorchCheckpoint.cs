using UnityEngine;

/// <summary>Checkpoint post that lights its torch when Erik touches it.
/// Needs a Collider2D with "Is Trigger" ticked.</summary>
[RequireComponent(typeof(Collider2D))]
public class TorchCheckpoint : MonoBehaviour
{
    public SpriteRenderer postRenderer;    // the post's SpriteRenderer (this object)
    public Sprite unlitSprite;             // cp_off
    public Sprite litSprite;               // cp_lit
    public bool startLit = false;          // tick only for Checkpoint 1 at the gate
    public AudioSource lightSound;         // optional

    bool reached;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void Start()
    {
        if (postRenderer == null) postRenderer = GetComponent<SpriteRenderer>();
        if (postRenderer != null && unlitSprite != null) postRenderer.sprite = startLit ? litSprite : unlitSprite;
        if (startLit) Activate(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || other.GetComponentInParent<PlayerHealth>() == null) return;
        Activate(true);
    }

    void Activate(bool playSound)
    {
        reached = true;
        var rm = FindObjectOfType<RespawnManager>();
        if (rm != null) rm.SetCheckpoint(transform);
        if (postRenderer != null && litSprite != null) postRenderer.sprite = litSprite;
        if (playSound && lightSound != null) lightSound.Play();
    }
}
