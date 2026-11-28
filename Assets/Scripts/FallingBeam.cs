using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingBeam : MonoBehaviour
{
    public int damage = 25;
    public float detectWidth = 2f;
    public float detectHeight = 8f;
    public float warnTime = 0.8f;
    public float fallGravity = 3f;
    public float resetDelay = 6f;

    public string creakSound = "beam_creak";
    public string crashSound = "beam_crash";

    Rigidbody2D rb; Transform player;
    Vector3 startPos; Quaternion startRot;
    bool warning, falling, landed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        startPos = transform.position; startRot = transform.rotation;
        var p = FindObjectOfType<PlayerHealth>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        if (warning)
        {
            transform.position = startPos + new Vector3(Mathf.Sin(Time.time * 80f) * 0.05f, 0f, 0f);
            return;
        }

        if (!falling && !landed)
        {
            float dx = Mathf.Abs(player.position.x - transform.position.x);
            float dy = transform.position.y - player.position.y;
            if (dx < detectWidth / 2f && dy > 0f && dy < detectHeight)
            {
                warning = true;
                SoundManager.Play(creakSound);
                Invoke("Drop", warnTime);
            }
        }
    }

    void Drop()
    {
        warning = false;
        transform.position = startPos;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravity;
        falling = true;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!falling) return;
        var hp = c.collider.GetComponentInParent<PlayerHealth>();
        if (hp != null) { hp.TakeDamage(damage, transform.position); return; }
        Land();
    }

    void Land()
    {
        falling = false; landed = true;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SoundManager.Play(crashSound);
        Invoke("ResetBeam", resetDelay);
    }

    void ResetBeam()
    {
        transform.SetPositionAndRotation(startPos, startRot);
        landed = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y - detectHeight / 2f, 0f),
                            new Vector3(detectWidth, detectHeight, 0f));
    }
}