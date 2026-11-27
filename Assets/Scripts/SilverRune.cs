using UnityEngine;

/// <summary>Hidden pickup: heals Erik and counts as found. Needs a Collider2D (Is Trigger).</summary>
[RequireComponent(typeof(Collider2D))]
public class SilverRune : MonoBehaviour
{
    public int healAmount = 20;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;
    public AudioSource pickupSound;        // put the AudioSource on another object (this one gets turned off)

    public static int foundThisLevel;      // read this for the end-of-level score
    Vector3 start;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void Start() { start = transform.position; }

    void Update()
    {
        transform.position = start + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;
        hp.Heal(healAmount);
        foundThisLevel++;
        if (pickupSound != null) pickupSound.Play();
        gameObject.SetActive(false);
    }
}
