using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossDamageFeedback : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform visualRoot;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float flashIntensity = 3f;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.055f;

    [Header("Spark")]
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private float sparkScale = 1.2f;

    [Header("Shockwave")]
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private float shockwaveFactor = 0.6f;
    [SerializeField] private float shockwaveLifetime = 0.12f;
    [SerializeField] private bool placeShockwaveAtHit;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.07f;
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;

    private SpriteRenderer[] sprites;
    private Color[] originalColors;
    private MaterialPropertyBlock propertyBlock;
    private SpriteRenderer mainSprite;
    private Coroutine flashRoutine;
    private float previousHealth;

    private void Awake()
    {
        ResolveReferences();
        CacheSprites();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (health != null)
        {
            previousHealth = health.CurrentHealth;
            health.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0f, flashDuration);
        flashIntensity = Mathf.Max(0f, flashIntensity);
        hitStopDuration = Mathf.Max(0f, hitStopDuration);
        sparkScale = Mathf.Max(0f, sparkScale);
        shockwaveFactor = Mathf.Max(0f, shockwaveFactor);
        shockwaveLifetime = Mathf.Max(0f, shockwaveLifetime);
        shakeDuration = Mathf.Max(0f, shakeDuration);
        shakeMagnitude = Mathf.Max(0f, shakeMagnitude);
    }

    public void PlayAt(Vector3 hitWorldPosition)
    {
        if (hitStopDuration > 0f)
            StartCoroutine(HitStop.Do(hitStopDuration));

        PlayFlash();
        PlaySpark(hitWorldPosition);
        PlayShockwave(hitWorldPosition);
        PlayCameraShake();
        PlayAudio();
    }

    private void HandleDamaged(float currentHealth)
    {
        if (currentHealth >= previousHealth)
        {
            previousHealth = currentHealth;
            return;
        }

        previousHealth = currentHealth;
        PlayAt(health != null ? health.LastDamagePoint : transform.position);
    }

    private void PlayFlash()
    {
        if (sprites == null || sprites.Length == 0 || flashDuration <= 0f)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashOnce());
    }

    private IEnumerator FlashOnce()
    {
        Color bright = flashColor * flashIntensity;
        if (bright.a <= 0f)
            bright.a = 1f;

        for (int i = 0; i < sprites.Length; i++)
            ApplySpriteColor(sprites[i], bright);

        yield return new WaitForSecondsRealtime(flashDuration);

        for (int i = 0; i < sprites.Length; i++)
            ApplySpriteColor(sprites[i], originalColors[i]);

        flashRoutine = null;
    }

    private void ApplySpriteColor(SpriteRenderer sprite, Color color)
    {
        if (sprite == null)
            return;

        sprite.color = color;
        sprite.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        sprite.SetPropertyBlock(propertyBlock);
    }

    private void PlaySpark(Vector3 hitWorldPosition)
    {
        if (hitSparkPrefab == null)
            return;

        Vector3 position = hitWorldPosition;
        position.z = 0f;

        ParticleSystem spark = Instantiate(hitSparkPrefab, position, Quaternion.Euler(0f, 0f, 90f));
        ParticleSystem.MainModule main = spark.main;
        main.useUnscaledTime = true;
        spark.transform.localScale = Vector3.one * sparkScale;

        ParticleSystemRenderer renderer = spark.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && mainSprite != null)
        {
            renderer.sortingLayerID = mainSprite.sortingLayerID;
            renderer.sortingOrder = mainSprite.sortingOrder + 1;
        }

        spark.Play();
        Destroy(spark.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
    }

    private void PlayShockwave(Vector3 hitWorldPosition)
    {
        if (shockwavePrefab == null)
            return;

        float bossWidth = mainSprite != null ? mainSprite.bounds.size.x : 2f;
        float scale = bossWidth * shockwaveFactor;
        Vector3 position = placeShockwaveAtHit ? hitWorldPosition : transform.position;
        position.z = 0f;

        GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
        shockwave.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer shockwaveSprite = shockwave.GetComponent<SpriteRenderer>();
        if (shockwaveSprite != null && mainSprite != null)
        {
            shockwaveSprite.sortingLayerID = mainSprite.sortingLayerID;
            shockwaveSprite.sortingOrder = mainSprite.sortingOrder + 2;
        }

        Destroy(shockwave, shockwaveLifetime);
    }

    private void PlayCameraShake()
    {
        if (cameraController == null || shakeDuration <= 0f || shakeMagnitude <= 0f)
            return;

        cameraController.Shake(shakeDuration, shakeMagnitude);
    }

    private void PlayAudio()
    {
        if (hitSfx != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(hitSfx);
    }

    private void ResolveReferences()
    {
        if (health == null)
            health = GetComponent<Health>() ?? GetComponentInParent<Health>() ?? GetComponentInChildren<Health>(true);

        if (cameraController == null && Camera.main != null)
            cameraController = Camera.main.GetComponent<CameraController>();

        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();
    }

    private void CacheSprites()
    {
        Transform spriteSearchRoot = visualRoot != null ? visualRoot : GetSpriteSearchRoot();
        sprites = spriteSearchRoot.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i] != null ? sprites[i].color : Color.white;

        propertyBlock = new MaterialPropertyBlock();
        mainSprite = sprites.Length > 0 ? sprites[0] : null;
    }

    private Transform GetSpriteSearchRoot()
    {
        if (health != null && health.transform.parent != null)
            return health.transform.parent;

        if (transform.parent != null)
            return transform.parent;

        return transform;
    }
}
