using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class levelmanger : MonoBehaviour
{
    public PlayerController player;        // drag Erik here (found automatically if empty)
    public Transform currentCheckpoint;     // drag Checkpoint 1 here
    public float respawnDelay = 0.5f;

    Vector3 startPosition;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (player != null) startPosition = player.transform.position;
    }

    public void Setcheckpoint(Transform checkpoint) { currentCheckpoint = checkpoint; }

    /// <summary>Falling in the pit: back to the checkpoint, health stays the same.</summary>
    public void RespawnPlayer() { if (player != null) StartCoroutine(RespawnRoutine(false)); }

    /// <summary>Retry from Game Over: back to the checkpoint with full health.</summary>
    public void RespawnWithFullHealth() { if (player != null) StartCoroutine(RespawnRoutine(true)); }

    System.Collections.IEnumerator RespawnRoutine(bool fullHealth)
    {
        player.canMove = false;
        player.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(respawnDelay);
        
        Vector3 pos = currentCheckpoint != null ? currentCheckpoint.position : startPosition;
        player.transform.position = pos;
        player.gameObject.SetActive(true);
        player.ResetState();
        if (fullHealth)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null) hp.RestoreFull();
    }
        player.canMove = true;
       }
}
