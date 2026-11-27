using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>ESC opens/closes the pause menu. Put on the Canvas. Drag the "PausePanel" in.</summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public string mainMenuScene = "MainMenu";
    public static bool IsPaused { get; private set; }

    void Start() { Resume(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Resume()                  // Resume button → OnClick
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void ToggleSound()             // Sound button → OnClick
    {
        AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
    }

    public void MainMenu()                // Main Menu button → OnClick
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
