using UnityEngine;

public class BossTriggerZone : MonoBehaviour
{
    [Header("Boss Setup")]
    [SerializeField] private GameObject bossObject;
    [SerializeField] private GameObject bossHealthUI;
    [SerializeField] private AudioClip bossIntroMusic;

    [Header("Arena Walls")]
    [SerializeField] private ArenaWallsController arenaWalls;

    [Header("Camera Lock")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform cameraLockPoint;

    [Header("Door On Enter")]
    [SerializeField] private DoorController doorToClose;

    [Header("Trigger Settings")]
    [SerializeField] private bool oneTimeTrigger = true;
    [SerializeField] private float delayBeforeSpawn = 0.5f;

    private bool triggered;

    private void Start()
    {
        if (bossObject != null) bossObject.SetActive(false);
        if (bossHealthUI != null) bossHealthUI.SetActive(false);
        if (arenaWalls != null) arenaWalls.DeactivateWalls();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(ActivateBossSequence());
    }

    private System.Collections.IEnumerator ActivateBossSequence()
    {
        yield return new WaitForSeconds(delayBeforeSpawn);

        // Arena duvarları ON
        if (arenaWalls != null) 
            arenaWalls.ActivateWalls();

        // Kamera kilidi
        if (cameraController != null && cameraLockPoint != null)
            cameraController.LockToPosition(cameraLockPoint.position, true); // true = bossSize kullan

        // Kapıyı kapat
        if (doorToClose != null)
            doorToClose.CloseDoor();

        // Boss aktif olsun
        if (bossObject != null)
            bossObject.SetActive(true);

        // UI ON
        if (bossHealthUI != null)
            bossHealthUI.SetActive(true);

        // Müzik
        if (bossIntroMusic != null && SoundManager.instance != null)
            SoundManager.instance.PlayMusic(bossIntroMusic, true);

        if (!oneTimeTrigger)
            triggered = false;
    }
}
