using UnityEngine;

/// <summary>
/// Crossbow Hunter (Level 5) - a patient marksman who shoots from the fortress walls.
/// Also used for the Crossbow ARMY soldiers: same script, just give them the "crossbow_army"
/// override controller and lower stats.
///
/// Attack cycle (report 4.1.5 / 5.3):
///   Idle -> Aim (crossbow raised + glint + click = warning) -> Shoot (bolt flies at Erik)
///        -> Reload (crossbow lowered: THIS is the moment to hit him) -> Idle ...
/// If Erik gets too close while the hunter is NOT reloading, the hunter jumps back to keep his distance,
/// so the player has to wait for the reload to land his hits.
///
/// Setup: Rigidbody2D (Freeze Rotation Z), Collider2D, Animator (crossbow_hunter_spritesheet_0 or
/// crossbow_army), Layer = Enemy. Drag the "CrossbowBolt" prefab into Bolt Prefab.
///
/// Animator parameters used (they already exist in the controller):
///   Speed (float), Height (float), Grounded (bool), Aiming (bool), Shoot (trigger),
///   Reloading (bool), Hit (trigger), Dead (bool)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CrossbowHunter : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 50;              // 2 axe hits (axe = 25 damage)
    public int contactDamage = 10;          // touching him hurts a little

    [Header("Seeing Erik")]
    public float noticeRange = 12f;         // starts shooting when Erik is this close
    public float aimHeight = 0.6f;          // aims at Erik's chest, not his feet

    [Header("Shooting")]
    public GameObject boltPrefab;           // drag the CrossbowBolt prefab here
    public Transform firePoint;             // optional: an empty child at the tip of the crossbow
    public float aimTime = 0.8f;            // warning time before the shot (glint + click)
    public float boltSpeed = 11f;
    public float reloadTime = 1.6f;         // the "hit him now" window
    public float timeBetweenShots = 0.4f;   // short pause after reloading before aiming again

    [Header("Keeping distance")]
    public float retreatRange = 2.2f;       // Erik closer than this (and not reloading) -> jump back
    public Vector2 retreatJump = new Vector2(4f, 6f);  // backwards speed, upwards speed
    public float retreatCooldown = 2.5f;    // can't jump back again right away

    [Header("Patrol (optional)")]
    public bool patrol = false;             // walk left/right while he hasn't seen Erik
    public float patrolDistance = 2f;
    public float walkSpeed = 1.5f;

    [Header("Ground")]
    public LayerMask groundLayer;           // tick "Ground"

    [Header("Sounds (drag the wav files from the crossbow_hunter folder)")]
    public AudioClip introClip;             // CrossBow_Hunter_Intro - first time he sees Erik
    public AudioClip shootClip;             // Crossbow_Attack
    public AudioClip damageClip;            // Crossbow_Damage
    public AudioClip defeatClip;            // Crossbow_Defeat
    public AudioClip tauntClip;             // Crossbow_taunt - when his bolt hits Erik

    [Header("Optional")]
    public SpriteRenderer glint;            // a small shiny sprite on the crossbow, flashes while aiming
    public FreyaCallout freya;              // Freya shouts a hint the first time
    public bool spriteFacesRight = true;    // the sprite sheets face RIGHT
    public bool fadeOutWhenDefeated = true; // knocked out -> fades away (no blood)

    enum State { Idle, Aim, Reload, Retreat, Dead }
    State state = State.Idle;

    Rigidbody2D rb; SpriteRenderer sr; Animator anim; Collider2D col;
    Transform player;
    int health;
    float timer;                // counts down the current Aim / Reload / Retreat
    float shotTimer;            // pause before the next aim
    float retreatTimer;         // cooldown of the jump back
    float startX; int patrolDir = 1;
    bool noticedErik, grounded;

    public bool IsDead { get { return state == State.Dead; } }
    public System.Action<CrossbowHunter> onDied;     // for waves / locked gates (like BeastRider)

    void Reset() { groundLayer = LayerMask.GetMask("Ground"); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        health = maxHealth;
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        if (glint != null) glint.enabled = false;
    }

    void Start()
    {
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
        if (freya == null) freya = FindObjectOfType<FreyaCallout>();
        startX = transform.position.x;
    }

    // ------------------------------------------------------------------ brain (every frame)
    void Update()
    {
        UpdateAnimator();
        if (state == State.Dead || player == null) return;

        grounded = IsGrounded();
        float dx = player.position.x - transform.position.x;
        bool seesErik = player.gameObject.activeInHierarchy &&
                        Vector2.Distance(transform.position, player.position) <= noticeRange;

        if (shotTimer > 0f) shotTimer -= Time.deltaTime;
        if (retreatTimer > 0f) retreatTimer -= Time.deltaTime;
        timer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (!seesErik) break;                       // FixedUpdate does the patrol walk
                Face(dx);
                if (!noticedErik) OnFirstSeeErik();

                if (Mathf.Abs(dx) < retreatRange && retreatTimer <= 0f && grounded && HasGroundBehind())
                    StartRetreat(dx);
                else if (shotTimer <= 0f && grounded)
                    StartAim();
                break;

            case State.Aim:
                Face(dx);
                // flashing glint = "he's about to shoot!"
                if (glint != null) glint.enabled = Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
                if (!seesErik) { CancelAim(); break; }        // Erik ran away: lower the crossbow
                if (timer <= 0f) Fire();
                break;

            case State.Reload:
                if (timer <= 0f)
                {
                    AnimBool("Reloading", false);
                    state = State.Idle;
                    shotTimer = timeBetweenShots;
                }
                break;

            case State.Retreat:
                // back on the ground after the jump -> carry on
                if (timer <= 0f && grounded) state = State.Idle;
                break;
        }
    }

    // ------------------------------------------------------------------ movement (physics)
    void FixedUpdate()
    {
        if (state == State.Dead) return;
        if (state == State.Retreat) return;                // keep the speed of the jump

        float vx = 0f;
        bool seesErik = player != null && player.gameObject.activeInHierarchy &&
                        Vector2.Distance(transform.position, player.position) <= noticeRange;
        if (state == State.Idle && patrol && !seesErik)
        {
            // walk between startX - patrolDistance and startX + patrolDistance
            if (transform.position.x > startX + patrolDistance) patrolDir = -1;
            if (transform.position.x < startX - patrolDistance) patrolDir = 1;
            if (!HasGroundAhead(patrolDir)) patrolDir = -patrolDir;   // don't walk off the wall
            vx = patrolDir * walkSpeed;
            Face(patrolDir);
        }
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

    // ------------------------------------------------------------------ actions
    void OnFirstSeeErik()
    {
        noticedErik = true;
        ClipPlayer.PlayVoice(introClip);
        if (freya != null) freya.Say("Crossbows! Hit them while they reload!");
    }

    void StartAim()
    {
        state = State.Aim;
        timer = aimTime;
        AnimBool("Aiming", true);
    }

    void CancelAim()
    {
        state = State.Idle;
        AnimBool("Aiming", false);
        if (glint != null) glint.enabled = false;
        shotTimer = timeBetweenShots;
    }

    void Fire()
    {
        if (glint != null) glint.enabled = false;
        AnimTrigger("Shoot");                 // Aim -> Shoot -> (Reloading) -> Reload
        AnimBool("Aiming", false);
        AnimBool("Reloading", true);
        ClipPlayer.Play(shootClip);

        if (boltPrefab != null)
        {
            // the bolt starts at the crossbow and flies towards Erik's chest.
            // The direction is chosen NOW, so Erik can still dodge it by moving or jumping.
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position
                                               : (Vector2)transform.position + new Vector2(FacingDir() * 0.6f, 0.3f);
            Vector2 target = (Vector2)player.position + Vector2.up * aimHeight;
            var bolt = Instantiate(boltPrefab, origin, Quaternion.identity).GetComponent<CrossbowBolt>();
            if (bolt != null)
            {
                bolt.Launch(target - origin, boltSpeed);
                bolt.onHitPlayer += () => ClipPlayer.PlayVoice(tauntClip);
            }
        }
        else Debug.LogWarning(name + ": no Bolt Prefab - drag the CrossbowBolt prefab in.");

        state = State.Reload;
        timer = reloadTime;
    }

    void StartRetreat(float dx)
    {
        state = State.Retreat;
        timer = 0.3f;                              // minimum time in the air before checking the ground
        retreatTimer = retreatCooldown;
        float away = -Mathf.Sign(dx);
        rb.velocity = new Vector2(away * retreatJump.x, retreatJump.y);   // Height > 0 -> Jumping anim
    }

    // ------------------------------------------------------------------ getting hit
    public void TakeDamage(int amount, Vector2 from)
    {
        if (state == State.Dead) return;
        health -= amount;
        ClipPlayer.Play(damageClip);

        if (health <= 0) { Die(); return; }

        AnimTrigger("Hit");
        // being hit while aiming spoils the shot
        if (state == State.Aim) CancelAim();
    }

    void Die()
    {
        state = State.Dead;
        if (glint != null) glint.enabled = false;
        AnimBool("Aiming", false);
        AnimBool("Reloading", false);
        AnimBool("Dead", true);               // Any State -> Defeat
        ClipPlayer.Play(defeatClip);

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        if (col != null) col.enabled = false;
        onDied?.Invoke(this);
        if (fadeOutWhenDefeated) StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.8f);     // let the Defeat animation play first
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 1.5f)
        { var c = sr.color; c.a = a; sr.color = c; yield return null; }
        gameObject.SetActive(false);
    }

    // touching the hunter hurts Erik a little (same as the other hunters)
    void OnCollisionEnter2D(Collision2D c) { HurtPlayer(c.collider); }
    void OnCollisionStay2D(Collision2D c)  { HurtPlayer(c.collider); }

    void HurtPlayer(Collider2D other)
    {
        if (state == State.Dead) return;
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(contactDamage, transform.position);
    }

    // ------------------------------------------------------------------ helpers
    // (checks the Animator exists, so the script also works on an object without one)
    void AnimBool(string param, bool value) { if (anim != null) anim.SetBool(param, value); }
    void AnimTrigger(string param)          { if (anim != null) anim.SetTrigger(param); }

    void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("Height", rb.velocity.y);
        anim.SetBool("Grounded", state == State.Dead || IsGrounded());
    }

    void Face(float dx)
    {
        if (Mathf.Abs(dx) > 0.05f) sr.flipX = spriteFacesRight ? dx < 0f : dx > 0f;

        // flipX only mirrors the picture, so move the crossbow-tip children to the side he faces
        float side = FacingDir();
        if (firePoint != null) MirrorX(firePoint, side);
        if (glint != null && glint.transform != transform) MirrorX(glint.transform, side);
    }

    static void MirrorX(Transform t, float side)
    {
        var p = t.localPosition;
        p.x = Mathf.Abs(p.x) * side;
        t.localPosition = p;
    }

    float FacingDir() { return (sr.flipX == spriteFacesRight) ? -1f : 1f; }

    // small box under the feet touching the Ground layer?
    bool IsGrounded()
    {
        if (col == null) return true;
        Bounds b = col.bounds;
        return Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f), 0f, groundLayer);
    }

    bool HasGroundAhead(float dir) { return HasGroundAt(dir * 0.8f); }
    bool HasGroundBehind()         { return HasGroundAt(-FacingDir() * (retreatJump.x * 0.5f + 0.5f)); }

    bool HasGroundAt(float offsetX)
    {
        if (col == null) return true;
        Vector2 from = new Vector2(col.bounds.center.x + offsetX, col.bounds.min.y + 0.1f);
        return Physics2D.Raycast(from, Vector2.down, 2f, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, noticeRange);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}
