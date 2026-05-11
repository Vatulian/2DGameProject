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

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        uiManager = FindAnyObjectByType<UIManager>();
    }

    public void CheckRespawn()
    {
        uiManager.GameOver();
    }

    public void RestartFromCheckpoint()
    {
        if (currentCheckpoint != null)
        {
            transform.position = currentCheckpoint.position;
            if (rb != null)
            {
                rb.position = currentCheckpoint.position;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            playerHealth.Respawn();

            // Reset boss fight state if the boss is still alive.
            Boss boss = FindObjectOfType<Boss>(true);
            if (boss != null && !boss.isDead)
            {
                BossTriggerZone zone = FindObjectOfType<BossTriggerZone>(true);
                if (zone != null)
                    zone.ResetBossFight();
            }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Checkpoint"))
            return;

        currentCheckpoint = collision.transform;

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
}
