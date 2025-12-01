using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D doorCollider;

    private void Reset()
    {
        animator     = GetComponent<Animator>();
        doorCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Başlangıçta kapı açık varsayıyoruz
        if (animator != null)
            animator.Play("Opening", 0, 1f); // Opening klibinin son karesi

        if (doorCollider != null)
            doorCollider.enabled = false; // açıkken collider kapalı
    }

    // BossTriggerZone'dan çağıracağımız fonksiyon
    public void CloseDoor()
    {
        if (animator == null) return;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Close");
        // Kapanma animasyonu bitince anim event ile EnableCollider çağıracağız
    }

    public void OpenDoor()
    {
        if (animator == null) return;

        animator.ResetTrigger("Close");
        animator.SetTrigger("Open");
        // Açılma bitince DisableCollider anim event'i
    }

    // --- Animation Event fonksiyonları ---

    // Closing klibinin SON karesine event ekle → EnableCollider
    public void EnableCollider()
    {
        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    // Opening klibinin SON karesine event ekle → DisableCollider
    public void DisableCollider()
    {
        if (doorCollider != null)
            doorCollider.enabled = false;
    }
}