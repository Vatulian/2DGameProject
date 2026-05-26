using System.Collections;
using UnityEngine;

public static class HitStop
{
    private static int activeStops;
    private static float restoreTimeScale = 1f;

    public static IEnumerator Do(float duration)
    {
        if (duration <= 0f)
            yield break;

        if (activeStops == 0)
        {
            restoreTimeScale = Time.timeScale;
            if (restoreTimeScale <= 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
                yield break;
            }

            Time.timeScale = 0f;
        }

        activeStops++;

        try
        {
            yield return new WaitForSecondsRealtime(duration);
        }
        finally
        {
            activeStops = Mathf.Max(0, activeStops - 1);
            if (activeStops == 0)
                Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
        }
    }

    public static void ForceResume()
    {
        activeStops = 0;
        Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
    }
}
