using UnityEngine;

/// Remembers the last lit checkpoint and brings Erik back there.
/// Put on the "Levelmanger" object and drag Checkpoint 1 into Current Checkpoint.
public class RespawnManager : MonoBehaviour
{
    public PlayerHealth player;            // drag Erik here (found automatically if empty)
    public Transform currentCheckpoint;    // drag Checkpoint 1 here
    public float respawnDelay = 1f;
    public Vector2 spawnOffset = new Vector2(0f, 0.5f);   // appear a little above the post's base

    Vector3 startPosition;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerHealth>();
        if (player != null) startPosition = player.transform.position;
    }

    public void SetCheckpoint(Transform checkpoint) { currentCheckpoint = checkpoint; }

    /// Back to the checkpoint, health unchanged.
    public void Respawn() { if (player != null) StartCoroutine(Routine(false)); }

    /// Back to the checkpoint with full health (after losing a life, or Retry).
    public void RespawnWithFullHealth() { if (player != null) StartCoroutine(Routine(true)); }

    System.Collections.IEnumerator Routine(bool fullHealth)
    {
        player.SetMovement(false);
        player.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(respawnDelay);

        Vector3 pos = currentCheckpoint != null
            ? currentCheckpoint.position + (Vector3)spawnOffset
            : startPosition;
        player.transform.position = pos;
        player.gameObject.SetActive(true);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
        if (fullHealth) player.RestoreFull();
        player.SetMovement(true);

        var cam = FindObjectOfType<CameraFollow>();   // snap camera if you use CameraFollow
        if (cam != null) cam.transform.position = new Vector3(pos.x, cam.transform.position.y, cam.transform.position.z);
    }
}
