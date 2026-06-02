using UnityEngine;

public class LevelEndPortal : MonoBehaviour
{
    [SerializeField] private bool activeOnStart = true;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool isActive;
    private bool playerInside;

    private void Awake()
    {
        SetActive(activeOnStart);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        playerInside = false;
        gameObject.SetActive(active);
    }

    private void Update()
    {
        if (!isActive) return;
        if (!playerInside) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (LevelFlow.Instance == null)
                return;

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

    private void OnDisable()
    {
        playerInside = false;
    }
}
