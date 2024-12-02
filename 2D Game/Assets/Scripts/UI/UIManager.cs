using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private PlayerRespawn playerRespawn; // Referans al

    private void Awake()
    {
        gameOverScreen.SetActive(false);
        playerRespawn = FindObjectOfType<PlayerRespawn>(); // PlayerRespawn script'ini bul
    }

    #region Game Over Functions
    // Game over function
    public void GameOver()
    {
        gameOverScreen.SetActive(true);
        SoundManager.instance.PlaySound(gameOverSound);
    }

    // Restart level
    public void Restart()
    {
        gameOverScreen.SetActive(false);
        playerRespawn.RestartFromCheckpoint();
    }

    // Activate game over screen
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    // Quit game/exit play mode if in Editor
    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion
}
