using System.Collections;
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
    [SerializeField] private float cameraOrthographicSize = 7f;

    [Header("Door On Enter")]
    [SerializeField] private DoorController doorToClose;

    [Header("Activation Targets")]
    [SerializeField] private ActivationTarget[] additionalStartTargets;
    [SerializeField] private ActivationTarget[] additionalResetTargets;
    [SerializeField] private ActivationTarget[] additionalCompleteTargets;

    [Header("Trigger Settings")]
    [SerializeField] private bool oneTimeTrigger = true;
    [SerializeField] private float delayBeforeSpawn = 0.5f;

    private IBossEncounterTarget bossTarget;
    private Health bossHealth;
    private Boss legacyBoss;
    private Vector3 bossSpawnPosition;
    private bool hasBossSpawnPosition;
    private bool triggered;
    private bool fightRunning;
    private bool encounterCompleted;

    public static BossTriggerZone ActiveEncounter { get; private set; }
    public bool IsFightRunning => fightRunning;
    public bool IsCompleted => encounterCompleted;

    private void Awake()
    {
        CacheBossReferences();
    }

    private void OnValidate()
    {
        cameraOrthographicSize = Mathf.Max(0.1f, cameraOrthographicSize);
    }

    private void Start()
    {
        PrepareAssignedBoss();

        if (bossHealthUI != null)
            bossHealthUI.SetActive(false);

        if (arenaWalls != null)
            arenaWalls.DeactivateWalls();
    }

    private void Update()
    {
        if (!fightRunning || !IsAssignedBossDefeated())
            return;

        CompleteBossFight();
    }

    private void OnDisable()
    {
        if (ActiveEncounter == this)
            ActiveEncounter = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || (oneTimeTrigger && encounterCompleted) || !other.CompareTag("Player"))
            return;

        triggered = true;
        fightRunning = true;
        encounterCompleted = false;
        ActiveEncounter = this;
        StartCoroutine(ActivateBossSequence());
    }

    private IEnumerator ActivateBossSequence()
    {
        yield return new WaitForSeconds(delayBeforeSpawn);

        if (arenaWalls != null)
            arenaWalls.ActivateWalls();

        if (cameraController != null && cameraLockPoint != null)
            cameraController.LockToPosition(cameraLockPoint.position, cameraOrthographicSize);

        if (doorToClose != null)
            doorToClose.Deactivate(gameObject);

        InvokeActivationTargets(additionalStartTargets);

        ActivateAssignedBoss();

        if (bossHealthUI != null)
            bossHealthUI.SetActive(true);

        if (bossIntroMusic != null && SoundManager.instance != null)
            SoundManager.instance.PlayMusic(bossIntroMusic, true);
    }

    public static bool ResetActiveBossFight()
    {
        if (ActiveEncounter == null || !ActiveEncounter.fightRunning)
            return false;

        ActiveEncounter.ResetBossFight();
        return true;
    }

    public void ResetBossFight()
    {
        StopAllCoroutines();

        fightRunning = false;
        encounterCompleted = false;
        triggered = false;

        if (ActiveEncounter == this)
            ActiveEncounter = null;

        ResetAssignedBoss();
        DeactivateAssignedBoss();

        if (bossHealthUI != null)
            bossHealthUI.SetActive(false);

        if (arenaWalls != null)
            arenaWalls.DeactivateWalls();

        if (doorToClose != null)
            doorToClose.Activate(gameObject);

        InvokeActivationTargets(additionalResetTargets);

        if (cameraController != null)
            cameraController.Unlock();
    }

    private void CompleteBossFight()
    {
        fightRunning = false;
        encounterCompleted = true;

        if (!oneTimeTrigger)
            triggered = false;

        if (ActiveEncounter == this)
            ActiveEncounter = null;

        if (bossHealthUI != null)
            bossHealthUI.SetActive(false);

        if (arenaWalls != null)
            arenaWalls.DeactivateWalls();

        if (doorToClose != null)
            doorToClose.Activate(gameObject);

        InvokeActivationTargets(additionalCompleteTargets);

        if (cameraController != null)
            cameraController.Unlock();
    }

    private void PrepareAssignedBoss()
    {
        CacheBossReferences();

        if (bossObject == null)
            return;

        if (!hasBossSpawnPosition)
        {
            bossSpawnPosition = bossObject.transform.position;
            hasBossSpawnPosition = true;
        }

        bossTarget?.SetEncounterSpawnPosition(bossSpawnPosition);
        legacyBoss?.SetSpawnPosition(bossSpawnPosition);
        DeactivateAssignedBoss();
    }

    private void ActivateAssignedBoss()
    {
        if (bossObject == null)
            return;

        bossObject.SetActive(true);
        CacheBossReferences();
        bossTarget?.ActivateEncounter();
    }

    private void DeactivateAssignedBoss()
    {
        bossTarget?.DeactivateEncounter();

        if (bossObject != null)
            bossObject.SetActive(false);
    }

    private void ResetAssignedBoss()
    {
        if (bossObject == null)
            return;

        bossObject.transform.position = bossSpawnPosition;
        CacheBossReferences();

        if (bossTarget != null)
        {
            bossTarget.ResetEncounter();
            return;
        }

        if (legacyBoss != null)
        {
            legacyBoss.ResetBoss();
            return;
        }

        bossHealth?.ResetToStartingHealth();
    }

    private bool IsAssignedBossDefeated()
    {
        CacheBossReferences();

        if (bossTarget != null)
            return bossTarget.IsEncounterDefeated;

        if (legacyBoss != null)
            return legacyBoss.isDead;

        return bossHealth != null && bossHealth.IsDead;
    }

    private void CacheBossReferences()
    {
        if (bossObject == null)
            return;

        if (!hasBossSpawnPosition)
        {
            bossSpawnPosition = bossObject.transform.position;
            hasBossSpawnPosition = true;
        }

        legacyBoss = bossObject.GetComponent<Boss>() ?? bossObject.GetComponentInChildren<Boss>(true);
        bossHealth = bossObject.GetComponent<Health>() ?? bossObject.GetComponentInChildren<Health>(true);
        bossTarget = FindEncounterTarget();
    }

    private IBossEncounterTarget FindEncounterTarget()
    {
        if (bossObject == null)
            return null;

        MonoBehaviour[] behaviours = bossObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IBossEncounterTarget target)
                return target;
        }

        return null;
    }

    private void InvokeActivationTargets(ActivationTarget[] targets)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
            targets[i]?.Invoke(gameObject);
    }
}
