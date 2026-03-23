using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    private Vector3 spawnPosition;

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

    public bool isDead;
    private bool stageTwoTriggered;

    [Header("Door On Death")]
    [SerializeField] private DoorController doorOnDeath;
    
    [Header("Arena Walls")]
    [SerializeField] private GameObject arenaWalls;

    private void Start()
    {
        spawnPosition = transform.position; // boss'un ilk spawn noktası
        anim = GetComponent<Animator>();
        hitFlash = GetComponent<HitFlash>();
        hitVfx = GetComponent<BossHitVFX>();

        timeBtwDamage = 0f;

        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
        }
    }

    private void Update()
    {
        if (isDead) return;

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
        if (isDead) return;
        if (!other.CompareTag("Player")) return;
        if (timeBtwDamage > 0f) return;

        var hp = other.GetComponent<Health>();
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
        if (isDead) return;

        health = Mathf.Max(0, health - amount);

        if (healthBar != null)
            healthBar.value = health;

        hitFlash?.Play();
        hitVfx?.PlayAt(hitPos);

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
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("death");

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);
        
        if (arenaWalls != null)
            arenaWalls.SetActive(false);

        // OPEN DOOR AFTER BOSS DEATH
        if (doorOnDeath != null)
            doorOnDeath.OpenDoor();
        
        // ACTIVATE END PORTAL AFTER BOSS DEATH
        if (LevelFlow.Instance != null)
        {
            LevelFlow.Instance.ActivateEndPortal();
        }
        else
        {
            Debug.LogError("[Boss] LevelFlow.Instance is NULL! LevelFlow object missing in scene?");
        }

        // CAMERA UNLOCK + ZOOM RESET
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null)
        {
            cam.Unlock();
        }
    }
    public void ResetBoss()
    {
        isDead = false;
        stageTwoTriggered = false;

        // Pozisyon reset
        transform.position = spawnPosition;

        // HP reset
        health = 20; // max HP
        timeBtwDamage = 0f;

        // Animator reset
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // Health bar reset
        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
            healthBar.gameObject.SetActive(false);
        }
    }


}
