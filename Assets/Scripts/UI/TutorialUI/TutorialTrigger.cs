using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private TutorialUIManager.HintRequest hint;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TutorialUIManager.Instance?.ShowPersistent(hint);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TutorialUIManager.Instance?.HidePersistent();
    }
}