using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [System.Serializable]
    public struct HintRequest
    {
        [TextArea(2, 6)] public string message;
        public Sprite icon;
    }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Image iconImage;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    private Coroutine fadeRoutine;
    private int showRequestCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null && canvasGroup == null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        ForceHidden();
    }

    public void ShowPersistent(HintRequest request)
    {
        showRequestCount = Mathf.Max(showRequestCount + 1, 1);

        if (panel == null || canvasGroup == null || hintText == null)
            return;

        panel.SetActive(true);
        hintText.text = request.message;

        if (iconImage != null)
        {
            if (request.icon != null)
            {
                iconImage.sprite = request.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        StartFade(1f, fadeInDuration, false);
    }

    public void HidePersistent()
    {
        showRequestCount = Mathf.Max(showRequestCount - 1, 0);

        if (showRequestCount > 0)
            return;

        if (panel == null || canvasGroup == null)
            return;

        StartFade(0f, fadeOutDuration, true);
    }

    public void ForceHidden()
    {
        showRequestCount = 0;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (panel != null)
            panel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void StartFade(float targetAlpha, float duration, bool disableOnEnd)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, disableOnEnd));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool disableOnEnd)
    {
        float startAlpha = canvasGroup.alpha;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;

            if (targetAlpha <= 0f && disableOnEnd)
                panel.SetActive(false);

            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f && disableOnEnd)
            panel.SetActive(false);
    }
}