using UnityEngine;

/// Erik's health AND lives. Put on Erik.
/// Health 0 -> lose 1 life -> back to the last lit checkpoint with full health.
/// No lives left -> Game Over screen.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public float invincibleTime = 1f;      // can't be hit again for this long after a hit
    public float knockback = 5f;
    public float hitStunTime = 0.25f;      // movement switched off this long after a hit

    [Header("Lives")]
    public int startLives = 3;
    public float respawnDelay = 1f;        // pause before coming back at the checkpoint

    [Header("Links")]
    public string movementScriptName = "PlayerController";
    public HealthBarUI healthBar;          // drag the HealthBar here
    public LivesUI livesUI;                // drag the Lives object here
    public GameOverUI gameOver;            // drag the Canvas (with GameOverUI) here

    public int Health { get; private set; }
    public int Lives { get; private set; }
    public bool IsDead { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Behaviour movement;
    float invincibleTimer, stunTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent(movementScriptName) as Behaviour;
        Health = maxHealth;
        Lives = startLives;
    }

    void Start() { UpdateUI(); }

    void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
            if (sr != null) sr.enabled = invincibleTimer <= 0f || Mathf.FloorToInt(invincibleTimer * 15f) % 2 == 0;
        }
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f && !IsDead) SetMovement(true);
        }
    }

    public void SetMovement(bool on) { if (movement != null) movement.enabled = on; }

    public void TakeDamage(int amount, Vector2 fromPosition)
    {
        if (IsDead || invincibleTimer > 0f) return;
        Health = Mathf.Max(0, Health - amount);
        UpdateUI();

        if (Health <= 0) { Die(); return; }

        float dir = Mathf.Sign(transform.position.x - fromPosition.x);
        SetMovement(false);
        stunTimer = hitStunTime;
        rb.velocity = new Vector2(dir * knockback, knockback * 0.6f);
        invincibleTimer = invincibleTime;
    }

    /// Instant death (for pits, spikes, falling off the level).
    public void Kill()
    {
        if (IsDead) return;
        Health = 0;
        UpdateUI();
        Die();
    }

    void Die()
    {
        IsDead = true;
        if (sr != null) sr.enabled = true;
        SetMovement(false);
        rb.velocity = Vector2.zero;
        Lives = Mathf.Max(0, Lives - 1);
        UpdateUI();

        if (Lives > 0)
        {
            var rm = FindObjectOfType<RespawnManager>();
            if (rm != null) { rm.respawnDelay = respawnDelay; rm.RespawnWithFullHealth(); }
            else Debug.LogError("PlayerHealth: no RespawnManager in the scene - add it to the Levelmanger object.");
        }
        else if (gameOver != null) gameOver.Show();
        else Debug.LogError("PlayerHealth: no lives left but Game Over is not linked.");
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        UpdateUI();
    }

    /// Called by RespawnManager after moving Erik to the checkpoint.
    public void RestoreFull()
    {
        IsDead = false;
        Health = maxHealth;
        invincibleTimer = 1.5f;            // short safety blink after respawn
        stunTimer = 0f;
        SetMovement(true);
        UpdateUI();
    }

    /// Called by Game Over -> Retry: lives back to full.
    public void ResetLives()
    {
        Lives = startLives;
        UpdateUI();
    }

    public void AddLife() { Lives++; UpdateUI(); }

    void UpdateUI()
    {
        if (healthBar != null) healthBar.SetValue((float)Health / maxHealth);
        if (livesUI != null) livesUI.SetLives(Lives);
    }
}
