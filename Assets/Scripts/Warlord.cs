using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The Warlord - final boss of Level 5 (report 3.1, 4.1.5, 5.2.4, 5.3).
/// Also used for the Warlord's ARMY (spear soldiers): same script with "Is Boss" OFF,
/// the "warlord_army" override controller and lower stats.
///
/// He has 3 attacks. Each one starts with a clear WIND-UP pose (the warning), then the attack:
///   1 STRIKE  - spear raised overhead, then brought down in front of him   -> step back
///   2 LUNGE   - leans back, then dashes forward with a thrust              -> jump over / get away
///   3 SWEEP   - crouches low, then sweeps the floor on BOTH sides          -> JUMP over it
/// After the SWEEP he is out of breath (Recover). That is the opening: Freya shouts
/// "He's open! Strike now!". As a boss he blocks axe hits outside his openings.
/// 250 health = 10 axe hits (report: "10 hits to defeat").
///
/// Setup: Rigidbody2D (Freeze Rotation Z), Collider2D, Animator (warlord_spritesheet_0 or
/// warlord_army), Layer = Enemy.
///
/// Animator parameters used (they already exist in the controller):
///   Speed (float), Height (float), Grounded (bool), Attack (int: 0 none, 1 strike, 2 lunge, 3 sweep),
///   Release (trigger), Vulnerable (bool), Hit (trigger), Dead (bool)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Warlord : MonoBehaviour, IDamageable
{
    [Header("Boss or army soldier")]
    public bool isBoss = true;              // OFF for the army soldiers
    public bool blockOutsideOpening = true; // only takes damage while Recovering / resting (boss)

    [Header("Stats")]
    public int maxHealth = 250;             // 10 axe hits of 25
    public int contactDamage = 10;          // walking into him hurts a little

    [Header("Moving")]
    public float activateRange = 10f;       // the fight starts when Erik is this close (or call StartFight())
    public float runSpeed = 3.5f;
    public bool canJump = true;             // jumps up to Erik if he is standing on something higher
    public float jumpSpeed = 9f;

    [Header("Choosing an attack")]
    public float closeRange = 2.5f;         // Erik this close -> Strike or Sweep
    public float lungeRange = 6f;           // Erik this close (but not close) -> Lunge
    public int sweepAtLeastEvery = 3;       // makes sure a Sweep (= the opening) comes regularly

    [Header("Strike (overhead)")]
    public int strikeDamage = 20;
    public float strikeWindup = 0.6f;
    public Vector2 strikeBox = new Vector2(2.2f, 2.4f);   // area in front of him that gets hit

    [Header("Lunge (forward dash)")]
    public int lungeDamage = 15;
    public float lungeWindup = 0.5f;
    public float lungeSpeed = 11f;
    public float lungeTime = 0.35f;
    public Vector2 lungeBox = new Vector2(1.6f, 1.8f);

    [Header("Sweep (low, both sides)")]
    public int sweepDamage = 20;
    public float sweepWindup = 0.7f;
    public Vector2 sweepBox = new Vector2(6f, 0.7f);      // wide and LOW: jump over it
    public float recoverTime = 1.6f;                       // the opening after the sweep

    [Header("Timing")]
    public float activeTime = 0.15f;        // how long a Strike / Sweep can hit after it is released
    public float restTime = 0.6f;           // short pause after Strike / Lunge
    public float staggerTime = 0.4f;        // army soldiers: a hit during the wind-up cancels the attack
    public float angryWindupMultiplier = 0.75f;  // below half health the boss winds up faster

    [Header("Boss health bar (optional)")]
    public GameObject bossBarRoot;          // the whole bar - hidden until the fight starts
    public HealthBarUI bossBar;             // the HealthBarUI on it

    [Header("Sounds (drag the wav files from the warlord folder)")]
    public AudioClip introClip;             // Warlord_Intro - when the fight starts
    public AudioClip startAttackClip;       // Warlord_Initiate_Attck - his first attack
    public AudioClip attackClip;            // Warlord_Mid_Attack - each attack
    public AudioClip damageClip;            // Warlord_Damage - when he is hurt
    public AudioClip defeatClip;            // Warlord_Defeat
    public AudioClip blockClip;             // optional: metal "clink" when he blocks the axe

    [Header("When he is defeated")]
    public UnityEvent onDefeated = new UnityEvent();   // e.g. open the final cage / gate, start the ending
    public bool fadeOutWhenDefeated = false;// boss stays knocked out on the floor; soldiers fade

    [Header("Optional")]
    public FreyaCallout freya;
    public bool spriteFacesRight = true;
    public LayerMask groundLayer;           // tick "Ground"

    enum State { Waiting, Chase, Windup, Attacking, Rest, Recover, Dead }
    State state = State.Waiting;

    const int NONE = 0, STRIKE = 1, LUNGE = 2, SWEEP = 3;

    Rigidbody2D rb; SpriteRenderer sr; Animator anim; Collider2D col;
    Transform player;
    int health, currentAttack, lastAttack, attacksSinceSweep;
    float timer, dir = 1f;
    bool hitThisAttack, firstAttackDone, toldToWait, grounded;

    public bool IsDead { get { return state == State.Dead; } }
    public bool IsVulnerable { get { return state == State.Recover || state == State.Rest || !blockOutsideOpening; } }
    public System.Action<Warlord> onDied;   // for waves / locked gates (like BeastRider)

    void Reset() { groundLayer = LayerMask.GetMask("Ground"); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        health = maxHealth;
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
    }

    void Start()
    {
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
        if (freya == null) freya = FindObjectOfType<FreyaCallout>();
    }

    // ------------------------------------------------------------------ brain (every frame)
    void Update()
    {
        UpdateAnimator();
        if (state == State.Dead || player == null) return;

        grounded = IsGrounded();
        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        bool erikHere = player.gameObject.activeInHierarchy;   // false while Erik respawns
        timer -= Time.deltaTime;

        switch (state)
        {
            case State.Waiting:
                if (erikHere && Vector2.Distance(transform.position, player.position) <= activateRange) StartFight();
                break;

            case State.Chase:
                if (!erikHere) break;                    // wait for Erik to come back
                Face(dx);
                int attack = ChooseAttack(dist);
                if (attack != NONE && grounded) StartWindup(attack);
                else if (canJump && grounded && player.position.y - transform.position.y > 1.5f && dist < 4f)
                    rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);   // -> Jumping / Falling animations
                break;

            case State.Windup:
                // The wind-up pose is the warning. He keeps facing Erik until he releases.
                if (currentAttack != LUNGE) Face(dx);
                if (timer <= 0f) Release();
                break;

            case State.Attacking:
                if (!hitThisAttack) CheckHit();          // can hit Erik once per attack
                if (timer <= 0f) EndAttack();
                break;

            case State.Rest:
                if (timer <= 0f) state = State.Chase;
                break;

            case State.Recover:
                if (timer <= 0f)
                {
                    AnimBool("Vulnerable", false);       // Recover -> Standing
                    state = State.Chase;
                }
                break;
        }
    }

    // ------------------------------------------------------------------ movement (physics)
    void FixedUpdate()
    {
        if (state == State.Dead) return;
        float vx = 0f;

        if (state == State.Chase && player != null && player.gameObject.activeInHierarchy)
        {
            float dx = player.position.x - transform.position.x;
            // run towards Erik until he is close enough to attack (Speed > 0 -> Running animation)
            if (Mathf.Abs(dx) > closeRange * 0.8f && HasGroundAhead(Mathf.Sign(dx)))
                vx = Mathf.Sign(dx) * runSpeed * (Angry() ? 1.2f : 1f);
        }
        else if (state == State.Attacking && currentAttack == LUNGE)
        {
            // the lunge dash - stops at the edge of a platform
            vx = HasGroundAhead(dir) ? dir * lungeSpeed : 0f;
        }
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

    // ------------------------------------------------------------------ the fight
    /// Starts the fight. Called automatically when Erik comes close, or from an arena trigger.
    public void StartFight()
    {
        if (state != State.Waiting) return;
        state = State.Chase;
        if (isBoss)
        {
            ClipPlayer.PlayVoice(introClip);
            if (bossBarRoot != null) bossBarRoot.SetActive(true);
            UpdateBossBar();
            if (freya != null) freya.Say("The Warlord! Watch his spear!");
        }
    }

    int ChooseAttack(float dist)
    {
        int pick = NONE;
        if (dist <= closeRange)
        {
            // close: Sweep if it's "due", otherwise Strike or Sweep at random
            if (attacksSinceSweep >= sweepAtLeastEvery - 1) pick = SWEEP;
            else pick = Random.value < 0.5f ? STRIKE : SWEEP;
        }
        else if (dist <= lungeRange) pick = LUNGE;

        // don't do the same non-sweep attack 3 times in a row (keeps him readable but not boring)
        if (pick != NONE && pick == lastAttack && pick != SWEEP && Random.value < 0.6f)
            pick = (dist <= closeRange) ? SWEEP : LUNGE;
        return pick;
    }

    void StartWindup(int attack)
    {
        currentAttack = attack;
        state = State.Windup;
        dir = FacingDir();                       // the lunge goes where he faced at the start
        AnimInt("Attack", attack);               // Any State -> Strike_Windup / Lunge_Windup / Sweep_Windup

        float t = attack == STRIKE ? strikeWindup : attack == LUNGE ? lungeWindup : sweepWindup;
        timer = Angry() ? t * angryWindupMultiplier : t;

        if (!firstAttackDone) { firstAttackDone = true; ClipPlayer.PlayVoice(startAttackClip); }

        // Freya reads his posture for the player (report 5.3)
        if (isBoss && freya != null)
        {
            if (attack == STRIKE) freya.Say("Overhead! Step back!");
            else if (attack == LUNGE) freya.Say("He's lunging! Jump!");
            else freya.Say("Low sweep! Jump over it!");
        }
    }

    void Release()
    {
        state = State.Attacking;
        hitThisAttack = false;
        AnimTrigger("Release");                  // windup -> attack animation
        AnimInt("Attack", NONE);
        ClipPlayer.Play(attackClip);
        timer = currentAttack == LUNGE ? lungeTime : activeTime;
        CheckHit();
    }

    void EndAttack()
    {
        lastAttack = currentAttack;
        if (currentAttack == SWEEP)
        {
            // THE OPENING: out of breath after the sweep
            attacksSinceSweep = 0;
            state = State.Recover;
            timer = recoverTime;
            AnimBool("Vulnerable", true);        // Sweep -> Recover
            if (isBoss && freya != null) freya.Say("He's open! Strike now!");
        }
        else
        {
            attacksSinceSweep++;
            state = State.Rest;
            timer = restTime;
        }
        currentAttack = NONE;
    }

    /// Looks for Erik inside the current attack's box and hurts him (once per attack).
    void CheckHit()
    {
        Vector2 center, size;
        int damage;
        Bounds b = col != null ? col.bounds : new Bounds(transform.position, Vector3.one * 2f);

        if (currentAttack == STRIKE)
        { size = strikeBox; center = new Vector2(b.center.x + dir * (b.extents.x + size.x * 0.5f), b.center.y); damage = strikeDamage; }
        else if (currentAttack == LUNGE)
        { size = lungeBox; center = new Vector2(b.center.x + dir * (b.extents.x + size.x * 0.5f), b.center.y); damage = lungeDamage; }
        else
        { size = sweepBox; center = new Vector2(b.center.x, b.min.y + size.y * 0.5f); damage = sweepDamage; }   // low, both sides

        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            var hp = hit.GetComponentInParent<PlayerHealth>();
            if (hp != null) { hp.TakeDamage(damage, transform.position); hitThisAttack = true; return; }
        }
    }

    // ------------------------------------------------------------------ getting hit
    public void TakeDamage(int amount, Vector2 from)
    {
        if (state == State.Dead) return;
        if (state == State.Waiting) StartFight();          // hitting him starts the fight

        if (!IsVulnerable)
        {
            // blocked: flash white, metal clink, Freya reminds the player of the plan (once)
            StartCoroutine(Flash(new Color(0.75f, 0.85f, 1f)));
            ClipPlayer.Play(blockClip);
            if (isBoss && freya != null && !toldToWait) { toldToWait = true; freya.Say("He's blocking! Wait for his sweep!"); }
            return;
        }

        health -= amount;
        UpdateBossBar();
        StartCoroutine(Flash(new Color(1f, 0.5f, 0.5f)));
        if (damageClip != null) ClipPlayer.PlayVoice(damageClip);   // voice: won't stack when hit quickly

        if (health <= 0) { Die(); return; }

        // army soldier hit during his wind-up -> the attack is cancelled (stagger)
        if (!isBoss && state == State.Windup)
        {
            AnimInt("Attack", NONE);
            currentAttack = NONE;
            state = State.Rest;
            timer = staggerTime;
        }
        // the boss doesn't flinch while attacking (only in his openings)
        if (!isBoss || state == State.Recover || state == State.Rest) AnimTrigger("Hit");
    }

    void Die()
    {
        state = State.Dead;
        AnimInt("Attack", NONE);
        AnimBool("Vulnerable", false);
        AnimBool("Dead", true);                  // Any State -> Defeat (knocked out, no blood)
        if (isBoss) ClipPlayer.PlayVoice(defeatClip); else ClipPlayer.Play(defeatClip);
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (isBoss && freya != null) freya.Say("He's down! Free the dragon!");

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        if (col != null) col.enabled = false;
        onDied?.Invoke(this);
        onDefeated.Invoke();
        if (fadeOutWhenDefeated) StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1f);     // let the Defeat animation play first
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 1.5f)
        { var c = sr.color; c.a = a; sr.color = c; yield return null; }
        gameObject.SetActive(false);
    }

    System.Collections.IEnumerator Flash(Color c)
    {
        sr.color = c;
        yield return new WaitForSeconds(0.1f);
        if (state != State.Dead || !fadeOutWhenDefeated) sr.color = Color.white;
    }

    void OnCollisionEnter2D(Collision2D c) { HurtPlayer(c.collider); }
    void OnCollisionStay2D(Collision2D c)  { HurtPlayer(c.collider); }

    void HurtPlayer(Collider2D other)
    {
        if (state == State.Dead || state == State.Waiting) return;
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(contactDamage, transform.position);
    }

    // ------------------------------------------------------------------ helpers
    bool Angry() { return isBoss && health <= maxHealth / 2; }

    void UpdateBossBar() { if (bossBar != null) bossBar.SetValue((float)Mathf.Max(health, 0) / maxHealth); }

    void AnimBool(string param, bool value) { if (anim != null) anim.SetBool(param, value); }
    void AnimInt(string param, int value)   { if (anim != null) anim.SetInteger(param, value); }
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
    }

    float FacingDir() { return (sr.flipX == spriteFacesRight) ? -1f : 1f; }

    bool IsGrounded()
    {
        if (col == null) return true;
        Bounds b = col.bounds;
        return Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, 0.1f), 0f, groundLayer);
    }

    bool HasGroundAhead(float d)
    {
        if (col == null) return true;
        Vector2 from = new Vector2(col.bounds.center.x + d * (col.bounds.extents.x + 0.3f), col.bounds.min.y + 0.1f);
        return Physics2D.Raycast(from, Vector2.down, 2f, groundLayer);
    }

    // draws the attack areas in the Scene view when the Warlord is selected
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider2D>();
        Bounds b = c != null ? c.bounds : new Bounds(transform.position, Vector3.one * 2f);
        float d = (GetComponent<SpriteRenderer>() != null && GetComponent<SpriteRenderer>().flipX) ? -1f : 1f;
        Gizmos.color = Color.yellow;  Gizmos.DrawWireSphere(transform.position, activateRange);
        Gizmos.color = Color.red;     Gizmos.DrawWireCube(new Vector2(b.center.x + d * (b.extents.x + strikeBox.x * 0.5f), b.center.y), strikeBox);
        Gizmos.color = new Color(1f, 0.5f, 0f); Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y + sweepBox.y * 0.5f), sweepBox);
    }
}
