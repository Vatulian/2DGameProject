using UnityEngine;

public class LevelEndPortal : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool isActive;
    private bool playerInside;

    private void Awake()
    {
        // Başta kapalı
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active); // görünür + collider aktif
    }

    private void Update()
    {
        if (!isActive) return;
        if (!playerInside) return;

        if (Input.GetKeyDown(interactKey))
        {
            LevelFlow.Instance.CompleteLevel();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;
        playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
    }
}