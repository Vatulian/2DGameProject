using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private AudioClip checkpoint;
    private Transform currentCheckpoint;
    private Health playerHealth;
    private UIManager uiManager;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
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
            playerHealth.Respawn();
            transform.position = currentCheckpoint.position;

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
