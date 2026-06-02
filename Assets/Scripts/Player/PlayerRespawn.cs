using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerRespawn : MonoBehaviour
{
    [FormerlySerializedAs("checkpoint")]
    [SerializeField] private AudioClip checkpointSound;

    private Transform currentCheckpoint;
    private Health playerHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private UIManager uiManager;
    private PlayerMovement movement;
    private LedgeClimb ledgeClimb;
    private Vector3 initialSpawnPosition;
    private Vector3 hazardRecoveryPosition;
    private bool hasHazardRecoveryPosition;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        uiManager = FindAnyObjectByType<UIManager>();
        movement = GetComponent<PlayerMovement>();
        ledgeClimb = GetComponent<LedgeClimb>();
        initialSpawnPosition = transform.position;
    }

    public void CheckRespawn()
    {
        uiManager.GameOver();
    }

    public void RestartFromCheckpoint()
    {
        if (currentCheckpoint != null)
        {
            MovePlayerTo(currentCheckpoint.position);

            playerHealth.Respawn();

            BossTriggerZone.ResetActiveBossFight();
        }
        else
        {
            DisableActiveEventSystems();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (anim != null)
                anim.enabled = false;

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            gameObject.SetActive(false);

            Debug.LogWarning("No checkpoint available! Restarting from the beginning.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void SetHazardRecoveryPosition(Vector3 position)
    {
        hazardRecoveryPosition = position;
        hasHazardRecoveryPosition = true;
    }

    public void RecoverFromHazard(float damage)
    {
        if (playerHealth == null || playerHealth.IsDead)
            return;

        if (damage > 0f)
        {
            playerHealth.TakeDamage(damage);
            if (playerHealth.IsDead)
                return;
        }

        MovePlayerTo(GetHazardRecoveryPosition());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Checkpoint"))
            return;

        currentCheckpoint = collision.transform;
        SetHazardRecoveryPosition(currentCheckpoint.position);

        if (playerHealth != null)
            playerHealth.SetCheckpointHealth();

        if (checkpointSound != null)
        {
            SoundManager.instance.PlaySound(checkpointSound);
        }
        else
        {
            Debug.LogWarning("Checkpoint sound is missing!");
        }

        Collider2D checkpointCollider = collision.GetComponent<Collider2D>();
        if (checkpointCollider != null)
            checkpointCollider.enabled = false;

        Animator checkpointAnimator = collision.GetComponent<Animator>();
        if (checkpointAnimator != null)
            checkpointAnimator.SetTrigger("Appear");
    }

    private static void DisableActiveEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null && eventSystem.isActiveAndEnabled)
                eventSystem.gameObject.SetActive(false);
        }
    }

    private Vector3 GetDefaultRecoveryPosition()
    {
        return currentCheckpoint != null ? currentCheckpoint.position : initialSpawnPosition;
    }

    private Vector3 GetHazardRecoveryPosition()
    {
        return hasHazardRecoveryPosition ? hazardRecoveryPosition : GetDefaultRecoveryPosition();
    }

    private void MovePlayerTo(Vector3 position)
    {
        ledgeClimb?.CancelClimb(false);
        movement?.ClearForcedHorizontalVelocity();
        movement?.ClearAirAttackFloat();

        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }
}
