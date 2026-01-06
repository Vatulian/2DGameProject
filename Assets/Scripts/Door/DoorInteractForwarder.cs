using UnityEngine;

public class DoorInteractForwarder : MonoBehaviour
{
    [SerializeField] private DoorKeyInteract doorInteract;

    private void Reset()
    {
        doorInteract = GetComponentInParent<DoorKeyInteract>();
    }

    private void Awake()
    {
        if (doorInteract == null)
        {
            Debug.LogError("[DoorForwarder] DoorKeyInteract NOT found in parent!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[DoorForwarder] Trigger ENTER by Player");
        if (doorInteract != null) doorInteract.SetPlayerInside(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[DoorForwarder] Trigger EXIT by Player");
        if (doorInteract != null) doorInteract.SetPlayerInside(other, false);
    }
}