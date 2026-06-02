using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    private const string PromptText = "Press E";

    private static InteractionPromptUI instance;
    private static readonly HashSet<MonoBehaviour> requesters = new();

    private CanvasGroup canvasGroup;

    public static void Show(MonoBehaviour requester)
    {
        if (requester == null)
            return;

        requesters.Add(requester);
        GetOrCreate().SetVisible(HasActiveRequesters());
    }

    public static void Hide(MonoBehaviour requester)
    {
        if (requester != null)
            requesters.Remove(requester);

        if (instance != null)
            instance.SetVisible(HasActiveRequesters());
    }

    private static InteractionPromptUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject root = new GameObject("InteractionPromptUI");
        instance = root.AddComponent<InteractionPromptUI>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private static bool HasActiveRequesters()
    {
        requesters.RemoveWhere(requester => requester == null || !requester.isActiveAndEnabled);
        return requesters.Count > 0;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUI();
        SetVisible(false);
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 120f);
        panelRect.sizeDelta = new Vector2(210f, 54f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.text = PromptText;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 30f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = new Color(1f, 1f, 1f, 0.92f);
        promptText.raycastTarget = false;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
    }
}
