using UnityEngine;

/// <summary>
/// A crossbow bolt fired by a CrossbowHunter (works like the Trap Engineer's SpikedRing).
/// Flies in a straight line, hurts Erik once, and breaks when it hits the ground or a wall.
/// Erik's axe can also knock it out of the air (it is IDamageable).
///
/// Prefab setup (the ready-made "CrossbowBolt" prefab in Assets/prefab already has this):
///  - SpriteRenderer with "crossbow_arrow" (the picture must point to the RIGHT)
///  - BoxCollider2D (Is Trigger) + Rigidbody2D (Kinematic)
///  - Ground Layer = "Ground"
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class CrossbowBolt : MonoBehaviour, IDamageable
{
    public int damage = 15;
    public float lifetime = 4f;            // destroyed after this many seconds if it hits nothing
    public LayerMask groundLayer;          // tick "Ground" so bolts stop at walls and floors

    /// The hunter that fired the bolt listens to this to play his taunt when Erik is hit.
    public System.Action onHitPlayer;

    Vector2 velocity;
    bool launched;

    void Reset() { groundLayer = LayerMask.GetMask("Ground"); }

    void Awake()
    {
        // moved by code, not by physics, and only needs to detect overlaps
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = true;
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
    }

    /// Called by the CrossbowHunter right after the bolt is created.
    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;

        // turn the arrow so its tip points where it flies
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        launched = true;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!launched) return;
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // hit Erik -> hurt him once and disappear
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage, transform.position);
            onHitPlayer?.Invoke();
            Destroy(gameObject);
            return;
        }

        // hit the ground / a wall -> break
        if (((1 << other.gameObject.layer) & groundLayer.value) != 0) Destroy(gameObject);
    }

    // Erik's axe hits the bolt -> it is knocked out of the air
    public void TakeDamage(int amount, Vector2 from) { Destroy(gameObject); }
}
