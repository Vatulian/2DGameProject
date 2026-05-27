using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EliteKillerBossAI : MonoBehaviour, IBossEncounterTarget, IParryReceiver
{
    private enum State
    {
        Idle,
        ComboDash,
        ComboAttack,
        ChainLeap,
        ChainAttack,
        Recovery,
        Parried,
        Dead
    }

    private enum AttackPattern
    {
        Combo,
        Chain
    }

    [Serializable]
    private struct ComboHitWindow
    {
        public float startTime;
        public float duration;
        public float lungeDistance;
        public float lungeDuration;
        public bool clearPreviousHits;
    }

    [Header("Refs")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform detectionOrigin;
    [SerializeField] private EliteKillerAttackHitbox comboHitbox;
    [SerializeField] private EliteKillerAttackHitbox chainHitbox;
    [SerializeField] private Health health;
    [SerializeField] private CameraController cameraController;

    [Header("Hierarchy")]
    [SerializeField] private Transform actorRoot;

    [Header("Arena")]
    [SerializeField] private Transform leftArenaPoint;
    [SerializeField] private Transform rightArenaPoint;

    [Header("Death Drop")]
    [SerializeField] private GameObject deathDropPrefab;
    [SerializeField] private Transform deathDropSpawnPoint;
    [SerializeField] private Vector2 deathDropOffset;
    [SerializeField] private bool dropOnlyOnce = true;

    [Header("Pattern")]
    [SerializeField] private bool startActive = true;
    [SerializeField] private bool alternatePatterns = true;
    [SerializeField, Range(0f, 1f)] private float randomChainChance = 0.45f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float recoveryTime = 0.45f;
    [SerializeField] private float parriedStunTime = 0.75f;

    [Header("Animation Event Flow")]
    [SerializeField] private bool useAnimationEvents = true;

    [Header("Free Movement")]
    [SerializeField] private float freeMoveSpeed = 2.4f;
    [SerializeField] private float freeMoveStopDistance = 1.65f;

    [Header("Distances")]
    [SerializeField] private float comboAttackDistance = 1.15f;
    [SerializeField] private float comboDashDistanceThreshold = 1.55f;
    [SerializeField] private float comboDashStopDistance = 1.55f;
    [SerializeField] private float maxTargetDistance = 60f;

    [Header("Combo Dash")]
    [SerializeField] private float comboDashMaxDistance = 15f;
    [SerializeField] private float comboDashDuration = 0.28f;
    [SerializeField] private float comboWindupAfterDash = 0.12f;
    [SerializeField] private AnimationCurve comboDashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Combo Tracking")]
    [SerializeField] private bool updateComboTargetDuringAttack = true;
    [SerializeField] private float comboInnerReachPadding = 0.15f;
    [SerializeField] private float comboForwardCorrectionLimit = 0.95f;
    [SerializeField] private float comboFinalLungeBonus = 0.28f;

    [Header("Combo Attack Events")]
    [SerializeField] private float comboDuration = 1.35f;
    [SerializeField] private bool finishComboOnAnimationEvent = true;
    [SerializeField] private ComboHitWindow[] comboEventHits =
    {
        new ComboHitWindow { lungeDistance = 0.28f, lungeDuration = 0.08f, clearPreviousHits = true },
        new ComboHitWindow { lungeDistance = 0.32f, lungeDuration = 0.08f, clearPreviousHits = true },
        new ComboHitWindow { lungeDistance = 0.38f, lungeDuration = 0.1f, clearPreviousHits = true }
    };

    [Header("Combo Attack Timing Fallback")]
    [SerializeField] private ComboHitWindow[] comboHitWindows =
    {
        new ComboHitWindow { startTime = 0.18f, duration = 0.11f, lungeDistance = 0.28f, lungeDuration = 0.08f, clearPreviousHits = true },
        new ComboHitWindow { startTime = 0.48f, duration = 0.12f, lungeDistance = 0.32f, lungeDuration = 0.08f, clearPreviousHits = true },
        new ComboHitWindow { startTime = 0.78f, duration = 0.14f, lungeDistance = 0.38f, lungeDuration = 0.1f, clearPreviousHits = true }
    };

    [Header("Chain Leap")]
    [SerializeField, Range(0.1f, 1f)] private float chainLeapReachRatio = 0.7f;
    [SerializeField] private float leapDuration = 0.62f;
    [SerializeField] private float leapArcHeight = 3.4f;
    [SerializeField] private float leapLandingOffsetFromPlayer = 1.15f;
    [SerializeField] private float chainWindupAfterLeap = 0.25f;
    [SerializeField] private int leapTouchDamage = 1;
    [SerializeField] private ParryAttackSettings leapTouchParry = new ParryAttackSettings();
    [SerializeField] private AnimationCurve leapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Leap Feedback")]
    [SerializeField] private float leapLandingShakeDuration = 0.12f;
    [SerializeField] private float leapLandingShakeMagnitude = 0.18f;

    [Header("Chain Attack")]
    [SerializeField] private bool finishChainOnAnimationEvent = true;
    [SerializeField] private float chainAnimationFallbackDuration = 1f;
    [SerializeField] private float chainExtendDuration = 0.22f;
    [SerializeField] private float chainHoldDuration = 0.08f;
    [SerializeField] private float chainRetractDuration = 0.22f;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveStateName = "Move";
    [SerializeField] private string dashStateName = "Dash";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string chainAttackStateName = "Chain Attack";
    [SerializeField] private string jumpStateName = "Jump";
    [SerializeField] private string fallStateName = "Fall";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField, Range(0f, 0.12f)] private float animationTransitionTime = 0.03f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos;

    private readonly HashSet<Collider2D> leapTouchTargets = new HashSet<Collider2D>();

    private Rigidbody2D rb;
    private State state = State.Idle;
    private int facing = 1;
    private int lockedAttackFacing = 1;
    private bool nextAlternateIsChain;
    private bool active;
    private bool attackFinishedByAnimation;
    private Coroutine combatRoutine;
    private Coroutine lungeRoutine;
    private string currentAnim = "";
    private Vector3 comboMemoryPosition;
    private bool hasComboMemory;
    private Vector3 encounterSpawnPosition;
    private bool encounterSpawnPositionInitialized;
    private bool deathDropSpawned;

    public int Facing => facing;
    public bool IsActive => active;
    public bool IsEncounterDefeated => state == State.Dead || (health != null && health.IsDead);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        Transform root = transform.Find("Root");

        if (visual == null)
            visual = FindVisual(root);

        if (visual == null)
            visual = transform;

        if (actorRoot == null)
            actorRoot = ResolveActorRoot();

        if (anim == null)
            anim = visual.GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (detectionOrigin == null)
            detectionOrigin = root != null ? root : transform;

        if (comboHitbox == null)
            comboHitbox = GetComponentInChildren<EliteKillerAttackHitbox>(true);

        if (chainHitbox == null)
        {
            EliteKillerAttackHitbox[] hitboxes = GetComponentsInChildren<EliteKillerAttackHitbox>(true);
            chainHitbox = hitboxes.Length > 1 ? hitboxes[1] : comboHitbox;
        }

        if (health == null)
            health = GetComponent<Health>();

        if (cameraController == null && Camera.main != null)
            cameraController = Camera.main.GetComponent<CameraController>();

        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();

        CacheEncounterSpawnPositionIfNeeded();
        ApplyFacing();
    }

    private Transform FindVisual(Transform root)
    {
        if (root != null)
        {
            Transform childVisual = root.Find("Visual");
            if (childVisual != null)
                return childVisual;
        }

        Transform ownVisual = transform.Find("Visual");
        if (ownVisual != null)
            return ownVisual;

        if (transform.parent != null)
        {
            Transform siblingVisual = transform.parent.Find("Visual");
            if (siblingVisual != null)
                return siblingVisual;
        }

        return null;
    }

    private Transform ResolveActorRoot()
    {
        if (visual != null && transform.parent != null && visual.parent == transform.parent)
            return transform.parent;

        return transform;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;

        if (startActive)
            Activate();
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        StopCombatRoutine();
        StopLunge();
        comboHitbox?.DisableHitbox();
        chainHitbox?.DisableHitbox();
    }

    public void Activate()
    {
        if (active || state == State.Dead)
            return;

        active = true;
        StopCombatRoutine();
        combatRoutine = StartCoroutine(CombatLoop());
    }

    public void Deactivate()
    {
        active = false;
        StopCombatRoutine();
        StopLunge();
        Stop();
        Play(idleStateName);
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

        Activate();
    }

    public void DeactivateEncounter()
    {
        Deactivate();
    }

    public void ResetEncounter()
    {
        CacheEncounterSpawnPositionIfNeeded();

        active = false;
        state = State.Idle;
        attackFinishedByAnimation = false;
        hasComboMemory = false;
        currentAnim = "";
        deathDropSpawned = false;

        StopCombatRoutine();
        StopLunge();
        Stop();

        comboHitbox?.DisableHitbox();
        chainHitbox?.DisableHitbox();

        SetActorPosition(encounterSpawnPosition);

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        health?.ResetToStartingHealth();
        ApplyFacing();
        Play(idleStateName);
    }

    private IEnumerator CombatLoop()
    {
        while (active && state != State.Dead)
        {
            Transform player = GetPlayerTransform();
            if (player == null || IsTargetTooFar(player))
            {
                state = State.Idle;
                Stop();
                Play(idleStateName);
                yield return null;
                continue;
            }

            FaceTarget(player.position);
            yield return ApproachPlayerForDuration(attackCooldown);

            player = GetPlayerTransform();
            if (player == null || IsTargetTooFar(player))
                continue;

            AttackPattern pattern = ChoosePattern();
            if (pattern == AttackPattern.Combo)
                yield return ComboPattern(player);
            else
                yield return ChainPattern(player);

            if (state == State.Parried)
            {
                yield return new WaitForSeconds(parriedStunTime);
                state = State.Recovery;
            }

            if (state != State.Dead && state != State.Parried)
            {
                state = State.Recovery;
                Stop();
                Play(idleStateName);
                yield return new WaitForSeconds(recoveryTime);
            }
        }

        combatRoutine = null;
    }

    private AttackPattern ChoosePattern()
    {
        if (alternatePatterns)
        {
            nextAlternateIsChain = !nextAlternateIsChain;
            return nextAlternateIsChain ? AttackPattern.Chain : AttackPattern.Combo;
        }

        return UnityEngine.Random.value <= randomChainChance ? AttackPattern.Chain : AttackPattern.Combo;
    }

    private IEnumerator ApproachPlayerForDuration(float duration)
    {
        if (duration <= 0f)
            yield break;

        state = State.Idle;
        float elapsed = 0f;
        while (elapsed < duration && active && state != State.Dead)
        {
            elapsed += Time.deltaTime;

            Transform player = GetPlayerTransform();
            if (player == null || IsTargetTooFar(player))
            {
                Stop();
                Play(idleStateName);
                yield return null;
                continue;
            }

            FaceTarget(player.position);

            if (ShouldFreeMoveToward(player.position))
            {
                Play(moveStateName);
                MoveHorizontallyTowards(player.position, freeMoveSpeed * Time.deltaTime);
            }
            else
            {
                Stop();
                Play(idleStateName);
            }

            yield return null;
        }

        Stop();
    }

    private bool ShouldFreeMoveToward(Vector3 targetPosition)
    {
        return GetHorizontalDistance(targetPosition) > GetFreeMoveStopDistance();
    }

    private float GetFreeMoveStopDistance()
    {
        return Mathf.Max(0.1f, freeMoveStopDistance);
    }

    private void MoveHorizontallyTowards(Vector3 targetPosition, float maxStep)
    {
        if (maxStep <= 0f)
            return;

        Vector3 currentPosition = GetActorPosition();
        float nextX = Mathf.MoveTowards(currentPosition.x, targetPosition.x, maxStep);
        SetActorPosition(ClampToArena(new Vector3(nextX, currentPosition.y, currentPosition.z)));
    }

    private bool ShouldDashBeforeCombo(Vector3 targetPosition)
    {
        int direction = GetDirectionTo(targetPosition, facing);
        float forwardDistance = GetForwardDistance(targetPosition, direction);
        float minimumDistance = GetComboMinimumStrikeDistance();
        float maximumDistance = GetComboMaximumStrikeDistance();

        return forwardDistance < minimumDistance
            || forwardDistance > maximumDistance
            || forwardDistance > GetComboDashDistanceThreshold();
    }

    private IEnumerator ComboPattern(Transform player)
    {
        hasComboMemory = false;
        RememberComboTarget(player.position);
        FaceTarget(comboMemoryPosition);

        if (ShouldDashBeforeCombo(comboMemoryPosition))
        {
            yield return DashTowardComboMemory();

            if (comboWindupAfterDash > 0f && state != State.Dead)
            {
                Stop();
                Play(idleStateName);
                yield return new WaitForSeconds(comboWindupAfterDash);
            }
        }

        if (state == State.Dead)
            yield break;

        Transform latestPlayer = GetPlayerTransform();
        if (latestPlayer != null && !IsTargetTooFar(latestPlayer))
            RememberComboTarget(latestPlayer.position);

        FaceTarget(comboMemoryPosition);
        lockedAttackFacing = facing;
        state = State.ComboAttack;
        Stop();
        Play(attackStateName);

        comboHitbox?.Begin(transform, lockedAttackFacing);
        comboHitbox?.PlayParryCue(transform);

        if (useAnimationEvents)
        {
            yield return WaitForAnimationDrivenAttack(comboHitbox, comboDuration, finishComboOnAnimationEvent);
            comboHitbox?.DisableHitbox();
            StopLunge();
            yield break;
        }

        float elapsed = 0f;
        for (int i = 0; i < comboHitWindows.Length; i++)
        {
            ComboHitWindow window = comboHitWindows[i];
            float waitTime = Mathf.Max(0f, window.startTime - elapsed);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
                elapsed += waitTime;
            }

            BeginComboHit(i, window, window.duration);
            yield return new WaitForSeconds(Mathf.Max(0f, window.duration));
            elapsed += Mathf.Max(0f, window.duration);

            if (state == State.Parried)
            {
                yield break;
            }
        }

        float remaining = Mathf.Max(0f, comboDuration - elapsed);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        comboHitbox?.DisableHitbox();
        StopLunge();
    }

    private bool ShouldLeapBeforeChain(Vector3 targetPosition)
    {
        return GetHorizontalDistance(targetPosition) > GetChainLeapDistanceThreshold();
    }

    private float GetChainLeapDistanceThreshold()
    {
        return GetChainForwardReach() * Mathf.Clamp(chainLeapReachRatio, 0.1f, 1f);
    }

    private float GetChainForwardReach()
    {
        EliteKillerAttackHitbox activeChainHitbox = chainHitbox != null ? chainHitbox : comboHitbox;
        return activeChainHitbox != null ? activeChainHitbox.ChainForwardReach : 2.1f;
    }

    private IEnumerator ChainPattern(Transform player)
    {
        Vector3 lastKnownPlayerPosition = player.position;
        FaceTarget(lastKnownPlayerPosition);

        if (ShouldLeapBeforeChain(lastKnownPlayerPosition))
        {
            yield return LeapTowardLastKnownPosition(lastKnownPlayerPosition);

            if (chainWindupAfterLeap > 0f && state != State.Dead)
            {
                Stop();
                Play(idleStateName);
                yield return new WaitForSeconds(chainWindupAfterLeap);
            }
        }

        if (state == State.Dead)
            yield break;

        Transform latestPlayer = GetPlayerTransform();
        FaceTarget(latestPlayer != null ? latestPlayer.position : lastKnownPlayerPosition);
        lockedAttackFacing = facing;

        state = State.ChainAttack;
        Stop();
        Play(chainAttackStateName);

        EliteKillerAttackHitbox activeChainHitbox = chainHitbox != null ? chainHitbox : comboHitbox;
        activeChainHitbox?.Begin(transform, lockedAttackFacing);
        activeChainHitbox?.PlayParryCue(transform);

        if (!useAnimationEvents)
            activeChainHitbox?.PlayChain(chainExtendDuration, chainHoldDuration, chainRetractDuration);

        float totalDuration = chainExtendDuration + chainHoldDuration + chainRetractDuration;
        if (useAnimationEvents)
            totalDuration = Mathf.Max(totalDuration, chainAnimationFallbackDuration);

        attackFinishedByAnimation = false;
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            if (useAnimationEvents && finishChainOnAnimationEvent && attackFinishedByAnimation)
                break;

            elapsed += Time.deltaTime;

            if (state == State.Parried)
            {
                yield break;
            }

            yield return null;
        }

        activeChainHitbox?.DisableHitbox();
    }

    private IEnumerator WaitForAnimationDrivenAttack(EliteKillerAttackHitbox hitbox, float fallbackDuration, bool requireFinishEvent)
    {
        attackFinishedByAnimation = false;

        float elapsed = 0f;
        while (elapsed < fallbackDuration)
        {
            if (requireFinishEvent && attackFinishedByAnimation)
                break;

            elapsed += Time.deltaTime;

            if (state == State.Parried)
            {
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator DashTowardComboMemory()
    {
        state = State.ComboDash;
        Play(dashStateName);

        Vector3 start = GetActorPosition();
        Vector3 rememberedTarget = hasComboMemory ? comboMemoryPosition : start + Vector3.right * facing;
        FaceTarget(rememberedTarget);

        Vector3 target = GetComboStandPosition(rememberedTarget, start, facing);

        yield return MoveAlongCurve(start, target, comboDashDuration, 0f, comboDashCurve, null);
        Stop();
    }

    private Vector3 GetComboStandPosition(Vector3 targetPosition, Vector3 start, int direction)
    {
        int dashDirection = direction >= 0 ? 1 : -1;
        Vector3 destination = start;
        destination.x = targetPosition.x - dashDirection * GetComboPreferredDistance();
        destination.y = start.y;
        destination.z = start.z;

        float travel = destination.x - start.x;
        if (comboDashMaxDistance > 0f)
            travel = Mathf.Clamp(travel, -comboDashMaxDistance, comboDashMaxDistance);

        destination.x = start.x + travel;
        return ClampToArena(destination);
    }

    private IEnumerator LeapTowardLastKnownPosition(Vector3 playerPosition)
    {
        state = State.ChainLeap;
        leapTouchTargets.Clear();
        Play(jumpStateName);
        leapTouchParry?.PlayCue(this, transform);

        Vector3 start = GetActorPosition();
        FaceTarget(playerPosition);

        Vector3 target = playerPosition - Vector3.right * facing * leapLandingOffsetFromPlayer;
        target.y = start.y;
        target.z = start.z;
        target = ClampToArena(target);

        yield return MoveAlongCurve(start, target, leapDuration, leapArcHeight, leapCurve, normalizedTime =>
        {
            if (normalizedTime > 0.55f)
                Play(fallStateName);
        });

        Stop();
        PlayLeapLandingFeedback();
    }

    private void PlayLeapLandingFeedback()
    {
        if (cameraController == null || leapLandingShakeDuration <= 0f || leapLandingShakeMagnitude <= 0f)
            return;

        cameraController.Shake(leapLandingShakeDuration, leapLandingShakeMagnitude);
    }

    private IEnumerator MoveAlongCurve(Vector3 start, Vector3 target, float duration, float arcHeight, AnimationCurve curve, Action<float> onProgress)
    {
        if (duration <= 0f)
        {
            SetActorPosition(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;
            Vector3 nextPosition = Vector3.Lerp(start, target, t);

            if (arcHeight > 0f)
                nextPosition.y += Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;

            SetActorPosition(ClampToArena(nextPosition));
            onProgress?.Invoke(normalizedTime);
            yield return null;
        }

        SetActorPosition(target);
    }

    private void StartLunge(float distance, float duration)
    {
        StartLunge(distance, duration, facing);
    }

    private void StartLunge(float distance, float duration, int direction)
    {
        StopLunge();
        lungeRoutine = StartCoroutine(Lunge(distance, duration, direction));
    }

    private IEnumerator Lunge(float distance, float duration, int direction)
    {
        Vector3 start = GetActorPosition();
        int lungeDirection = direction >= 0 ? 1 : -1;
        Vector3 target = ClampToArena(start + Vector3.right * lungeDirection * distance);

        yield return MoveAlongCurve(start, target, duration, 0f, comboDashCurve, null);
        lungeRoutine = null;
    }

    private void StopLunge()
    {
        if (lungeRoutine == null)
            return;

        StopCoroutine(lungeRoutine);
        lungeRoutine = null;
    }

    private void StartParried()
    {
        state = State.Parried;
        Stop();
        StopLunge();
        comboHitbox?.DisableHitbox();
        chainHitbox?.DisableHitbox();
        Play(hitStateName);
    }

    public void OnParried(PlayerParry parry, Vector3 attackerPosition)
    {
        if (state == State.Parried || state == State.Dead)
            return;

        StartParried();
    }

    private void HandleDeath()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;
        active = false;
        StopCombatRoutine();
        StopLunge();
        Stop();
        comboHitbox?.DisableHitbox();
        chainHitbox?.DisableHitbox();

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        SpawnDeathDrop();
        Play(deathStateName);
    }

    private void SpawnDeathDrop()
    {
        if (deathDropPrefab == null)
            return;

        if (dropOnlyOnce && deathDropSpawned)
            return;

        Vector3 spawnPosition = deathDropSpawnPoint != null
            ? deathDropSpawnPoint.position
            : GetActorPosition();

        spawnPosition += (Vector3)deathDropOffset;
        Instantiate(deathDropPrefab, spawnPosition, Quaternion.identity);
        deathDropSpawned = true;
    }

    public void BeginComboHitFromAnimation(int hitNumber)
    {
        if (state != State.ComboAttack)
            return;

        int index = hitNumber <= 0 ? 0 : hitNumber - 1;
        ComboHitWindow hit = GetComboEventHit(index);
        BeginComboHit(index, hit, 0f);
    }

    private void BeginComboHit(int hitIndex, ComboHitWindow hit, float timedDuration)
    {
        RefreshComboTargetMemory();

        float advanceDistance = GetComboAdvanceDistance(hitIndex, hit);
        if (advanceDistance > 0f && hit.lungeDuration > 0f)
            StartLunge(advanceDistance, hit.lungeDuration, lockedAttackFacing);

        if (timedDuration > 0f)
            comboHitbox?.EnableTimedMeleeHitbox(timedDuration, hit.clearPreviousHits);
        else
            comboHitbox?.EnableMeleeHitbox(hit.clearPreviousHits);
    }

    private float GetComboAdvanceDistance(int hitIndex, ComboHitWindow hit)
    {
        float desiredAdvance = Mathf.Max(0f, hit.lungeDistance);
        if (IsFinalComboHit(hitIndex))
            desiredAdvance += Mathf.Max(0f, comboFinalLungeBonus);

        float forwardCorrection = GetComboForwardCorrection();
        if (forwardCorrection > 0f)
            desiredAdvance = Mathf.Max(desiredAdvance, Mathf.Min(forwardCorrection, Mathf.Max(0f, comboForwardCorrectionLimit)));

        float safeAdvance = GetSafeComboAdvanceDistance();
        return Mathf.Min(desiredAdvance, safeAdvance);
    }

    private float GetComboForwardCorrection()
    {
        if (!hasComboMemory)
            return 0f;

        return GetForwardDistance(comboMemoryPosition, lockedAttackFacing) - GetComboPreferredDistance();
    }

    private float GetSafeComboAdvanceDistance()
    {
        if (!hasComboMemory)
            return float.PositiveInfinity;

        return Mathf.Max(0f, GetForwardDistance(comboMemoryPosition, lockedAttackFacing) - GetComboMinimumStrikeDistance());
    }

    private bool IsFinalComboHit(int hitIndex)
    {
        return hitIndex >= GetComboHitCount() - 1;
    }

    private int GetComboHitCount()
    {
        ComboHitWindow[] source = useAnimationEvents ? comboEventHits : comboHitWindows;
        return source != null && source.Length > 0 ? source.Length : 1;
    }

    private ComboHitWindow GetComboEventHit(int index)
    {
        if (comboEventHits == null || comboEventHits.Length == 0)
            return default;

        return comboEventHits[Mathf.Clamp(index, 0, comboEventHits.Length - 1)];
    }

    public void DisableComboHitboxFromAnimation()
    {
        comboHitbox?.DisableHitbox();
    }

    public void PlayChainHitboxFromAnimation()
    {
        if (state != State.ChainAttack)
            return;

        EliteKillerAttackHitbox activeChainHitbox = chainHitbox != null ? chainHitbox : comboHitbox;
        activeChainHitbox?.Begin(transform, lockedAttackFacing);
        activeChainHitbox?.PlayParryCue(transform);
        activeChainHitbox?.PlayChain(chainExtendDuration, chainHoldDuration, chainRetractDuration);
    }

    public void FinishCurrentAttackFromAnimation()
    {
        attackFinishedByAnimation = true;
        comboHitbox?.DisableHitbox();
        chainHitbox?.DisableHitbox();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryLeapTouchDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryLeapTouchDamage(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLeapTouchDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryLeapTouchDamage(collision.collider);
    }

    private void TryLeapTouchDamage(Collider2D other)
    {
        if (state != State.ChainLeap || other == null || leapTouchTargets.Contains(other))
            return;

        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerMovement>() == null)
            return;

        Health playerHealth = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (playerHealth == null)
            return;

        leapTouchTargets.Add(other);
        if (leapTouchParry != null && leapTouchParry.TryParry(other, GetActorPosition(), this))
            return;

        playerHealth.TakeDamage(leapTouchDamage, GetActorPosition());
    }

    private Transform GetPlayerTransform()
    {
        if (!PlayerReference.IsAvailable)
            return null;

        return PlayerReference.Player;
    }

    private bool IsTargetTooFar(Transform target)
    {
        if (target == null || maxTargetDistance <= 0f)
            return false;

        if (IsInsideArenaHorizontal(target.position))
            return false;

        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : GetActorPosition();
        return Vector2.Distance(origin, target.position) > maxTargetDistance;
    }

    private bool IsInsideArenaHorizontal(Vector3 position)
    {
        if (leftArenaPoint == null || rightArenaPoint == null)
            return false;

        float minX = Mathf.Min(leftArenaPoint.position.x, rightArenaPoint.position.x);
        float maxX = Mathf.Max(leftArenaPoint.position.x, rightArenaPoint.position.x);
        return position.x >= minX && position.x <= maxX;
    }

    private float GetHorizontalDistance(Vector3 target)
    {
        return Mathf.Abs(target.x - GetActorPosition().x);
    }

    private void RememberComboTarget(Vector3 targetPosition)
    {
        comboMemoryPosition = targetPosition;
        hasComboMemory = true;
    }

    private void RefreshComboTargetMemory()
    {
        if (!updateComboTargetDuringAttack)
            return;

        Transform player = GetPlayerTransform();
        if (player == null || IsTargetTooFar(player))
            return;

        float forwardDistance = GetForwardDistance(player.position, lockedAttackFacing);
        if (state == State.ComboAttack && forwardDistance < -GetComboPreferredDistance())
            return;

        RememberComboTarget(player.position);
    }

    private int GetDirectionTo(Vector3 target, int fallbackDirection)
    {
        float toTarget = target.x - GetActorPosition().x;
        if (Mathf.Approximately(toTarget, 0f))
            return fallbackDirection >= 0 ? 1 : -1;

        return toTarget > 0f ? 1 : -1;
    }

    private float GetForwardDistance(Vector3 target, int direction)
    {
        int forward = direction >= 0 ? 1 : -1;
        return (target.x - GetActorPosition().x) * forward;
    }

    private float GetComboPreferredDistance()
    {
        float preferredDistance = comboDashStopDistance > 0f ? comboDashStopDistance : comboAttackDistance;
        float minimumDistance = GetComboMinimumStrikeDistance();
        float maximumDistance = GetComboMaximumStrikeDistance();

        if (maximumDistance >= minimumDistance)
            preferredDistance = Mathf.Clamp(preferredDistance, minimumDistance, maximumDistance);

        return Mathf.Max(0f, preferredDistance);
    }

    private float GetComboDashDistanceThreshold()
    {
        float threshold = comboDashDistanceThreshold > 0f ? comboDashDistanceThreshold : GetComboPreferredDistance();
        return Mathf.Max(threshold, GetComboMinimumStrikeDistance());
    }

    private float GetComboMinimumStrikeDistance()
    {
        return Mathf.Max(0f, GetComboInnerReach() + Mathf.Max(0f, comboInnerReachPadding));
    }

    private float GetComboMaximumStrikeDistance()
    {
        float maximumDistance = GetComboOuterReach() - Mathf.Max(0f, comboInnerReachPadding);
        return Mathf.Max(GetComboMinimumStrikeDistance(), maximumDistance);
    }

    private float GetComboInnerReach()
    {
        return comboHitbox != null ? comboHitbox.MeleeForwardInnerReach : 0f;
    }

    private float GetComboOuterReach()
    {
        if (comboHitbox != null)
            return comboHitbox.MeleeForwardReach;

        return Mathf.Max(comboAttackDistance, comboDashStopDistance);
    }

    private void FacePlayerIfAvailable()
    {
        Transform player = GetPlayerTransform();
        if (player != null)
            FaceTarget(player.position);
    }

    private void FaceTarget(Vector3 target)
    {
        float toTarget = target.x - GetActorPosition().x;
        if (Mathf.Approximately(toTarget, 0f))
            return;

        int nextFacing = toTarget > 0f ? 1 : -1;
        if (nextFacing == facing)
            return;

        facing = nextFacing;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (visual != null)
        {
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            visual.localScale = scale;
        }

        comboHitbox?.SetFacing(facing);
        chainHitbox?.SetFacing(facing);
    }

    private Vector3 ClampToArena(Vector3 position)
    {
        if (leftArenaPoint == null || rightArenaPoint == null)
            return position;

        float minX = Mathf.Min(leftArenaPoint.position.x, rightArenaPoint.position.x);
        float maxX = Mathf.Max(leftArenaPoint.position.x, rightArenaPoint.position.x);
        position.x = Mathf.Clamp(position.x, minX, maxX);
        return position;
    }

    private Vector3 GetActorPosition()
    {
        return actorRoot != null ? actorRoot.position : transform.position;
    }

    private void CacheEncounterSpawnPositionIfNeeded()
    {
        if (encounterSpawnPositionInitialized)
            return;

        encounterSpawnPosition = GetActorPosition();
        encounterSpawnPositionInitialized = true;
    }

    private void SetActorPosition(Vector3 position)
    {
        if (actorRoot != null)
            actorRoot.position = position;
        else
            transform.position = position;
    }

    private void Stop()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void StopCombatRoutine()
    {
        if (combatRoutine == null)
            return;

        StopCoroutine(combatRoutine);
        combatRoutine = null;
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

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : GetActorPosition();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, comboAttackDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, GetChainLeapDistanceThreshold());

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, GetChainForwardReach());

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, maxTargetDistance);

        if (leftArenaPoint != null && rightArenaPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftArenaPoint.position, rightArenaPoint.position);
        }
    }
}
