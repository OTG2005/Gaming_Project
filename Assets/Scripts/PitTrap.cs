using UnityEngine;

/// Put inside a gap in the ground (Collider2D with Is Trigger).
/// loseLife ON  = falling costs a life (back to checkpoint, or Game Over if none left)
/// loseLife OFF = just back to the checkpoint, no life lost
[RequireComponent(typeof(Collider2D))]
public class PitTrap : MonoBehaviour
{
    public bool loseLife = true;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;
        if (loseLife) hp.Kill();
        else { var rm = FindObjectOfType<RespawnManager>(); if (rm != null) rm.Respawn(); }
    }
}
