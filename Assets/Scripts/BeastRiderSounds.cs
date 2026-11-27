using UnityEngine;

/// Put on each Beast Rider (next to BeastRider). Growl when it spots Erik,
/// heavy footsteps when it charges, and a defeat roar when it is beaten.
[RequireComponent(typeof(Rigidbody2D))]
public class BeastRiderSounds : MonoBehaviour
{
    public float growlRange = 7f;          // same as Notice Range on BeastRider
    public float growlCooldown = 3.5f;
    public float chargeSpeed = 5f;         // faster than this = charging

    Rigidbody2D rb;
    Transform player;
    float growlTimer;
    bool charging;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Start()
    {
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;
        if (growlTimer > 0f) growlTimer -= Time.deltaTime;

        float dist = Mathf.Abs(player.position.x - transform.position.x);
        float speed = Mathf.Abs(rb.velocity.x);

        if (!charging && speed < 1f && dist < growlRange && growlTimer <= 0f)
        {
            SoundManager.Play("beast_growl");
            growlTimer = growlCooldown;
        }
        if (!charging && speed > chargeSpeed) { charging = true; SoundManager.Play("beast_charge_steps"); }
        if (charging && speed < 1f) charging = false;
    }

    void OnDisable()
    {
        // BeastRider switches itself off when defeated
        if (gameObject.scene.isLoaded && Time.timeSinceLevelLoad > 0.5f && SoundManager.I != null)
            SoundManager.Play("beast_defeat");
    }
}
