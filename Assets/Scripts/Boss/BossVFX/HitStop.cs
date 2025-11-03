using System.Collections;
using UnityEngine;

public static class HitStop
{
    // oyun zamanını çok kısa dondurur (arcade hissi)
    public static IEnumerator Do(float duration)
    {
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
    }
}