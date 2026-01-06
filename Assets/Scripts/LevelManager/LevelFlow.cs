using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFlow : MonoBehaviour
{
    public static LevelFlow Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LevelCompleteUI levelCompleteUI;
    [SerializeField] private LevelEndPortal endPortal;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float waitAfterUISeconds = 1.0f;

    private bool completed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Boss ölünce burayı çağır
    public void ActivateEndPortal()
    {
        if (endPortal != null)
            endPortal.SetActive(true);
    }

    // Portal E ile burayı çağırıyor
    public void CompleteLevel()
    {
        if (completed) return;
        completed = true;

        if (levelCompleteUI != null)
            levelCompleteUI.Play();

        StartCoroutine(LoadNextRoutine());
    }

    private IEnumerator LoadNextRoutine()
    {
        yield return new WaitForSecondsRealtime(waitAfterUISeconds);

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        // Build Settings'teki sahne sayısına göre karar ver
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            // başka level yok → Main Menu
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}