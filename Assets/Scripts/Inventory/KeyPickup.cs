using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private KeyItem keyItem;
    [SerializeField] private int amount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogError("[KeyPickup] PlayerInventory NOT found on Player!");
            return;
        }

        if (keyItem == null)
        {
            Debug.LogError("[KeyPickup] KeyItem is NULL! Assign it in Inspector.");
            return;
        }

        Debug.Log($"[KeyPickup] Player picked up key → {keyItem.name} (+{amount})");
        inv.AddKey(keyItem, amount);

        Destroy(gameObject);
    }
}