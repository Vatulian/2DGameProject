using System.Collections;
using UnityEngine;

public class BossHitVFX : MonoBehaviour
{
    [Header("Small spark (constant size)")]
    [SerializeField] private ParticleSystem hitSparkPrefab;   // küçük kıvılcım
    [SerializeField] private float sparkScale = 1.2f;

    [Header("Shockwave (scales by boss size)")]
    [SerializeField] private GameObject shockwavePrefab;      // tek frame flash/ring (Sprite ya da PS)
    [SerializeField] private float shockwaveFactor = 0.6f;    // boss genişliğinin yüzde kaçı
    [SerializeField] private float shockwaveLifetime = 0.12f; // çok kısa
    [SerializeField] private bool placeShockwaveAtHit = false; // true: vurulan nokta, false: boss merkezi

    [Header("Camera Shake")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float shakeDuration = 0.07f;
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfxHeavy;

    private SpriteRenderer bossSR;

    void Awake()
    {
        if (targetCamera == null && Camera.main) targetCamera = Camera.main;
        bossSR = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlayAt(Vector3 hitWorldPos)
    {
        // mikro hit-stop
        StartCoroutine(HitStop.Do(0.055f));

        // 1) küçük spark (sabit boy)
        if (hitSparkPrefab)
        {
            var pos = hitWorldPos; pos.z = 0f;
            var ps = Instantiate(hitSparkPrefab, pos, Quaternion.Euler(0,0,90));
            var main = ps.main;
            main.useUnscaledTime = true;
            ps.transform.localScale = Vector3.one * sparkScale;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend && bossSR)
            {
                rend.sortingLayerID = bossSR.sortingLayerID;
                rend.sortingOrder   = bossSR.sortingOrder + 1;
                if (!rend.sharedMaterial) rend.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            ps.Play();
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
        }

        // 2) shockwave (boss boyuna göre)
        if (shockwavePrefab)
        {
            // Boss genişliğini al
            float bossWidth = bossSR ? bossSR.bounds.size.x : 2f;
            float scale = bossWidth * shockwaveFactor;

            Vector3 pos = (placeShockwaveAtHit ? hitWorldPos : transform.position);
            pos.z = 0f;

            var go = Instantiate(shockwavePrefab, pos, Quaternion.identity);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // eğer SpriteRenderer ise sorting’i ayarla
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr && bossSR)
            {
                sr.sortingLayerID = bossSR.sortingLayerID;
                sr.sortingOrder   = bossSR.sortingOrder + 2;
            }

            // kısa ömür
            Destroy(go, shockwaveLifetime);
        }

        // 3) kamera shake
        if (targetCamera && shakeMagnitude > 0f && shakeDuration > 0f)
            StartCoroutine(DoShake());

        // 4) sfx
        if (hitSfxHeavy && SoundManager.instance)
            SoundManager.instance.PlaySound(hitSfxHeavy);
    }

    private IEnumerator DoShake()
    {
        var orig = targetCamera.transform.localPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float x = Random.Range(-1f,1f) * shakeMagnitude;
            float y = Random.Range(-1f,1f) * shakeMagnitude;
            targetCamera.transform.localPosition = orig + new Vector3(x,y,0);
            yield return null;
        }
        targetCamera.transform.localPosition = orig;
    }
}
