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
    [SerializeField] private string firstLevelName = "Level1"; // PlayGame() burayı yükler

    [Header("Pause Settings")]
    [SerializeField] private bool enablePauseInThisScene = true; // MainMenu'de false yapabilirsin

    [Header("Refs")]
    [SerializeField] private PlayerRespawn playerRespawn; // opsiyonel

    private void Awake()
    {
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (pauseScreen)    pauseScreen.SetActive(false);

        // Oyun sahnesindeysek playerRespawn bul; menüde yoksa null kalır
        if (playerRespawn == null)
            playerRespawn = FindObjectOfType<PlayerRespawn>(includeInactive: true);

        // Sahne değişince pause kalmasın
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Sahne değişince her ihtimale karşı zaman akışını normalle
        Time.timeScale = 1f;

        // Yeni sahnede PlayerRespawn yeniden bulunabilir
        if (playerRespawn == null)
            playerRespawn = FindObjectOfType<PlayerRespawn>(includeInactive: true);

        // Ekranlar reset
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (pauseScreen)    pauseScreen.SetActive(false);
    }

    private void Update()
    {
        if (!enablePauseInThisScene) return; // menü sahnesinde pause devre dışı

        if (Input.GetKeyDown(KeyCode.Escape) && pauseScreen != null)
        {
            // Açık ise kapat, kapalı ise aç
            PauseGame(!pauseScreen.activeInHierarchy);
        }
    }

    #region Game Over
    public void GameOver()
    {
        if (gameOverScreen) gameOverScreen.SetActive(true);
        if (SoundManager.instance != null && gameOverSound != null)
            SoundManager.instance.PlaySound(gameOverSound);

        // İstersen burada oyunu dondurabilirsin:
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        if (gameOverScreen) gameOverScreen.SetActive(false);
        
        Time.timeScale = 1f;  // Oyunu tekrar akışa sok

        if (playerRespawn != null)
        {
            playerRespawn.RestartFromCheckpoint();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (playerRespawn != null)
        {
            // Checkpoint'ten devam
            playerRespawn.RestartFromCheckpoint();
            // Time.timeScale = 1f; // respawn içinde yönetiyorsan gerek yok
        }
        else
        {
            // Yedek: sahneyi baştan yükle (aktif sahne)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void MainMenu()
    {
        // Build Settings'te 0. index menü ise:
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
    // Menülerde kullanmak için net bir Play tuşu:
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(firstLevelName))
            SceneManager.LoadScene(firstLevelName);
        else
            Debug.LogWarning("[UIManager] firstLevelName boş, Level1 ismiyle eşle!");
    }

    public void LoadLevel(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("[UIManager] LoadLevel: Geçersiz sahne adı.");
    }

    public void LoadLevelByIndex(int buildIndex)
    {
        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(buildIndex);
        else
            Debug.LogWarning("[UIManager] LoadLevelByIndex: Geçersiz index.");
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
