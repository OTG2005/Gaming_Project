using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class SpikedRing : MonoBehaviour, IDamageable
{
    public int damage = 15;
    public float spinSpeed = 720f;
    public float lifetime = 4f;
    public LayerMask groundLayer;
    public string hitSound = "sfx_trap_snap";

    Vector2 velocity;
    bool launched;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;
        launched = true;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!launched) return;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage, transform.position);
            SoundManager.Play(hitSound);
            Destroy(gameObject);
        }
        else if (((1 << other.gameObject.layer) & groundLayer.value) != 0) Destroy(gameObject);
    }

    public void TakeDamage(int amount, Vector2 from) { Destroy(gameObject); }
}