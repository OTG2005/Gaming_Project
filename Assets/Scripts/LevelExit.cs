using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Loads Level 2 when Erik reaches the exit. Needs a tall BoxCollider2D (Is Trigger).</summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    public string nextSceneName = "Level2";   // must match the scene name in File > Build Settings
    public EnemyWave[] mustClear;              // exit stays closed until these waves are cleared
    public GameObject closedBlocker;           // optional: a collider/barrier that blocks the way until cleared

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    bool AllCleared()
    {
        foreach (var w in mustClear) if (w != null && !w.Cleared) return false;
        return true;
    }

    void Update()
    {
        if (closedBlocker != null) closedBlocker.SetActive(!AllCleared());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null || !AllCleared()) return;
        SceneManager.LoadScene(nextSceneName);
    }
}
