using UnityEngine;

/// <summary>
/// Small helper for playing an AudioClip that you dragged into a script in the Inspector
/// (the Crossbow Hunter and Warlord sounds live in their character folders, not in Resources/Audio,
/// so SoundManager.Play("name") can't find them).
///
/// Usage from any script:   ClipPlayer.Play(myClip);
///
/// The sound is played on its own temporary object, so it keeps playing even if the enemy
/// that started it is hidden/destroyed (e.g. a long defeat sound after the enemy fades out).
/// The volume follows the SoundManager's "Sfx Volume" when there is a SoundManager in the scene.
/// </summary>
public static class ClipPlayer
{
    /// Plays the clip once. Returns the AudioSource so you can stop it early if you want.
    public static AudioSource Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return null;

        var go = new GameObject("Sfx_" + clip.name);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;                     // 2D sound: same volume wherever the camera is
        source.volume = volume * (SoundManager.I != null ? SoundManager.I.sfxVolume : 1f);
        source.Play();

        // clean up the temporary object when the sound has finished
        Object.Destroy(go, clip.length + 0.1f);
        return source;
    }

    // ---- Voice lines -------------------------------------------------------------------------
    // Long lines (intros, taunts) must not talk over each other when several enemies notice Erik
    // at the same moment. PlayVoice only plays when no other voice line is still running.

    static float voiceBusyUntil;

    /// Plays the clip only if no other voice line is playing. Returns true if it was played.
    public static bool PlayVoice(AudioClip clip, float volume = 1f)
    {
        if (clip == null || Time.unscaledTime < voiceBusyUntil) return false;
        Play(clip, volume);
        voiceBusyUntil = Time.unscaledTime + clip.length;
        return true;
    }
}
