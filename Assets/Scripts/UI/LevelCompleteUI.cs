using System.Collections;
using UnityEngine;

public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float holdDuration = 1.0f;

    private Coroutine routine;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // Fade in
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(holdDuration);
    }
}