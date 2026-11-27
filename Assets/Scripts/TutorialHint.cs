using UnityEngine;

/// <summary>A hint sign that fades away for good once the player does the move it shows.
/// Put on the sign. Add a BoxCollider2D (Is Trigger) wide enough to cover the area in front of it.</summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialHint : MonoBehaviour
{
    public enum Action { Move, Jump, Attack }
    public Action action = Action.Move;
    public float fadeTime = 0.6f;

    SpriteRenderer[] renderers;
    bool playerNear, done;
    float fade = 1f;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void Awake() { renderers = GetComponentsInChildren<SpriteRenderer>(); }

    void OnTriggerEnter2D(Collider2D o) { if (o.GetComponentInParent<PlayerHealth>() != null) playerNear = true; }
    void OnTriggerExit2D(Collider2D o)  { if (o.GetComponentInParent<PlayerHealth>() != null) playerNear = false; }

    void Update()
    {
        if (!done && playerNear && DidAction()) done = true;

        if (done)
        {
            fade -= Time.deltaTime / fadeTime;
            foreach (var r in renderers) { var c = r.color; c.a = Mathf.Clamp01(fade); r.color = c; }
            if (fade <= 0f) gameObject.SetActive(false);
        }
    }

    bool DidAction()
    {
        switch (action)
        {
            case Action.Move:   return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
            case Action.Jump:   return Input.GetKeyDown(KeyCode.Space);
            default:            return Input.GetKeyDown(KeyCode.X);
        }
    }
}
