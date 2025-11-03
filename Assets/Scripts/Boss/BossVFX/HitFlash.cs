using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;  // genelde beyaz
    [SerializeField] private float flashDuration = 0.12f;     // ilk testte biraz uzun tut
    [SerializeField] private float intensity = 2.0f;          // 1=normal, 2–3=parlak (HDR)

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = false;    // timeScale etkilenmesin istersen

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip hitSfx;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private SpriteRenderer[] sprites;
    private Color[] originalColors;
    private MaterialPropertyBlock mpb;
    private Coroutine flashRoutine;

    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i].color;

        mpb = new MaterialPropertyBlock();

        if (logDebug)
            Debug.Log($"[HitFlash] Found {sprites.Length} SpriteRenderer(s) on {name}");
    }

    public void Play()
    {
        if (sprites == null || sprites.Length == 0) return;

        if (hitSfx && SoundManager.instance)
            SoundManager.instance.PlaySound(hitSfx);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashOnce());
    }

    private IEnumerator FlashOnce()
    {
        // Hedef rengi (HDR parlaklık için >1 çarpıyoruz)
        Color bright = flashColor * intensity;
        if (bright.a <= 0f) bright.a = 1f; // görünür kalsın

        // UYGULA: hem color, hem de property block (URP/Lit/Unlit için)
        for (int i = 0; i < sprites.Length; i++)
        {
            // 1) SpriteRenderer.color
            sprites[i].color = bright;

            // 2) MaterialPropertyBlock ile shader renkleri (_BaseColor ve _Color)
            sprites[i].GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", bright); // URP Sprite Lit/Unlit
            mpb.SetColor("_Color", bright);     // Built-in Sprites/Default
            sprites[i].SetPropertyBlock(mpb);
        }

        if (useUnscaledTime) yield return new WaitForSecondsRealtime(flashDuration);
        else yield return new WaitForSeconds(flashDuration);

        // GERİ AL
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = originalColors[i];

            sprites[i].GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", originalColors[i]);
            mpb.SetColor("_Color", originalColors[i]);
            sprites[i].SetPropertyBlock(mpb);
        }

        flashRoutine = null;
    }
}
