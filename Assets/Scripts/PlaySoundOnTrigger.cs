using UnityEngine;

/// Plays a sound once when Erik walks into this trigger.
/// Use it for: checkpoints (checkpoint_light), the silver rune (rune_pickup),
/// Freya lines at a spot (freya_more_hunters, freya_gate_clear), the level exit (level_complete).
[RequireComponent(typeof(Collider2D))]
public class PlaySoundOnTrigger : MonoBehaviour
{
    public string soundName = "checkpoint_light";
    [Range(0, 1)] public float volume = 1f;
    public bool onlyOnce = true;
    public bool keepPlayingIntoNextScene = false;   // tick for level_complete on the exit

    bool done;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done && onlyOnce) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        done = true;
        if (keepPlayingIntoNextScene) SoundManager.PlayAcrossScenes(soundName);
        else SoundManager.Play(soundName, volume);
    }
}
