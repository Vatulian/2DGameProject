using UnityEngine;

public class BossTriggerZone : MonoBehaviour
{
    [Header("Boss Setup")]
    [SerializeField] private GameObject bossObject; // boss'un kendisi (başta devre dışı)
    [SerializeField] private GameObject bossHealthUI; // boss'un can barı (başta gizli)
    [SerializeField] private AudioClip bossIntroMusic; // opsiyonel: boss müziği

    [Header("Trigger Settings")]
    [SerializeField] private bool oneTimeTrigger = true; // tekrar tetiklenmesin
    [SerializeField] private float delayBeforeSpawn = 0.5f; // intro için kısa gecikme

    private bool triggered;

    private void Start()
    {
        if (bossObject != null) bossObject.SetActive(false);
        if (bossHealthUI != null) bossHealthUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        Debug.Log("[BossTriggerZone] Player entered, starting boss fight...");

        StartCoroutine(ActivateBossSequence());
    }

    private System.Collections.IEnumerator ActivateBossSequence()
    {
        // Belki kısa bir fade-in, kamera zoom, ya da ses efekti eklenebilir
        yield return new WaitForSeconds(delayBeforeSpawn);

        if (bossObject != null)
        {
            bossObject.SetActive(true);
            Debug.Log("[BossTriggerZone] Boss activated.");
        }

        if (bossHealthUI != null)
        {
            bossHealthUI.SetActive(true);
            Debug.Log("[BossTriggerZone] Boss health bar shown.");
        }

        if (bossIntroMusic != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlayMusic(bossIntroMusic, loop: true);
        }

        if (!oneTimeTrigger)
            triggered = false; // tekrar tetiklenebilsin
    }
}