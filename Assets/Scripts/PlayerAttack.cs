using UnityEngine;

/// <summary>Makes Erik's axe hit things. Put on Erik. Works with any movement script.</summary>
public class PlayerAttack : MonoBehaviour
{
    public KeyCode attackKey = KeyCode.X;
    public int damage = 25;
    public float range = 0.9f;             // size of the red circle
    public float hitDelay = 0.15f;         // wait until the swing lands in the animation
    public float cooldown = 0.4f;
    public LayerMask enemyLayers;          // tick the "Enemy" layer
    public Vector2 offset = new Vector2(0.7f, 0.3f);

    SpriteRenderer sr;
    PlayerHealth health;
    float cooldownTimer;

    void Awake() { sr = GetComponent<SpriteRenderer>(); health = GetComponent<PlayerHealth>(); }

    void Update()
    {
        if (PauseMenu.IsPaused || (health != null && health.IsDead)) return;
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f && Input.GetKeyDown(attackKey))
        {
            cooldownTimer = cooldown;
            Invoke(nameof(DoHit), hitDelay);
        }
    }

    Vector2 HitPoint()
    {
        float dir = (sr != null && sr.flipX) ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(offset.x * dir, offset.y);
    }

    void DoHit()
    {
        foreach (var col in Physics2D.OverlapCircleAll(HitPoint(), range, enemyLayers))
        {
            var target = col.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitPoint(), range);
    }
}
