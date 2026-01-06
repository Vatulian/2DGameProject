using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D doorCollider;

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;

    private bool isOpen;

    private void Reset()
    {
        animator     = GetComponent<Animator>();
        doorCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        ApplyStartState();
    }

    private void ApplyStartState()
    {
        isOpen = startOpen;

        if (animator != null)
        {
            if (startOpen)
            {
                // Kapı AÇIK başlasın
                animator.Play("Opening", 0, 1f);
            }
            else
            {
                // Kapı KAPALI başlasın
                animator.Play("Closing", 0, 1f);
            }
        }

        if (doorCollider != null)
            doorCollider.enabled = !startOpen; // kapalıysa collider açık
    }

    public void OpenDoor()
    {
        if (isOpen) return;

        Debug.Log("[DoorController] OpenDoor CALLED");

        isOpen = true;

        if (animator == null) return;

        animator.ResetTrigger("Close");
        animator.SetTrigger("Open");
    }

    public void CloseDoor()
    {
        if (!isOpen) return;

        Debug.Log("[DoorController] CloseDoor CALLED");

        isOpen = false;

        if (animator == null) return;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Close");
    }

    // --- Animation Events ---

    // Closing animasyonunun SON karesine koy
    public void EnableCollider()
    {
        Debug.Log("[DoorController] EnableCollider");
        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    // Opening animasyonunun SON karesine koy
    public void DisableCollider()
    {
        Debug.Log("[DoorController] DisableCollider");
        if (doorCollider != null)
            doorCollider.enabled = false;
    }
}
