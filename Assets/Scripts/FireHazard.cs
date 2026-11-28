using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireHazard : MonoBehaviour
{
    public int damage = 10;

    [Header("Pulsing (optional)")]
    public bool pulsing = false;
    public float onTime = 2f;
    public float offTime = 1.5f;
    public float warnTime = 0.6f;
    public float startDelay = 0f;
    public SpriteRenderer flameSprite;

    Collider2D col; float t;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void Start() { col = GetComponent<Collider2D>(); t = startDelay; }

    void Update()
    {
        if (!pulsing) return;
        t += Time.deltaTime;
        float cycle = onTime + offTime;
        float phase = t % cycle;
        bool burning = phase < onTime;
        bool warning = !burning && phase > cycle - warnTime;

        col.enabled = burning;
        if (flameSprite != null)
        {
            var c = flameSprite.color;
            c.a = burning ? 1f : (warning ? 0.35f : 0f);
            flameSprite.color = c;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(damage, transform.position);
    }
}   