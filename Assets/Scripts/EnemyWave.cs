using System.Collections.Generic;
using UnityEngine;

/// <summary>Turns a group of Beast Riders on when Erik walks into this trigger.
/// Put on an empty object with a tall BoxCollider2D (Is Trigger). Drag the riders into "riders"
/// and switch the rider objects OFF in the Hierarchy (untick them).</summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyWave : MonoBehaviour
{
    public List<BeastRider> riders = new List<BeastRider>();
    public bool Cleared { get; private set; }

    bool started; int alive;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (started || other.GetComponentInParent<PlayerHealth>() == null) return;
        started = true;
        alive = riders.Count;
        foreach (var r in riders)
        {
            r.gameObject.SetActive(true);
            r.onDied += _ => { alive--; if (alive <= 0) Cleared = true; };
        }
        if (alive == 0) Cleared = true;
    }
}
