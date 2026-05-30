using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColossalBossAI : MonoBehaviour, IBossEncounterTarget
{
    private enum State
    {
        Inactive,
        Waking,
        Idle,
        Moving,
        Preparing,
        Attacking,
        Buffing,
        Dead
    }

    [Header("Refs")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform root;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Health health;
    [SerializeField] private ColossalBossAttackHitbox groundSlamHitbox;
    [SerializeField] private ColossalBossAttackHitbox rangeHitbox;
    [SerializeField] private ColossalBossAttackHitbox burstHitbox;

    [Header("Hierarchy")]
    [SerializeField] private Transform actorRoot;

    [Header("Encounter")]
    [SerializeField] private bool startActive;
    [SerializeField] private float wakeDuration = 1.1f;
    [SerializeField] private float maxTargetDistance = 120f;
    [SerializeField] private float attackEventTimeout = 2.6f;

    [Header("Flying Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float stopDistance = 3.4f;
    [SerializeField] private float heightOffsetFromSpawn = 1f;

    [Header("Ground Slam Attack")]
    [SerializeField] private float groundSlamPrepTime = 0.35f;
    [SerializeField] private float groundSlamRecoveryTime = 0.45f;

    [Header("Ground Slam Shockwave")]
    [SerializeField] private ColossalBossShockwaveSettings shockwave = new ColossalBossShockwaveSettings();

    [Header("Range Attack")]
    [SerializeField] private float rangePrepTime = 0.45f;
    [SerializeField] private float rangeRecoveryTime = 0.55f;

    [Header("Phase Two")]
    [SerializeField] private bool enablePhaseTwo = true;
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealthRatio = 0.5f;
    [Tooltip("Fallback duration if the Buff animation does not fire EndBuff.")]
    [SerializeField] private float phaseTwoBuffDuration = 1.4f;

    [Header("Animation States")]
    [SerializeField] private string wakeStateName = "Wake";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveStateName = "Move";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string rangeAttackStateName = "Range Attack";
    [SerializeField] private string burstAttackStateName = "Burst";
    [SerializeField] private string phaseTwoBuffStateName = "Buff";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField, Range(0f, 0.12f)] private float animationTransitionTime = 0.03f;

    [Header("Debug")]
    [Tooltip("Draws only simple movement ranges when this object is selected.")]
    [SerializeField] private bool showDebugGizmos;

    private Coroutine behaviorRoutine;
    private Vector3 encounterSpawnPosition;
    private bool encounterSpawnPositionInitialized;
    private bool active;
    private int facing = 1;
    private State state = State.Inactive;
    private string currentAnim = "";
    private bool attackEnded;
    private bool nextAttackIsRange;
    private bool phaseTwoActive;
    private bool phaseTwoBuffing;
    private bool phaseTwoBuffEnded;
    private Coroutine phaseTwoBuffRoutine;
    private Transform fallbackPlayer;
    private ColossalBossAttackHitbox activeAttackHitbox;

    public bool IsEncounterDefeated => state == State.Dead || (health != null && health.IsDead);

    private void Awake()
    {
        ResolveReferences();
        ConfigureRigidbody();
        CacheEncounterSpawnPositionIfNeeded();
        ApplyFacing();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }

        if (startActive)
            ActivateEncounter();
    }

    private void OnDisable()
    {
        StopPhaseTwoBuffRoutine();

        if (health != null)
        {
            health.OnDeath -= HandleDeath;
            health.OnDamaged -= HandleDamaged;
            health.SetInvulnerable(false);
        }

        StopBehaviorRoutine();
        active = false;
    }

    private void ResolveReferences()
    {
        if (root == null)
            root = transform.name == "Root" ? transform : transform.Find("Root") ?? FindDeepChild(transform, "Root");

        if (actorRoot == null)
            actorRoot = transform.parent != null ? transform.parent : transform;

        if (visual == null)
            visual = (root != null ? root.Find("Visual") : null) ?? transform.Find("Visual") ?? FindDeepChild(transform, "Visual");

        if (anim == null)
            anim = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>(true);

        if (rb == null)
            rb = root != null ? root.GetComponent<Rigidbody2D>() : GetComponent<Rigidbody2D>();

        if (bodyCollider == null)
            bodyCollider = root != null ? root.GetComponent<Collider2D>() : GetComponentInChildren<Collider2D>(true);

        if (health == null)
            health = GetComponent<Health>() ?? GetComponentInChildren<Health>(true);

        if (groundSlamHitbox == null)
            groundSlamHitbox = FindHitbox("GroundSlamHitbox");

        if (rangeHitbox == null)
            rangeHitbox = FindHitbox("RangeAttackHitbox");

        if (burstHitbox == null)
            burstHitbox = FindHitbox("BurstHitbox");
    }

    private ColossalBossAttackHitbox FindHitbox(string hitboxName)
    {
        Transform hitboxTransform = root != null ? root.Find(hitboxName) : transform.Find(hitboxName);
        if (hitboxTransform == null)
            hitboxTransform = FindDeepChild(transform, hitboxName);

        return hitboxTransform != null ? hitboxTransform.GetComponent<ColossalBossAttackHitbox>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void SetEncounterSpawnPosition(Vector3 position)
    {
        encounterSpawnPosition = position;
        encounterSpawnPositionInitialized = true;
    }

    public void ActivateEncounter()
    {
        if (IsEncounterDefeated)
            ResetEncounter();

        if (active)
            return;

        active = true;
        gameObject.SetActive(true);

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        StopBehaviorRoutine();
        behaviorRoutine = StartCoroutine(BehaviorLoop());
    }

    public void DeactivateEncounter()
    {
        active = false;
        StopBehaviorRoutine();
        StopPhaseTwoBuffRoutine();
        health?.SetInvulnerable(false);
        DisableAllHitboxes();
        StopMovement();
        Play(idleStateName);
    }

    public void ResetEncounter()
    {
        CacheEncounterSpawnPositionIfNeeded();
        StopBehaviorRoutine();

        active = false;
        state = State.Inactive;
        currentAnim = "";
        phaseTwoActive = false;
        phaseTwoBuffing = false;
        phaseTwoBuffEnded = false;
        nextAttackIsRange = false;
        StopPhaseTwoBuffRoutine();
        SetActorPosition(encounterSpawnPosition);
        DisableAllHitboxes();

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        health?.SetInvulnerable(false);
        health?.ResetToStartingHealth();

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        ApplyFacing();
        Play(idleStateName);
    }

    private IEnumerator BehaviorLoop()
    {
        yield return WakeSequence();

        while (active && state != State.Dead)
        {
            if (phaseTwoBuffing)
            {
                yield return null;
                continue;
            }

            Transform player = GetPlayerTransform();
            if (player == null || IsTargetTooFar(player))
            {
                state = State.Idle;
                Play(idleStateName);
                yield return null;
                continue;
            }

            FaceTarget(player.position);

            if (ShouldMoveBeforeAttacking(player.position))
            {
                MoveToHoverPosition(player.position);
                yield return null;
                continue;
            }

            if (nextAttackIsRange)
                yield return RangeAttackSequence();
            else
                yield return GroundSlamSequence();

            if (phaseTwoBuffing || state == State.Dead)
                continue;

            nextAttackIsRange = !nextAttackIsRange;
        }

        behaviorRoutine = null;
    }

    private IEnumerator WakeSequence()
    {
        state = State.Waking;
        Play(wakeStateName);

        if (wakeDuration > 0f)
            yield return new WaitForSeconds(wakeDuration);
    }

    private IEnumerator GroundSlamSequence()
    {
        state = State.Preparing;
        Play(idleStateName);
        yield return WaitUnlessPhaseTwoBuff(groundSlamPrepTime);

        if (phaseTwoBuffing || state == State.Dead)
            yield break;

        state = State.Attacking;
        Play(attackStateName);
        yield return WaitForAttackEnd(groundSlamHitbox);
        yield return Recover(groundSlamRecoveryTime);
    }

    private IEnumerator RangeAttackSequence()
    {
        state = State.Preparing;
        Play(idleStateName);
        yield return WaitUnlessPhaseTwoBuff(rangePrepTime);

        if (phaseTwoBuffing || state == State.Dead)
            yield break;

        state = State.Attacking;
        bool useBurst = phaseTwoActive && burstHitbox != null;
        ColossalBossAttackHitbox hitbox = useBurst ? burstHitbox : rangeHitbox;
        Play(useBurst ? burstAttackStateName : rangeAttackStateName);
        yield return WaitForAttackEnd(hitbox);
        yield return Recover(rangeRecoveryTime);
    }

    private IEnumerator WaitForAttackEnd(ColossalBossAttackHitbox hitbox)
    {
        activeAttackHitbox = hitbox;
        attackEnded = false;
        activeAttackHitbox?.Begin(root != null ? root : transform, facing);

        float elapsed = 0f;
        while (!attackEnded && !phaseTwoBuffing && state != State.Dead && elapsed < attackEventTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        activeAttackHitbox?.DisableHitbox();
        activeAttackHitbox = null;
    }

    public void EnableHitbox()
    {
        if (state != State.Attacking)
            return;

        activeAttackHitbox?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        activeAttackHitbox?.DisableHitbox();
    }

    public void EndAttack()
    {
        attackEnded = true;
        DisableHitbox();
    }

    public void SpawnShockwaves()
    {
        if (state != State.Attacking)
            return;

        groundSlamHitbox?.MoveToImpactPosition();
        Vector3 origin = groundSlamHitbox != null ? groundSlamHitbox.GetWorldCenter() : GetActorPosition();
        HashSet<Health> sharedHitTargets = new HashSet<Health>();
        SpawnShockwave(origin, Vector2.left, sharedHitTargets);
        SpawnShockwave(origin, Vector2.right, sharedHitTargets);
    }

    public void EndBuff()
    {
        if (!phaseTwoBuffing)
            return;

        phaseTwoBuffEnded = true;
    }

    private IEnumerator Recover(float duration)
    {
        if (state == State.Dead)
            yield break;

        state = State.Idle;
        Play(idleStateName);

        yield return WaitUnlessPhaseTwoBuff(duration);
    }

    private void HandleDamaged(float currentHealth)
    {
        if (!enablePhaseTwo || phaseTwoActive || health == null || health.StartingHealth <= 0f)
            return;

        if (currentHealth > health.StartingHealth * phaseTwoHealthRatio)
            return;

        phaseTwoActive = true;
        StopPhaseTwoBuffRoutine();
        phaseTwoBuffRoutine = StartCoroutine(PhaseTwoBuff());
    }

    private IEnumerator PhaseTwoBuff()
    {
        phaseTwoBuffing = true;
        phaseTwoBuffEnded = false;
        attackEnded = true;
        activeAttackHitbox?.DisableHitbox();
        activeAttackHitbox = null;
        DisableAllHitboxes();
        StopMovement();

        health?.SetInvulnerable(true);
        state = State.Buffing;
        Play(phaseTwoBuffStateName);

        float elapsed = 0f;
        while (active && state != State.Dead && !phaseTwoBuffEnded && elapsed < phaseTwoBuffDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        health?.SetInvulnerable(false);
        phaseTwoBuffing = false;
        phaseTwoBuffEnded = false;
        phaseTwoBuffRoutine = null;

        if (active && state != State.Dead)
        {
            state = State.Idle;
            Play(idleStateName);
        }
    }

    private IEnumerator WaitUnlessPhaseTwoBuff(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (!phaseTwoBuffing && state != State.Dead && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SpawnShockwave(Vector3 origin, Vector2 direction, HashSet<Health> sharedHitTargets)
    {
        GameObject shockwaveObject = new GameObject("ColossalBoss Shockwave");
        ColossalBossShockwave shockwaveRunner = shockwaveObject.AddComponent<ColossalBossShockwave>();
        shockwaveRunner.Initialize(origin, direction, shockwave, GetActorPosition(), sharedHitTargets);
    }

    private void MoveToHoverPosition(Vector3 playerPosition)
    {
        Vector3 current = GetActorPosition();
        Vector3 target = new Vector3(playerPosition.x, GetFixedHeight(), current.z);

        if (GetHorizontalDistance(playerPosition) <= stopDistance)
        {
            state = State.Idle;
            Play(idleStateName);
            return;
        }

        state = State.Moving;
        Play(moveStateName);

        current.x = Mathf.MoveTowards(current.x, target.x, moveSpeed * Time.deltaTime);
        current.y = target.y;

        SetActorPosition(current);
    }

    private bool ShouldMoveBeforeAttacking(Vector3 playerPosition)
    {
        return GetHorizontalDistance(playerPosition) > stopDistance;
    }

    private float GetFixedHeight()
    {
        return encounterSpawnPosition.y + heightOffsetFromSpawn;
    }

    private void HandleDeath()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;
        active = false;
        StopBehaviorRoutine();
        StopPhaseTwoBuffRoutine();
        health?.SetInvulnerable(false);
        DisableAllHitboxes();
        StopMovement();

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        Play(deathStateName);
    }

    private Transform GetPlayerTransform()
    {
        if (PlayerReference.IsAvailable)
            return PlayerReference.Player;

        if (fallbackPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                fallbackPlayer = playerObject.transform;
            }
            else
            {
                PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
                fallbackPlayer = playerMovement != null ? playerMovement.transform : null;
            }
        }

        return fallbackPlayer;
    }

    private bool IsTargetTooFar(Transform target)
    {
        return target == null || Vector2.Distance(GetActorPosition(), target.position) > maxTargetDistance;
    }

    private float GetHorizontalDistance(Vector3 targetPosition)
    {
        return Mathf.Abs(targetPosition.x - GetActorPosition().x);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        float delta = targetPosition.x - GetActorPosition().x;
        if (Mathf.Approximately(delta, 0f))
            return;

        int nextFacing = delta > 0f ? 1 : -1;
        if (nextFacing == facing)
            return;

        facing = nextFacing;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (visual == null)
        {
            SetHitboxFacing();
            return;
        }

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        visual.localScale = scale;
        SetHitboxFacing();
    }

    private void SetHitboxFacing()
    {
        groundSlamHitbox?.SetFacing(facing);
        rangeHitbox?.SetFacing(facing);
        burstHitbox?.SetFacing(facing);
    }

    private Vector2 GetOrigin()
    {
        return GetActorPosition();
    }

    private void DisableAllHitboxes()
    {
        groundSlamHitbox?.DisableHitbox();
        rangeHitbox?.DisableHitbox();
        burstHitbox?.DisableHitbox();
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
            return;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private Vector3 GetActorPosition()
    {
        return actorRoot != null ? actorRoot.position : transform.position;
    }

    private void SetActorPosition(Vector3 position)
    {
        if (actorRoot != null)
            actorRoot.position = position;
        else
            transform.position = position;
    }

    private void StopMovement()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void StopBehaviorRoutine()
    {
        if (behaviorRoutine == null)
            return;

        StopCoroutine(behaviorRoutine);
        behaviorRoutine = null;
    }

    private void StopPhaseTwoBuffRoutine()
    {
        if (phaseTwoBuffRoutine == null)
            return;

        StopCoroutine(phaseTwoBuffRoutine);
        phaseTwoBuffRoutine = null;
        phaseTwoBuffing = false;
        phaseTwoBuffEnded = false;
    }

    private void Play(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || anim == null || anim.runtimeAnimatorController == null)
            return;

        if (currentAnim == stateName)
            return;

        currentAnim = stateName;
        anim.CrossFadeInFixedTime(stateName, animationTransitionTime);
    }

    private void CacheEncounterSpawnPositionIfNeeded()
    {
        if (encounterSpawnPositionInitialized)
            return;

        encounterSpawnPosition = GetActorPosition();
        encounterSpawnPositionInitialized = true;
    }

    private void OnValidate()
    {
        wakeDuration = Mathf.Max(0f, wakeDuration);
        attackEventTimeout = Mathf.Max(0.1f, attackEventTimeout);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stopDistance = Mathf.Max(0.1f, stopDistance);
        phaseTwoBuffDuration = Mathf.Max(0f, phaseTwoBuffDuration);
        shockwave?.Validate();
        maxTargetDistance = Mathf.Max(stopDistance, maxTargetDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (shockwave != null && shockwave.drawDebug)
            DrawShockwaveDebugGizmos();

        if (showDebugGizmos)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetOrigin(), stopDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetOrigin(), maxTargetDistance);

            Transform player = GetPlayerTransform();
            if (player != null)
            {
                Vector3 hoverTarget = new Vector3(player.position.x, GetFixedHeight(), GetActorPosition().z);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(GetOrigin(), hoverTarget);
                Gizmos.DrawWireSphere(hoverTarget, 0.25f);
            }
        }
    }

    private void DrawShockwaveDebugGizmos()
    {
        if (shockwave == null)
            return;

        Vector3 origin = groundSlamHitbox != null ? groundSlamHitbox.GetWorldCenter() : GetOrigin();
        DrawShockwaveDirectionGizmos(origin, Vector2.left);
        DrawShockwaveDirectionGizmos(origin, Vector2.right);
    }

    private void DrawShockwaveDirectionGizmos(Vector3 origin, Vector2 direction)
    {
        float distance = Mathf.Max(0f, shockwave.maxDistance);
        Vector3 normalizedDirection = direction.normalized;
        Vector3 endOrigin = origin + normalizedDirection * distance;

        Gizmos.color = shockwave.pathGizmoColor;
        Gizmos.DrawLine(origin, endOrigin);
        Gizmos.DrawWireSphere(origin, 0.08f);
        Gizmos.DrawWireSphere(endOrigin, 0.08f);

        DrawShockwavePiecePreviews(origin, normalizedDirection, distance);
    }

    private void DrawShockwavePiecePreviews(Vector3 origin, Vector3 direction, float distance)
    {
        float spacing = Mathf.Max(0.05f, shockwave.pieceSpawnSpacing);

        for (float travelled = 0f; travelled <= distance; travelled += spacing)
        {
            Vector3 pieceOrigin = origin + direction * travelled + (Vector3)shockwave.pieceSpawnOffset;
            DrawShockwavePiecePreview(pieceOrigin);
        }
    }

    private void DrawShockwavePiecePreview(Vector3 pieceOrigin)
    {
        if (shockwave.shockwavePiecePrefab == null)
        {
            Gizmos.color = shockwave.pieceColliderGizmoColor;
            Gizmos.DrawWireSphere(pieceOrigin, 0.2f);
            return;
        }

        DrawPrefabColliderPreview(pieceOrigin);
    }

    private void DrawPrefabColliderPreview(Vector3 pieceOrigin)
    {
        BoxCollider2D box = shockwave.shockwavePiecePrefab.GetComponentInChildren<BoxCollider2D>(true);
        if (box == null)
            return;

        Vector3 localCenter = box.transform.localPosition + (Vector3)box.offset;
        Vector3 size = new Vector3(
            box.size.x * Mathf.Abs(box.transform.localScale.x),
            box.size.y * Mathf.Abs(box.transform.localScale.y),
            0.01f);

        Gizmos.color = shockwave.pieceColliderGizmoColor;
        Gizmos.DrawWireCube(pieceOrigin + localCenter, size);
    }
}
