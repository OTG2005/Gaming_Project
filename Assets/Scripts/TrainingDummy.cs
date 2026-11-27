using UnityEngine;

/// <summary>Wobbles when Erik hits it. Never breaks. Put on the dummy (Layer = Enemy, needs a Collider2D).</summary>
public class TrainingDummy : MonoBehaviour, IDamageable
{
    public float wobbleAngle = 12f;
    public float wobbleTime = 0.4f;
    public AudioSource hitSound;
    float t;

    public void TakeDamage(int amount, Vector2 from)
    {
        t = wobbleTime;
        if (hitSound != null) hitSound.Play();
    }

    void Update()
    {
        if (t > 0f) t -= Time.deltaTime;
        float a = t > 0f ? Mathf.Sin(t * 40f) * wobbleAngle * (t / wobbleTime) : 0f;
        transform.localRotation = Quaternion.Euler(0, 0, a);
    }
}
