using UnityEngine;

public class DragonCage : MonoBehaviour, IDamageable
{
    public static int rescuedTotal;

    public int hitsToBreak = 2;
    public Sprite brokenSprite;
    public Transform dragon;
    public SpriteRenderer glow;
    public float glowSpeed = 2.5f;
    public FreyaCallout freya;

    public string hitSound = "axe_hit";
    public string breakSound = "sfx_cage_break";
    public string freedSound = "sfx_baby_dragon_chirp";
    public string nettedSound = "sfx_baby_dragon_distress";

    public bool IsFreed { get; private set; }
    public bool IsNetted { get; private set; }

    int hits; float shake;
    Vector3 startPos;
    SpriteRenderer sr;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        if (freya == null) freya = FindObjectOfType<FreyaCallout>();
    }

    void Update()
    {
        if (glow != null)
        {
            var c = glow.color;
            c.a = (IsFreed || IsNetted) ? 0f : 0.3f + 0.3f * Mathf.Sin(Time.time * glowSpeed);
            glow.color = c;
        }
        if (shake > 0f)
        {
            shake -= Time.deltaTime;
            transform.position = startPos + new Vector3(Mathf.Sin(Time.time * 70f) * 0.06f, 0f, 0f);
            if (shake <= 0f) transform.position = startPos;
        }
    }

    public void TakeDamage(int amount, Vector2 from)
    {
        if (IsFreed || IsNetted) return;
        hits++; shake = 0.2f;
        SoundManager.Play(hitSound);
        if (hits >= hitsToBreak) Break();
    }

    void Break()
    {
        IsFreed = true;
        rescuedTotal++;
        if (brokenSprite != null) sr.sprite = brokenSprite; else sr.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        SoundManager.Play(breakSound);
        SoundManager.Play(freedSound);

        if (freya != null)
        {
            int freed = 0, all = 0;
            foreach (var c in FindObjectsOfType<DragonCage>()) { all++; if (c.IsFreed) freed++; }
            freya.Say("Dragon freed! " + freed + "/" + all);
        }
        if (dragon != null) StartCoroutine(FlyAway());
    }

    public void Net()
    {
        if (IsFreed || IsNetted) return;
        IsNetted = true;
        sr.color = new Color(0.5f, 0.5f, 0.5f);
        SoundManager.Play(nettedSound);
    }

    System.Collections.IEnumerator FlyAway()
    {
        var dsr = dragon.GetComponentInChildren<SpriteRenderer>();
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            dragon.position += new Vector3(1f, 0.7f, 0f) * 4f * Time.deltaTime;
            if (dsr != null) { var c = dsr.color; c.a = 1f - t / 3f; dsr.color = c; }
            yield return null;
        }
        dragon.gameObject.SetActive(false);
    }
}