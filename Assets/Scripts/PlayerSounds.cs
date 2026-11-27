using UnityEngine;

/// Put on Erik. Plays his sounds automatically by watching what he does:
/// footsteps, jump, land, axe swing, axe hit, hurt, death, life lost, game over.
public class PlayerSounds : MonoBehaviour
{
    public Transform groundCheck;          // drag the Groundcheck child
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;          // Ground
    public LayerMask enemyLayer;           // Enemy (for the axe hit sound)
    public KeyCode attackKey = KeyCode.X;
    public float stepInterval = 0.28f;     // time between footsteps while running
    public Vector2 hitOffset = new Vector2(0.7f, 0.3f);
    public float hitRange = 0.9f;

    PlayerHealth health;
    SpriteRenderer sr;
    Vector3 lastPos;
    bool wasGrounded = true;
    float lastVelY, stepTimer, attackTimer;
    int lastHealth, lastLives;
    bool wasDead;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        lastPos = transform.position;
        if (health != null) { lastHealth = health.Health; lastLives = health.Lives; }
    }

    bool Grounded()
    {
        if (groundCheck == null) return true;
        var gc = groundCheck.GetComponent<GroundCheck>();
        if (gc != null) return gc.IsGrounded;
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
    }

    void Update()
    {
        if (Time.deltaTime <= 0f) return;
        Vector2 vel = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
        bool grounded = Grounded();
        bool dead = health != null && health.IsDead;

        if (!dead)
        {
            // jump: left the ground going up
            if (wasGrounded && !grounded && vel.y > 2f) SoundManager.Play("player_jump");
            // land: touched the ground after falling
            if (!wasGrounded && grounded && lastVelY < -3f) SoundManager.Play("player_land");

            // footsteps
            if (grounded && Mathf.Abs(vel.x) > 1f)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { SoundManager.Play("footstep_dirt", 0.5f); stepTimer = stepInterval; }
            }
            else stepTimer = 0.05f;

            // attack
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f && Input.GetKeyDown(attackKey) && !PauseMenu.IsPaused)
            {
                attackTimer = 0.4f;
                SoundManager.Play("axe_swing");
                Invoke(nameof(HitSound), 0.15f);
            }
        }

        wasGrounded = grounded;
        if (Mathf.Abs(vel.y) < 50f) lastVelY = vel.y;   // ignore teleport on respawn

        // health and lives
        if (health != null)
        {
            if (health.Lives < lastLives)
            {
                SoundManager.Play("player_death");
                SoundManager.Play(health.Lives > 0 ? "life_lost" : "game_over");
            }
            else if (health.Health < lastHealth) SoundManager.Play("player_hurt");
            if (wasDead && !dead) SoundManager.Play("checkpoint_light", 0.6f);   // back at the checkpoint
            lastHealth = health.Health; lastLives = health.Lives; wasDead = dead;
        }
    }

    void HitSound()
    {
        float dir = (sr != null && sr.flipX) ? -1f : 1f;
        Vector2 p = (Vector2)transform.position + new Vector2(hitOffset.x * dir, hitOffset.y);
        foreach (var col in Physics2D.OverlapCircleAll(p, hitRange, enemyLayer))
        {
            if (col.GetComponentInParent<TrainingDummy>() != null) SoundManager.Play("dummy_hit_wood");
            else { SoundManager.Play("axe_hit"); SoundManager.Play("beast_hit", 0.8f); }
            return;   // one hit sound per swing
        }
    }
}
