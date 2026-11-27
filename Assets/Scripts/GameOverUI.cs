using UnityEngine;
using UnityEngine.SceneManagement;

/// Game Over screen with Retry and Main Menu. Put on the Canvas. Drag "GameOverPanel" in.
public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public string mainMenuScene = "MainMenu";

    void Start() { if (panel != null) panel.SetActive(false); }

    public void Show()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Retry()                   // Retry button -> OnClick
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        var hp = FindObjectOfType<PlayerHealth>();
        if (hp != null) hp.ResetLives();
        var rm = FindObjectOfType<RespawnManager>();
        if (rm != null) rm.RespawnWithFullHealth();
    }

    public void MainMenu()                // Main Menu button -> OnClick
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
