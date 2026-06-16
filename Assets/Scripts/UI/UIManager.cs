using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject pauseScreen;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;

    [Header("Level Loading")]
    [SerializeField] private string firstLevelName = "Level2";

    [Header("Pause Settings")]
    [SerializeField] private bool enablePauseInThisScene = true;

    [Header("Refs")]
    [SerializeField] private PlayerRespawn playerRespawn;

    private void Awake()
    {
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (pauseScreen) pauseScreen.SetActive(false);

        if (playerRespawn == null)
            playerRespawn = FindObjectOfType<PlayerRespawn>(includeInactive: true);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (playerRespawn == null)
            playerRespawn = FindObjectOfType<PlayerRespawn>(includeInactive: true);

        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (pauseScreen) pauseScreen.SetActive(false);
    }

    private void Update()
    {
        if (!enablePauseInThisScene)
            return;

        if (Input.GetKeyDown(KeyCode.Escape)
            && pauseScreen != null
            && !MerchantUI.BlocksPauseInput)
            PauseGame(!pauseScreen.activeInHierarchy);
    }

    #region Game Over
    public void GameOver()
    {
        if (gameOverScreen) gameOverScreen.SetActive(true);
        if (SoundManager.instance != null && gameOverSound != null)
            SoundManager.instance.PlaySound(gameOverSound);

        Time.timeScale = 0f;
    }

    public void Restart()
    {
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (pauseScreen) pauseScreen.SetActive(false);
        Time.timeScale = 1f;

        if (playerRespawn != null)
        {
            playerRespawn.RestartFromCheckpoint();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }
    #endregion

    #region Pause
    public void PauseGame(bool status)
    {
        if (pauseScreen) pauseScreen.SetActive(status);
        Time.timeScale = status ? 0f : 1f;
    }

    public void Resume() => PauseGame(false);

    public void SoundVolume()
    {
        if (SoundManager.instance != null)
            SoundManager.instance.ChangeSoundVolume(0.2f);
    }

    public void MusicVolume()
    {
        if (SoundManager.instance != null)
            SoundManager.instance.ChangeMusicVolume(0.2f);
    }
    #endregion

    #region Scene Loading
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(firstLevelName))
            SceneManager.LoadScene(firstLevelName);
        else
            Debug.LogWarning("[UIManager] firstLevelName is empty.");
    }

    public void LoadLevel(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("[UIManager] LoadLevel received an invalid scene name.");
    }

    public void LoadLevelByIndex(int buildIndex)
    {
        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(buildIndex);
        else
            Debug.LogWarning("[UIManager] LoadLevelByIndex received an invalid build index.");
    }

    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion
}
