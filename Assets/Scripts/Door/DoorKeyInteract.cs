using UnityEngine;

public class DoorKeyInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorController door;

    [Header("Key Requirement")]
    [SerializeField] private KeyItem requiredKey;
    [SerializeField] private int requiredCount = 1;
    [SerializeField] private bool consumeOnOpen = true;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private PlayerInventory playerInsideInv;
    private bool opened;

    private void Reset()
    {
        door = GetComponent<DoorController>();
    }

    private void Awake()
    {
        if (door == null)
            Debug.LogError("[Door] DoorController reference is NULL! (Same GameObject olmalı ya da Inspector'dan bağla)");

        if (requiredKey == null)
            Debug.LogWarning("[Door] RequiredKey is NULL! Kapı hiç açılmaz. Inspector'dan KeyItem bağla.");
    }

    private void Update()
    {
        if (opened) return;
        if (playerInsideInv == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("[Door] E pressed");

            if (requiredKey == null)
            {
                Debug.LogError("[Door] OPEN FAILED – requiredKey is NULL!");
                return;
            }

            bool ok = consumeOnOpen
                ? playerInsideInv.TryConsumeKey(requiredKey, requiredCount)
                : playerInsideInv.HasKey(requiredKey, requiredCount);

            Debug.Log($"[Door] Key check result = {ok} (requiredKey={requiredKey.name}, requiredCount={requiredCount})");

            if (!ok)
            {
                Debug.Log("[Door] OPEN FAILED – key missing");
                return;
            }

            opened = true;
            Debug.Log("[Door] OPENING DOOR");
            door.OpenDoor();
        }
    }

    // Forwarder child trigger bunu çağırır
    public void SetPlayerInside(Collider2D other, bool inside)
    {
        if (!other.CompareTag("Player")) return;

        if (inside)
        {
            playerInsideInv = other.GetComponent<PlayerInventory>();
            if (playerInsideInv == null)
                Debug.LogError("[Door] PlayerInventory NOT found on Player!");
            else
                Debug.Log("[Door] Player ENTERED interact zone");
        }
        else
        {
            playerInsideInv = null;
            Debug.Log("[Door] Player EXITED interact zone");
        }
    }
}
