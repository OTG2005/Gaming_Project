using UnityEngine;

/// <summary>A villager that runs away to the right once the gate area is clear.
/// Put on each villager (no Rigidbody needed).</summary>
public class Villager : MonoBehaviour
{
    public EnemyWave waitForWave;          // the last wave; leave empty to run straight away
    public float speed = 3.5f;
    public float bobAmount = 0.05f;
    public float disappearAfter = 12f;     // seconds of running before it is removed

    bool running; float runTime; Vector3 basePos;

    void Start() { basePos = transform.position; }

    void Update()
    {
        if (!running && (waitForWave == null || waitForWave.Cleared)) running = true;
        if (!running) return;

        runTime += Time.deltaTime;
        basePos += Vector3.right * speed * Time.deltaTime;
        transform.position = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(runTime * 12f)) * bobAmount;
        if (runTime > disappearAfter) gameObject.SetActive(false);
    }
}
