using System.Collections.Generic;
using UnityEngine;

/// One object that plays ALL the sounds of the level.
/// Put it on an empty object called "SoundManager".
/// It loads every clip inside  Assets/Resources/Audio  automatically and finds them by file name,
/// e.g. SoundManager.Play("axe_swing").  "footstep_dirt" picks one of footstep_dirt_1/2/3 at random.
public class SoundManager : MonoBehaviour
{
    public static SoundManager I;

    [Header("Music and ambience (file names in Resources/Audio)")]
    public string musicName = "bgm_level1_gate_loop";
    [Range(0, 1)] public float musicVolume = 0.45f;
    public string[] ambienceNames = { "amb_fire_loop", "amb_wind_loop" };
    [Range(0, 1)] public float ambienceVolume = 0.3f;

    [Header("Sound effects")]
    [Range(0, 1)] public float sfxVolume = 0.9f;
    public float pitchVariation = 0.06f;          // small random pitch so repeats don't sound robotic

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly List<AudioSource> pool = new List<AudioSource>();
    AudioSource musicSource;
    int next;

    void Awake()
    {
        I = this;
        foreach (var c in Resources.LoadAll<AudioClip>("Audio")) clips[c.name] = c;
        if (clips.Count == 0) Debug.LogWarning("SoundManager: no clips found in Assets/Resources/Audio");

        for (int i = 0; i < 10; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            pool.Add(s);
        }
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    void Start()
    {
        if (clips.TryGetValue(musicName, out var m))
        {
            musicSource.clip = m; musicSource.volume = musicVolume; musicSource.Play();
        }
        foreach (var n in ambienceNames)
        {
            if (!clips.TryGetValue(n, out var a)) continue;
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = a; s.loop = true; s.volume = ambienceVolume;
            s.time = Random.Range(0f, a.length);      // so loops don't start in sync
            s.Play();
        }
    }

    AudioClip Find(string name)
    {
        if (clips.TryGetValue(name, out var c)) return c;
        // variations: name_1, name_2, ...
        var list = new List<AudioClip>();
        foreach (var kv in clips) if (kv.Key.StartsWith(name + "_")) list.Add(kv.Value);
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    /// Use from any script:  SoundManager.Play("player_jump");
    public static void Play(string name, float volume = 1f)
    {
        if (I != null) I.PlayNamed(name, volume);
    }

    /// Use from a UI Button OnClick (pick SoundManager > PlaySound and type the name).
    public void PlaySound(string name) { PlayNamed(name, 1f); }

    public void PlayNamed(string name, float volume)
    {
        var clip = Find(name);
        if (clip == null) { Debug.LogWarning("SoundManager: no sound called " + name); return; }
        var s = pool[next]; next = (next + 1) % pool.Count;
        s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        s.PlayOneShot(clip, sfxVolume * volume);
    }

    /// Plays a sound that keeps going while the next scene loads (e.g. level_complete).
    public static void PlayAcrossScenes(string name)
    {
        if (I == null) return;
        var clip = I.Find(name);
        if (clip == null) return;
        var go = new GameObject("Sound_" + name);
        DontDestroyOnLoad(go);
        var s = go.AddComponent<AudioSource>();
        s.clip = clip; s.volume = I.sfxVolume; s.Play();
        Destroy(go, clip.length + 0.1f);
    }

    public void SetMusicVolume(float v) { musicVolume = v; if (musicSource) musicSource.volume = v; }
}
