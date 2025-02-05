using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private PlayerRespawn playerRespawn; // Referans al
    [SerializeField] private GameObject pauseScreen;
    

    private void Awake()
    {
        gameOverScreen.SetActive(false);
        pauseScreen.SetActive(false);
        playerRespawn = FindObjectOfType<PlayerRespawn>(); // PlayerRespawn script'ini bul
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //If pause screen already active unpause and viceversa
            PauseGame(!pauseScreen.activeInHierarchy);
        }
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

    #region Pause
    public void PauseGame(bool status)
    {
        
        pauseScreen.SetActive(status);

        //When pause status is true change timescale to 0 (time stops)
        //when it's false change it back to 1 (time goes by normally)
        if (status)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
    public void SoundVolume()
    {
        SoundManager.instance.ChangeSoundVolume(0.2f);
    }
    public void MusicVolume()
    {
        SoundManager.instance.ChangeMusicVolume(0.2f);
    }
    #endregion
}

