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

        if (endPortal == null)
            endPortal = FindFirstObjectByType<LevelEndPortal>(FindObjectsInactive.Include);
    }

    public void ActivateEndPortal()
    {
        if (endPortal != null)
            endPortal.SetActive(true);
    }

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

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
