using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private KeyItem keyItem;
    [SerializeField] private int amount = 1;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private PlayerInventory playerInventory;

    private void Update()
    {
        if (playerInventory == null)
            return;

        if (Input.GetKeyDown(interactKey))
            TryCollect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("[KeyPickup] PlayerInventory NOT found on Player!");
            return;
        }

        TutorialUIManager.ShowInteractionPrompt(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.GetComponent<PlayerInventory>() != playerInventory)
            return;

        playerInventory = null;
        TutorialUIManager.HideInteractionPrompt(this);
    }

    private void OnDisable()
    {
        playerInventory = null;
        TutorialUIManager.HideInteractionPrompt(this);
    }

    private void TryCollect()
    {
        if (keyItem == null)
        {
            Debug.LogError("[KeyPickup] KeyItem is NULL! Assign it in Inspector.");
            return;
        }

        Debug.Log($"[KeyPickup] Player picked up key -> {keyItem.name} (+{amount})");
        playerInventory.AddKey(keyItem, amount);

        Destroy(gameObject);
    }
}
