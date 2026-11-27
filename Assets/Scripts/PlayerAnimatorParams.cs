using UnityEngine;

/// Sends Erik's movement to the Animator parameters: Speed, hiegth, ground, attack, damage.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimatorParams : MonoBehaviour
{
    public Animator animator;          // leave empty = uses the Animator on Erik
    public Transform groundCheck;      // drag the "Groundcheck" child here
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;      // tick "Ground"
    public KeyCode attackKey = KeyCode.X;

    Rigidbody2D rb;
    PlayerHealth health;
    int lastHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        health = GetComponent<PlayerHealth>();
    }

    void Start() { if (health != null) lastHealth = health.Health; }
    Vector3 lastPos;
    void Update()
    {
        bool grounded = false;
        if (groundCheck != null)
        {
            var gc = groundCheck.GetComponent<GroundCheck>();
            grounded = gc != null
                ? gc.IsGrounded
                : Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        }

        // real movement speed, works with Rigidbody OR transform movement
        Vector2 vel = rb.velocity;
        if (Time.deltaTime > 0f && lastPos != Vector3.zero)
        {
            Vector2 measured = (transform.position - lastPos) / Time.deltaTime;
            if (Mathf.Abs(measured.x) > Mathf.Abs(vel.x)) vel.x = measured.x;
            if (Mathf.Abs(measured.y) > Mathf.Abs(vel.y)) vel.y = measured.y;
        }
        lastPos = transform.position;

        animator.SetFloat("Speed", Mathf.Abs(vel.x));
        animator.SetFloat("height", vel.y);      // <-- same spelling as your parameter
        animator.SetBool("ground", grounded);

        if (Input.GetKeyDown(attackKey) && !PauseMenu.IsPaused)
            animator.SetTrigger("attack");

        if (health != null)
        {
            if (health.Health < lastHealth) animator.SetTrigger("damage");
            lastHealth = health.Health;
        }
    }
}