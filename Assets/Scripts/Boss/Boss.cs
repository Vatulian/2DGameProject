using UnityEngine;
using UnityEngine.UI;
using System;

public class Boss : MonoBehaviour, IBossEncounterTarget
{
    private Vector3 spawnPosition;
    private bool spawnPositionInitialized;

    [Header("Stats")]
    public int health = 20;
    public int damage = 1;

    [SerializeField] private float damageCooldown = 1.5f;
    private float timeBtwDamage;

    [Header("UI")]
    [SerializeField] private Slider healthBar;

    [Header("Components")]
    private Animator anim;
    private HitFlash hitFlash;
    private BossHitVFX hitVfx;
    private BossDamageFeedback damageFeedback;

    public bool isDead;
    private bool stageTwoTriggered;
    public event Action Defeated;

    public bool IsEncounterDefeated => isDead;

    [Header("Door On Death")]
    [SerializeField] private DoorController doorOnDeath;
    [SerializeField] private ActivationTarget[] additionalDeathTargets;

    [Header("Arena Walls")]
    [SerializeField] private GameObject arenaWalls;

    private void Start()
    {
        CacheSpawnPositionIfNeeded();
        anim = GetComponent<Animator>();
        hitFlash = GetComponent<HitFlash>();
        hitVfx = GetComponent<BossHitVFX>();
        damageFeedback = GetComponent<BossDamageFeedback>();

        timeBtwDamage = 0f;

        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
        }
    }

    private void Update()
    {
        if (isDead)
            return;

        if (timeBtwDamage > 0f)
            timeBtwDamage -= Time.deltaTime;

        if (healthBar != null)
            healthBar.value = health;

        if (!stageTwoTriggered && health <= 10)
        {
            stageTwoTriggered = true;
            anim.SetTrigger("stageTwo");
        }

        if (health <= 0)
            Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || !other.CompareTag("Player") || timeBtwDamage > 0f)
            return;

        Health hp = other.GetComponent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            timeBtwDamage = damageCooldown;
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamageAt(amount, transform.position);
    }

    public void TakeDamageAt(int amount, Vector3 hitPos)
    {
        if (isDead)
            return;

        health = Mathf.Max(0, health - amount);

        if (healthBar != null)
            healthBar.value = health;

        if (damageFeedback != null)
        {
            damageFeedback.PlayAt(hitPos);
        }
        else
        {
            hitFlash?.Play();
            hitVfx?.PlayAt(hitPos);
        }

        if (!stageTwoTriggered && health <= 10)
        {
            stageTwoTriggered = true;
            anim.SetTrigger("stageTwo");
        }

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        anim.SetTrigger("death");

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        if (arenaWalls != null)
            arenaWalls.SetActive(false);

        if (doorOnDeath != null)
            doorOnDeath.Activate(gameObject);

        InvokeActivationTargets(additionalDeathTargets);

        if (LevelFlow.Instance != null)
        {
            LevelFlow.Instance.ActivateEndPortal();
        }
        else
        {
            Debug.LogError("[Boss] LevelFlow.Instance is NULL! LevelFlow object missing in scene?");
        }

        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null)
            cam.Unlock();

        Defeated?.Invoke();
    }

    public void ResetBoss()
    {
        CacheSpawnPositionIfNeeded();

        isDead = false;
        stageTwoTriggered = false;
        transform.position = spawnPosition;
        health = 20;
        timeBtwDamage = 0f;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
            healthBar.gameObject.SetActive(false);
        }
    }

    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        spawnPositionInitialized = true;
    }

    public void SetEncounterSpawnPosition(Vector3 position)
    {
        SetSpawnPosition(position);
    }

    public void ActivateEncounter()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateEncounter()
    {
        gameObject.SetActive(false);
    }

    public void ResetEncounter()
    {
        ResetBoss();
    }

    private void CacheSpawnPositionIfNeeded()
    {
        if (spawnPositionInitialized)
            return;

        spawnPosition = transform.position;
        spawnPositionInitialized = true;
    }

    private void InvokeActivationTargets(ActivationTarget[] targets)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
            targets[i]?.Invoke(gameObject);
    }
}
