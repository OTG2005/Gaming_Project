using UnityEngine;

/// <summary>
/// Freya - Erik's AI ally (report 3.1 / 4.1.3): follows Erik, fights hunters next to him,
/// and cannot be defeated. Her speech bubble is the separate FreyaCallout script (put both on her).
///
/// What she does every frame:
///   1. If Erik is far away (fell behind, or Erik respawned at a checkpoint) -> she appears next to him.
///   2. If a hunter is close to her AND close to Erik -> she runs to it and swings her sword.
///   3. Otherwise she follows Erik, staying a little behind him, and jumps when Erik is higher,
///      when a wall is in her way, or to cross a gap Erik already crossed.
///
/// Setup on the Freya object: Rigidbody2D (Freeze Rotation Z), Collider2D, Animator
/// (freya_spritesheet_0 1), this script. Set Ground Layer = Ground and Enemy Layers = Enemy.
///
/// Animator parameters used: Speed (float), Height (float), Grounded (bool), Attack (trigger), Hit (trigger)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Freya_Controller : MonoBehaviour
{
    [Header("Following Erik")]
    public Transform player;                // Erik (found automatically if empty)
    public float followDistance = 1.8f;     // how far behind Erik she stays
    public float runSpeed = 5f;
    public float catchUpSpeed = 7.5f;       // faster when she is far behind
    public float teleportDistance = 14f;    // further than this -> she appears next to Erik

    [Header("Jumping")]
    public float jumpSpeed = 9f;
    public float jumpCooldown = 0.5f;

    [Header("Fighting")]
    public LayerMask enemyLayers;           // tick "Enemy"
    public float engageRange = 6f;          // looks for hunters this close to her...
    public float leashRange = 8f;           // ...that are also this close to Erik (she never runs off alone)
    public float attackRange = 1.3f;
    public int damage = 15;                 // a bit weaker than Erik's axe (25)
    public float attackCooldown = 0.9f;
    public float hitDelay = 0.25f;          // wait until the sword swing lands in the animation

    [Header("Ground")]
    public LayerMask groundLayer;           // tick "Ground"

    [Header("Optional")]
    public bool spriteFacesRight = true;
    public FreyaCallout callout;            // her speech bubble (found on her if empty)

    Rigidbody2D rb; SpriteRenderer sr; Animator anim; Collider2D col;
    Transform target;                       // the hunter she is fighting right now
    float attackTimer, jumpTimer, hitReactTimer;
    bool grounded;

    void Reset()
    {
        groundLayer = LayerMask.GetMask("Ground");
        enemyLayers = LayerMask.GetMask("Enemy");
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        if (enemyLayers.value == 0) enemyLayers = LayerMask.GetMask("Enemy");
        if (callout == null) callout = GetComponent<FreyaCallout>();
    }

    void Start()
    {
        if (player == null)
        {
            var p = FindObjectOfType<PlayerHealth>();
            if (p != null) player = p.transform;
        }
        // Freya and Erik walk through each other instead of pushing each other around
        if (player != null && col != null)
            foreach (var c in player.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(col, c);
    }

    // ------------------------------------------------------------------ brain (every frame)
    void Update()
    {
        UpdateAnimator();
        if (player == null) return;

        grounded = IsGrounded();
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;
        if (hitReactTimer > 0f) hitReactTimer -= Time.deltaTime;

        // Erik is respawning (his object is switched off for a moment) -> just wait
        if (!player.gameObject.activeInHierarchy) { target = null; return; }

        // 1. too far from Erik -> appear next to him
        if (Vector2.Distance(transform.position, player.position) > teleportDistance) { TeleportToErik(); return; }

        // 2. pick a hunter to fight
        target = FindTarget();
        if (target != null)
        {
            float dx = target.position.x - transform.position.x;
            Face(dx);
            if (Mathf.Abs(dx) <= attackRange && attackTimer <= 0f) StartAttack();
        }
    }

    // ------------------------------------------------------------------ movement (physics)
    void FixedUpdate()
    {
        if (player == null || !player.gameObject.activeInHierarchy) { StopX(); return; }
        if (hitReactTimer > 0f) return;                    // being knocked back

        float goalX, speed = runSpeed;
        if (target != null)
        {
            // run up to the hunter and stop just in front of him
            float side = Mathf.Sign(transform.position.x - target.position.x);
            goalX = target.position.x + side * attackRange * 0.7f;
        }
        else
        {
            // stand a little BEHIND Erik (behind = opposite to where he is looking)
            var erikSr = player.GetComponent<SpriteRenderer>();
            float erikFacing = (erikSr != null && erikSr.flipX) ? -1f : 1f;
            goalX = player.position.x - erikFacing * followDistance;
            if (Mathf.Abs(player.position.x - transform.position.x) > followDistance * 3f) speed = catchUpSpeed;
        }

        float dx = goalX - transform.position.x;
        if (Mathf.Abs(dx) < 0.25f) { StopX(); if (target == null) FaceErik(); return; }

        float move = Mathf.Sign(dx);
        rb.velocity = new Vector2(move * speed, rb.velocity.y);
        if (target == null) Face(move);

        if (grounded && jumpTimer <= 0f && ShouldJump(move)) Jump();
    }

    // ------------------------------------------------------------------ fighting
    Transform FindTarget()
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, engageRange, enemyLayers))
        {
            // only things that can be hit (hunters), and only near Erik
            if (hit.GetComponentInParent<IDamageable>() == null) continue;
            if (Vector2.Distance(hit.transform.position, player.position) > leashRange) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < bestDist) { bestDist = d; best = hit.transform; }
        }
        return best;
    }

    void StartAttack()
    {
        attackTimer = attackCooldown;
        if (anim != null) anim.SetTrigger("Attack");
        Invoke(nameof(DoHit), hitDelay);
    }

    void DoHit()
    {
        // same idea as Erik's PlayerAttack: a circle in front of her hurts every enemy inside it
        Vector2 point = (Vector2)transform.position + new Vector2(FacingDir() * attackRange * 0.6f, 0.2f);
        foreach (var hit in Physics2D.OverlapCircleAll(point, attackRange * 0.6f, enemyLayers))
        {
            var enemy = hit.GetComponentInParent<IDamageable>();
            if (enemy != null) enemy.TakeDamage(damage, transform.position);
        }
        SoundManager.Play("axe_swing", 0.6f);
    }

    /// Freya can't be defeated - hunters only knock her back a little (plays her Damage animation).
    void OnCollisionEnter2D(Collision2D c)
    {
        if (((1 << c.gameObject.layer) & enemyLayers.value) == 0 || hitReactTimer > 0f) return;
        hitReactTimer = 0.4f;
        if (anim != null) anim.SetTrigger("Hit");
        float away = Mathf.Sign(transform.position.x - c.transform.position.x);
        rb.velocity = new Vector2(away * 3f, 3f);
    }

    // ------------------------------------------------------------------ following
    bool ShouldJump(float move)
    {
        // Erik is standing higher than her and close (e.g. on a ledge / fortress step)
        if (player.position.y - transform.position.y > 1.2f && Mathf.Abs(player.position.x - transform.position.x) < 4f) return true;
        // a wall is in her way
        if (WallAhead(move)) return true;
        // a gap in front of her, and Erik (or her target) is on the other side
        float goal = target != null ? target.position.x : player.position.x;
        if (!GroundAhead(move) && Mathf.Sign(goal - transform.position.x) == move) return true;
        return false;
    }

    void Jump()
    {
        jumpTimer = jumpCooldown;
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);   // Height > 0 -> Freya_Jump animation
    }

    void TeleportToErik()
    {
        var erikSr = player.GetComponent<SpriteRenderer>();
        float erikFacing = (erikSr != null && erikSr.flipX) ? -1f : 1f;
        transform.position = player.position + new Vector3(-erikFacing * followDistance, 0.3f, 0f);
        rb.velocity = Vector2.zero;
        target = null;
    }

    // ------------------------------------------------------------------ helpers
    void StopX() { rb.velocity = new Vector2(0f, rb.velocity.y); }

    void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("Height", rb.velocity.y);
        anim.SetBool("Grounded", IsGrounded());
    }

    void FaceErik() { if (player != null) Face(player.position.x - transform.position.x); }

    void Face(float dx)
    {
        if (Mathf.Abs(dx) > 0.05f) sr.flipX = spriteFacesRight ? dx < 0f : dx > 0f;
    }

    float FacingDir() { return (sr.flipX == spriteFacesRight) ? -1f : 1f; }

    bool IsGrounded()
    {
        if (col == null) return true;
        Bounds b = col.bounds;
        return Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f), 0f, groundLayer);
    }

    bool WallAhead(float move)
    {
        if (col == null) return false;
        Vector2 from = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.3f);
        return Physics2D.Raycast(from, new Vector2(move, 0f), col.bounds.extents.x + 0.3f, groundLayer);
    }

    bool GroundAhead(float move)
    {
        if (col == null) return true;
        Vector2 from = new Vector2(col.bounds.center.x + move * (col.bounds.extents.x + 0.4f), col.bounds.min.y + 0.1f);
        return Physics2D.Raycast(from, Vector2.down, 3f, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;  Gizmos.DrawWireSphere(transform.position, engageRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
