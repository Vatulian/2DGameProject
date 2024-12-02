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
            playerHealth.Respawn(); // Restore player health
            transform.position = currentCheckpoint.position; // Move to checkpoint location
        }
        else
        {
            Debug.LogWarning("No checkpoint available! Restarting from the beginning.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Restart level
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Checkpoint")
        {
            currentCheckpoint = collision.transform;

            if (checkpoint != null) // Ses dosyasý atanmýþ mý?
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
