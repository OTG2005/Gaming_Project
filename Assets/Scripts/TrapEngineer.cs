using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TrapEngineer : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 50;
    public int contactDamage = 10;

    [Header("Ring attack")]
    public GameObject ringPrefab;
    public Transform throwPoint;
    public float aimTime = 0.7f;
    public float throwCooldown = 2.2f;
    public float ringSpeed = 7f;

    [Header("His caged dragon")]
    public DragonCage cage;
    public float netTime = 25f;

    [Header("Optional")]
    public bool spriteFacesRight = true;
    public FreyaCallout freya;
    public string throwSound = "sfx_trap_throw";
    public string defeatSound = "beast_defeat";

    Rigidbody2D rb; SpriteRenderer sr;
    Transform player;
    int health; float timer, netProgress;
    bool aiming, netted, dead;

    public bool IsDead { get { return dead; } }
    public System.Action<TrapEngineer> onDied;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
        health = maxHealth;
    }

    void Start()
    {
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
        if (freya == null) freya = FindObjectOfType<FreyaCallout>();
        timer = 1f;
        if (freya != null) freya.Say("Spiked rings! Keep moving!");
    }

    void Update()
    {
        if (dead || player == null) return;

        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) sr.flipX = spriteFacesRight ? dx < 0f : dx > 0f;

        timer -= Time.deltaTime;

        if (!aiming && timer <= 0f) { aiming = true; timer = aimTime; }
        else if (aiming)
        {
            sr.color = Mathf.FloorToInt(Time.time * 14f) % 2 == 0 ? new Color(1f, 0.55f, 0.55f) : Color.white;
            if (timer <= 0f)
            {
                sr.color = Color.white;
                ThrowRing();
                aiming = false; timer = throwCooldown;
            }
        }

        if (cage != null && !netted && !cage.IsFreed)
        {
            netProgress += Time.deltaTime;
            if (netProgress >= netTime)
            {
                netted = true;
                cage.Net();
                if (freya != null) freya.Say("He netted a dragon!");
            }
        }
    }

    void ThrowRing()
    {
        if (ringPrefab == null) return;
        Vector2 origin = throwPoint != null ? (Vector2)throwPoint.position : (Vector2)transform.position + Vector2.up * 0.5f;
        Vector2 target = (Vector2)player.position + Vector2.up * 0.5f;
        var ring = Instantiate(ringPrefab, origin, Quaternion.identity).GetComponent<SpikedRing>();
        if (ring != null) ring.Launch(target - origin, ringSpeed);
        SoundManager.Play(throwSound);
    }

    void OnCollisionEnter2D(Collision2D c) { HurtPlayer(c.collider); }
    void OnCollisionStay2D(Collision2D c)  { HurtPlayer(c.collider); }

    void HurtPlayer(Collider2D other)
    {
        if (dead) return;
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(contactDamage, transform.position);
    }

    public void TakeDamage(int amount, Vector2 from)
    {
        if (dead) return;
        health -= amount;
        sr.color = Color.red; Invoke("ResetColor", 0.1f);
        if (health <= 0) Die();
    }

    void ResetColor() { if (!aiming) sr.color = Color.white; }

    void Die()
    {
        dead = true;
        sr.color = Color.white;
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        rb.isKinematic = true;
        SoundManager.Play(defeatSound);
        if (onDied != null) onDied(this);
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 1.5f)
        { var c = sr.color; c.a = a; sr.color = c; yield return null; }
        gameObject.SetActive(false);
    }
}