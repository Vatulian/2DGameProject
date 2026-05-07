using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private AudioClip checkpoint;
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
        // Show Game Over screen
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

            // Boss ölmediyse boss fight state'ini resetle (kamera/duvar/kapı/boss/ui)
            Boss boss = FindObjectOfType<Boss>(true);
            if (boss != null && !boss.isDead)
            {
                BossTriggerZone zone = FindObjectOfType<BossTriggerZone>(true);
                if (zone != null) zone.ResetBossFight();
            }
        }
        else
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            if (anim != null)
                anim.enabled = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            Debug.LogWarning("No checkpoint available! Restarting from the beginning.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Checkpoint")
        {
            currentCheckpoint = collision.transform;

            if (checkpoint != null) // Ses dosyas� atanm�� m�?
            {
                SoundManager.instance.PlaySound(checkpoint);
            }
            else
            {
                Debug.LogWarning("Checkpoint sound is missing!");
            }

            collision.GetComponent<Collider2D>().enabled = false;
            collision.GetComponent<Animator>().SetTrigger("Appear");
        }
    }
}
