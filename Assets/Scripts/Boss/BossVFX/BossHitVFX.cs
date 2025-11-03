using System.Collections;
using UnityEngine;

public class BossHitVFX : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem hitSparkPrefab; // küçük “patlama”/kıvılcım
    [SerializeField] private float sparkScale = 1f;

    [Header("Camera Shake (optional)")]
    [SerializeField] private Camera targetCamera;       // boşsa Camera.main
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip hitSfxHeavy;     // boss’a özel tok ses (opsiyonel)

    private Vector3 camOriginalPos;

    private void Awake()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main;
    }

    public void PlayAt(Vector3 worldPos)
    {
        // 1) Hit stop (mikro duraklatma) — arcade hissi çok fark ettirir
        StartCoroutine(HitStop.Do(0.06f));

        // 2) VFX patlama
        if (hitSparkPrefab != null)
        {
            var ps = Instantiate(hitSparkPrefab, worldPos, Quaternion.identity);
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Local;
            ps.transform.localScale = Vector3.one * sparkScale;
            ps.Play();
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
        }

        // 3) Kamera shake
        if (targetCamera != null && shakeMagnitude > 0f && shakeDuration > 0f)
            StartCoroutine(DoShake());

        // 4) SFX
        if (hitSfxHeavy != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(hitSfxHeavy);
    }

    private IEnumerator DoShake()
    {
        camOriginalPos = targetCamera.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // hitstop anında da titresin
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            targetCamera.transform.localPosition = camOriginalPos + new Vector3(x, y, 0f);
            yield return null;
        }
        targetCamera.transform.localPosition = camOriginalPos;
    }
}
