using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    [SerializeField] private float healthValue;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Tutorial")]
    [SerializeField] private string fullHealthMessage = "Health is already full.";
    [SerializeField] private float fullHealthMessageDuration = 1.5f;

    [Header("SFX")]
    [SerializeField] private AudioClip healthcollectibleSound;

    private Health playerHealth;

    private void Update()
    {
        if (playerHealth == null)
            return;

        if (Input.GetKeyDown(interactKey))
            TryCollect();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerHealth = collision.GetComponent<Health>();
        if (playerHealth == null)
            return;

        InteractionPromptUI.Show(this);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (collision.GetComponent<Health>() != playerHealth)
            return;

        playerHealth = null;
        InteractionPromptUI.Hide(this);
    }

    private void OnDisable()
    {
        playerHealth = null;
        InteractionPromptUI.Hide(this);
    }

    private void TryCollect()
    {
        if (playerHealth.CurrentHealth >= playerHealth.StartingHealth)
        {
            TutorialUIManager.Instance?.ShowTemporary(fullHealthMessage, fullHealthMessageDuration);
            return;
        }

        playerHealth.AddHealth(healthValue);
        gameObject.SetActive(false);

        if (healthcollectibleSound != null)
            SoundManager.instance.PlaySound(healthcollectibleSound);
    }
}
