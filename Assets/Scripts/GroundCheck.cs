using UnityEngine;

/// Put this on the "Groundcheck" child at Erik's feet.
/// IsGrounded is true while the small circle touches anything on the Ground layer.
public class GroundCheck : MonoBehaviour
{
    public float radius = 0.2f;        // size of the green circle
    public LayerMask groundLayer;      // tick "Ground"

    public bool IsGrounded { get; private set; }

    void FixedUpdate()
    {
        IsGrounded = Physics2D.OverlapCircle(transform.position, radius, groundLayer);
    }

    // shows the circle in the Scene view: green = on ground, red = in the air
    void OnDrawGizmos()
    {
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}