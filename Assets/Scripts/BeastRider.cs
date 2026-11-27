using UnityEngine;

/// <summary>Beast Rider: waits, growls (warning), charges in a straight line, stops, turns, repeats.
/// Put on the rider. Needs Rigidbody2D (Freeze Rotation Z), a Collider2D, Animator, Layer = Enemy.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BeastRider : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 75;             // 3 axe hits
    public int contactDamage = 20;

    [Header("Charge")]
    public float noticeRange = 7f;         // starts the attack when Erik is this close
    public float warnTime = 0.8f;          // growl before charging (time to react)
    public float chargeSpeed = 9f;
    public float chargeDistance = 8f;
    public float restTime = 1.2f;          // tired after charging: best moment to hit him

    [Header("Animator state names")]
    public string idleState = "anim";
    public string runState = "Run";
    public string hitState = "hit";

    [Header("Optional")]
    public AudioSource growlSound;
    public FreyaCallout freya;             // drag Freya here so she shouts the warning

    enum State { Idle, Warn, Charge, Rest, Dead }
    State state = State.Idle;

    Rigidbody2D rb; SpriteRenderer sr; Animator anim;
    Transform player;
    int health; float timer; float startX; float dir;
    string current = "";

    public System.Action<BeastRider> onDied;   // used by EnemyWave

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>(); anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        health = maxHealth;
    }

    void Start()
    {
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
        if (freya == null) freya = FindObjectOfType<FreyaCallout>();
    }

    void Update()
    {
        if (state == State.Dead || player == null) return;
        float dx = player.position.x - transform.position.x;
        timer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                Face(dx);
                Play(idleState);
                if (Mathf.Abs(dx) < noticeRange) StartWarn(dx);
                break;
            case State.Warn:
                // small shake so the player sees it coming
                transform.position += new Vector3(Mathf.Sin(Time.time * 60f) * 0.01f, 0, 0);
                if (timer <= 0f) { state = State.Charge; startX = transform.position.x; Play(runState); }
                break;
            case State.Charge:
                if (Mathf.Abs(transform.position.x - startX) >= chargeDistance)
                { state = State.Rest; timer = restTime; Play(idleState); }
                break;
            case State.Rest:
                if (timer <= 0f) state = State.Idle;
                break;
        }
    }

    void FixedUpdate()
    {
        float vx = state == State.Charge ? dir * chargeSpeed : 0f;
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

    void StartWarn(float dx)
    {
        dir = Mathf.Sign(dx);
        Face(dx);
        state = State.Warn; timer = warnTime;
        if (growlSound != null) growlSound.Play();
        if (freya != null) freya.Say("Watch the charge!");
    }

    void Face(float dx)
    {
        // the sprite sheet faces RIGHT; flip when the player is to the left
        if (Mathf.Abs(dx) > 0.05f) sr.flipX = dx < 0f;
    }

    void OnCollisionEnter2D(Collision2D c) { HurtPlayer(c.collider); }
    void OnCollisionStay2D(Collision2D c)  { HurtPlayer(c.collider); }

    void HurtPlayer(Collider2D other)
    {
        if (state == State.Dead) return;
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(contactDamage, transform.position);
    }

    public void TakeDamage(int amount, Vector2 from)
    {
        if (state == State.Dead) return;
        health -= amount;
        Play(hitState);
        if (health <= 0) Die();
    }
    void Die()
    {
        state = State.Dead;
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        rb.isKinematic = true;
        onDied?.Invoke(this);
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        // knocked out and runs away (no blood) – just fade
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 1.5f)
        { var c = sr.color; c.a = a; sr.color = c; yield return null; }
        gameObject.SetActive(false);
    }

    void Play(string s)
    {
        if (anim == null || s == current) return;
        anim.Play(s, 0, 0f); current = s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noticeRange);
    }
}
