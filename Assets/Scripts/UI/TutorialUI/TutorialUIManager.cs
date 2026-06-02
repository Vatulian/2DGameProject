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

    [Header("Layout")]
    [SerializeField] private bool configureLayoutOnAwake = true;
    [SerializeField] private Vector2 panelSize = new Vector2(600f, 120f);
    [SerializeField] private float contentWidth = 360f;
    [SerializeField] private float iconSlotWidth = 64f;
    [SerializeField] private float iconSize = 56f;
    [SerializeField] private float contentSpacing = 16f;
    [SerializeField] private float textHeight = 96f;
    [SerializeField] private float textFontSize = 28f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    private Coroutine fadeRoutine;
    private Coroutine temporaryRoutine;
    private int showRequestCount;
    private bool hasPersistentRequest;
    private HintRequest currentPersistentRequest;

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

        if (configureLayoutOnAwake)
            ApplyLayout();
    }

    private void Start()
    {
        ForceHidden();
    }

    public void ShowPersistent(HintRequest request)
    {
        showRequestCount = Mathf.Max(showRequestCount + 1, 1);
        currentPersistentRequest = request;
        hasPersistentRequest = true;

        if (panel == null || canvasGroup == null || hintText == null)
            return;

        if (temporaryRoutine == null)
            RenderRequest(request);

        StartFade(1f, fadeInDuration, false);
    }

    public void HidePersistent()
    {
        showRequestCount = Mathf.Max(showRequestCount - 1, 0);

        if (showRequestCount > 0)
            return;

        hasPersistentRequest = false;

        if (temporaryRoutine != null)
            return;

        if (panel == null || canvasGroup == null)
            return;

        StartFade(0f, fadeOutDuration, true);
    }

    public void ShowTemporary(string message, float duration = 1.5f)
    {
        ShowTemporary(new HintRequest { message = message }, duration);
    }

    public void ShowTemporary(HintRequest request, float duration = 1.5f)
    {
        if (panel == null || canvasGroup == null || hintText == null)
            return;

        if (temporaryRoutine != null)
            StopCoroutine(temporaryRoutine);

        temporaryRoutine = StartCoroutine(TemporaryRoutine(request, duration));
    }

    public void ForceHidden()
    {
        showRequestCount = 0;
        hasPersistentRequest = false;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (temporaryRoutine != null)
        {
            StopCoroutine(temporaryRoutine);
            temporaryRoutine = null;
        }

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

    private void RenderRequest(HintRequest request)
    {
        panel.SetActive(true);
        hintText.text = request.message;

        if (iconImage != null)
        {
            if (request.icon != null)
            {
                iconImage.sprite = request.icon;
                iconImage.preserveAspect = true;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (configureLayoutOnAwake)
            ApplyLayout();
    }

    private IEnumerator TemporaryRoutine(HintRequest request, float duration)
    {
        RenderRequest(request);
        StartFade(1f, fadeInDuration, false);

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        temporaryRoutine = null;

        if (showRequestCount > 0 && hasPersistentRequest)
        {
            RenderRequest(currentPersistentRequest);
            StartFade(1f, fadeInDuration, false);
        }
        else
        {
            StartFade(0f, fadeOutDuration, true);
        }
    }

    private void ApplyLayout()
    {
        RectTransform panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;
        RectTransform textRect = hintText != null ? hintText.rectTransform : null;
        RectTransform iconRect = iconImage != null ? iconImage.rectTransform : null;

        if (panelRect == null || textRect == null)
            return;

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);

        float safeIconSlotWidth = iconImage != null && iconImage.gameObject.activeSelf ? iconSlotWidth : 0f;
        float safeSpacing = safeIconSlotWidth > 0f ? contentSpacing : 0f;
        float textWidth = Mathf.Max(1f, contentWidth - safeIconSlotWidth - safeSpacing);
        float contentLeft = -contentWidth * 0.5f;

        if (iconRect != null)
        {
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(contentLeft + safeIconSlotWidth * 0.5f, 0f);
            iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconSize);
            iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize);
        }

        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = new Vector2(contentLeft + safeIconSlotWidth + safeSpacing, 0f);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        hintText.alignment = TextAlignmentOptions.MidlineLeft;
        hintText.enableWordWrapping = true;
        hintText.overflowMode = TextOverflowModes.Truncate;
        hintText.fontSize = textFontSize;
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
