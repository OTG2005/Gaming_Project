using UnityEngine;

/// <summary>
/// Simple Dagger Raider script in the style of Lab 6 (EnemyController / SlimeEnemy / FollowerEnemy),
/// adapted to work with this project's PlayerHealth and IDamageable (PlayerAttack).
/// </summary>
public class DaggerRaider : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxSpeed = 3.5f;
    public int damage = 15;
    public int maxHealth = 50;
    public float noticeRange = 8f;

    [Header("References")]
    public SpriteRenderer sr;
    public Transform player;

    private Rigidbody2D rb;
    private Animator anim;
    private int health;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (rb != null) rb.freezeRotation = true;
        health = maxHealth;

        if (player == null)
        {
            PlayerHealth p = FindObjectOfType<PlayerHealth>();
            if (p != null) player = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (isDead || rb == null) return;

        // Chase the player on the X-axis when within noticeRange; otherwise patrol like SlimeEnemy
        if (player != null && Vector2.Distance(transform.position, player.position) <= noticeRange)
        {
            float dir = player.position.x < transform.position.x ? -1f : 1f;
            rb.velocity = new Vector2(dir * maxSpeed, rb.velocity.y);
            if (sr != null) sr.flipX = (dir < 0f);
        }
        else
        {
            float dir = (sr != null && sr.flipX) ? -1f : 1f;
            rb.velocity = new Vector2(dir * maxSpeed, rb.velocity.y);
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            anim.SetFloat("Height", rb.velocity.y);
        }
    }

    public void Flip()
    {
        if (sr != null) sr.flipX = !sr.flipX;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other, flipOnHit: true);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HandleHit(other, flipOnHit: false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider, flipOnHit: true);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        HandleHit(collision.collider, flipOnHit: false);
    }

    void HandleHit(Collider2D other, bool flipOnHit)
    {
        if (isDead) return;

        PlayerHealth hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage, transform.position);
            if (flipOnHit && anim != null) anim.SetTrigger("Attack");
            if (flipOnHit) Flip();
        }
        else if (flipOnHit && other.CompareTag("Wall"))
        {
            Flip();
        }
    }

    // Allows Erik's axe (PlayerAttack) to damage and defeat the Dagger Raider
    public void TakeDamage(int amount, Vector2 fromPosition)
    {
        if (isDead) return;

        health -= amount;
        if (anim != null) anim.SetTrigger("Damage");

        if (health <= 0)
        {
            isDead = true;
            if (rb != null) rb.velocity = Vector2.zero;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 0.4f);
        }
    }
}
