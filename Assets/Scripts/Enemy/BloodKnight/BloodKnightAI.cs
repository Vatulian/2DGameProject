using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BloodKnightAI : MonoBehaviour, IParryReceiver
{
    private enum State
    {
        Patrol,
        Chase,
        LostPlayer,
        ReturnToPatrol,
        TurnPrep,
        ChasePrep,
        AttackPrep,
        Attack,
        AttackRecovery,
        HitStun,
        Parried
    }

    [Header("Refs")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private Transform detectionOrigin;
    [SerializeField] private BloodKnightAttack attack;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float turnPause = 0.35f;

    [Header("Movement Safety")]
    [SerializeField] private float chaseLeashExtra = 2f;
    [SerializeField] private LayerMask groundLayers = 1 << 8;
    [SerializeField] private float groundCheckForwardOffset = 0.55f;
    [SerializeField] private float groundCheckDownDistance = 1.2f;
    [SerializeField] private float groundCheckVerticalOffset = 0.08f;

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2.2f;
    [SerializeField] private float awarenessRadius = 3.2f;
    [SerializeField] private float awarenessPadding = 0.75f;
    [SerializeField] private float attackDistance = 0.9f;
    [SerializeField] private float chaseSpeed = 2.1f;
    [SerializeField] private float lostPlayerSpeed = 1.8f;
    [SerializeField] private float lostPlayerWaitTime = 1.2f;
    [SerializeField] private float turnPrepTime = 0.4f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;

    [Header("Attack Flow")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackPrepTime = 0.25f;
    [SerializeField] private float attackStateDuration = 1.35f;
    [SerializeField] private float attackRecoveryTime = 0.35f;
    [SerializeField] private float parriedStunTime = 0.7f;

    [Header("Attack Dash")]
    [SerializeField] private float attackDashDistance = 0.75f;
    [SerializeField] private float attackDashDuration = 0.16f;
    [SerializeField] private AnimationCurve attackDashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float attackDashWallSkin = 0.03f;
    [SerializeField] private float attackDashWallProbeInset = 0.08f;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string attackStateName = "double slash";
    [SerializeField] private string parriedStateName = "hit";

    private Rigidbody2D rb;
    private State state = State.Patrol;
    private int facing = 1;
    private float leftX;
    private float rightX;
    private float timer;
    private float cooldownTimer;
    private bool waitingAtTurn;
    private bool waitingAtLostPosition;
    private Vector3 lastKnownPlayerPos;
    private string currentAnim = "";
    private Coroutine attackDashRoutine;

    public int Facing => facing;

    private void OnValidate()
    {
        awarenessPadding = Mathf.Max(0f, awarenessPadding);
        awarenessRadius = Mathf.Max(awarenessRadius, detectionDistance + awarenessPadding);
        chaseLeashExtra = Mathf.Max(0f, chaseLeashExtra);
        groundCheckForwardOffset = Mathf.Max(0f, groundCheckForwardOffset);
        groundCheckDownDistance = Mathf.Max(0.05f, groundCheckDownDistance);
        attackDashWallSkin = Mathf.Max(0f, attackDashWallSkin);
        attackDashWallProbeInset = Mathf.Max(0f, attackDashWallProbeInset);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();

        Transform root = transform.Find("Root");

        if (visual == null)
            visual = root != null ? root.Find("Visual") : transform.Find("Visual");

        if (visual == null)
            visual = transform;

        if (anim == null)
            anim = visual.GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (leftPoint == null)
            leftPoint = transform.Find("LeftP");

        if (rightPoint == null)
            rightPoint = transform.Find("RightP");

        if (detectionOrigin == null)
            detectionOrigin = root != null ? root : transform;

        if (attack == null)
            attack = GetComponentInChildren<BloodKnightAttack>();

        CachePatrolBounds();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        ApplyFacing();
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        switch (state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.LostPlayer:
                TickLostPlayer();
                break;
            case State.ReturnToPatrol:
                TickReturnToPatrol();
                break;
            case State.TurnPrep:
                TickTurnPrep();
                break;
            case State.ChasePrep:
                TickChasePrep();
                break;
            case State.AttackPrep:
                TickAttackPrep();
                break;
            case State.Attack:
                TickAttack();
                break;
            case State.AttackRecovery:
                TickAttackRecovery();
                break;
            case State.HitStun:
                TickHitStun();
                break;
            case State.Parried:
                TickParried();
                break;
        }
    }

    private void TickPatrol()
    {
        if (cooldownTimer >= attackCooldown && CanDetectPlayerAhead())
        {
            StartChase(GetPlayerTransform());
            return;
        }

        if (leftPoint == null || rightPoint == null)
        {
            Stop();
            Play(idleStateName);
            return;
        }

        if (!IsInsidePatrolBounds())
        {
            StartReturnToPatrol();
            return;
        }

        if (waitingAtTurn)
        {
            Stop();
            Play(idleStateName);
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                waitingAtTurn = false;
                facing *= -1;
                ApplyFacing();
            }

            return;
        }

        if (!CanMoveInDirection(facing, false))
        {
            StartPatrolTurn();
            return;
        }

        Play(runStateName);
        rb.velocity = new Vector2(facing * patrolSpeed, 0f);

        float targetX = facing > 0 ? rightX : leftX;
        bool reachedPoint = (facing > 0 && transform.position.x >= targetX)
            || (facing < 0 && transform.position.x <= targetX);

        if (reachedPoint)
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            StartPatrolTurn();
        }
    }

    private void TickChase()
    {
        Transform player = GetPlayerTransform();

        if (!CanSensePlayerInAwareness(player))
        {
            StartLostPlayer();
            return;
        }

        if (player != null)
            lastKnownPlayerPos = player.position;

        if (IsPlayerBehind(player))
        {
            StartTurnPrep();
            return;
        }

        if (cooldownTimer >= attackCooldown && CanReachPlayerWithAttackRay() && CanAttackActuallyReachPlayer())
        {
            StartAttackPrep();
            return;
        }

        if (!CanMoveInDirection(facing, true))
        {
            Stop();
            Play(idleStateName);
            return;
        }

        Play(runStateName);
        rb.velocity = new Vector2(facing * chaseSpeed, 0f);
        ClampToCombatBounds();
    }

    private void StartChase(Transform player)
    {
        state = State.Chase;
        waitingAtLostPosition = false;

        if (player != null)
            lastKnownPlayerPos = player.position;
    }

    private void StartLostPlayer()
    {
        state = State.LostPlayer;
        waitingAtLostPosition = false;
        Stop();
    }

    private void TickLostPlayer()
    {
        Transform player = GetPlayerTransform();
        if (CanSensePlayerInAwareness(player) || CanDetectPlayerAhead())
        {
            StartChase(player);
            return;
        }

        Vector3 target = ClampPositionToPatrolBounds(lastKnownPlayerPos);
        float distanceX = Mathf.Abs(target.x - transform.position.x);

        if (distanceX < 0.15f)
        {
            WaitAtLostPosition();
            return;
        }

        int nextFacing = target.x >= transform.position.x ? 1 : -1;
        if (nextFacing != facing)
        {
            facing = nextFacing;
            ApplyFacing();
        }

        if (!CanMoveInDirection(facing, true))
        {
            WaitAtLostPosition();
            return;
        }

        Play(runStateName);
        rb.velocity = new Vector2(facing * lostPlayerSpeed, 0f);
        ClampToCombatBounds();
    }

    private void WaitAtLostPosition()
    {
        if (!waitingAtLostPosition)
        {
            waitingAtLostPosition = true;
            timer = lostPlayerWaitTime;
        }

        Stop();
        Play(idleStateName);
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            waitingAtLostPosition = false;
            StartReturnToPatrol();
        }
    }

    private void StartReturnToPatrol()
    {
        state = State.ReturnToPatrol;
        waitingAtTurn = false;
        waitingAtLostPosition = false;
        Stop();
    }

    private void TickReturnToPatrol()
    {
        if (leftPoint == null || rightPoint == null)
        {
            state = State.Patrol;
            return;
        }

        if (IsInsidePatrolBounds())
        {
            state = State.Patrol;
            return;
        }

        float targetX = Mathf.Clamp(transform.position.x, leftX, rightX);
        int returnDirection = targetX >= transform.position.x ? 1 : -1;

        if (returnDirection != facing)
        {
            facing = returnDirection;
            ApplyFacing();
        }

        if (!HasGroundAhead(returnDirection))
        {
            Stop();
            Play(idleStateName);
            return;
        }

        Play(runStateName);
        rb.velocity = new Vector2(returnDirection * lostPlayerSpeed, 0f);

        bool reachedPatrol = (returnDirection > 0 && transform.position.x >= targetX)
            || (returnDirection < 0 && transform.position.x <= targetX);

        if (reachedPatrol)
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            Stop();
            state = State.Patrol;
        }
    }

    private void StartTurnPrep()
    {
        state = State.TurnPrep;
        timer = turnPrepTime;
        Stop();
        Play(idleStateName);
    }

    private void StartChasePrep()
    {
        state = State.ChasePrep;
        timer = turnPrepTime;
        waitingAtTurn = false;
        waitingAtLostPosition = false;
        Stop();
        Play(idleStateName);
    }

    private void TickTurnPrep()
    {
        Stop();
        Play(idleStateName);

        Transform player = GetPlayerTransform();
        if (!CanSensePlayerInAwareness(player))
        {
            StartLostPlayer();
            return;
        }

        lastKnownPlayerPos = player.position;

        if (player != null && !IsPlayerBehind(player))
        {
            state = State.Chase;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        facing *= -1;
        ApplyFacing();

        Transform latestPlayer = GetPlayerTransform();
        if (latestPlayer != null)
            lastKnownPlayerPos = latestPlayer.position;

        state = State.Chase;
    }

    private void TickChasePrep()
    {
        Stop();
        Play(idleStateName);

        Transform player = GetPlayerTransform();
        if (player == null)
        {
            StartReturnToPatrol();
            return;
        }

        lastKnownPlayerPos = player.position;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        FacePlayer(player);
        StartChase(player);
    }

    private void StartAttackPrep()
    {
        state = State.AttackPrep;
        timer = attackPrepTime;
        Stop();
        Play(idleStateName);
        attack?.PlayParryCue(transform);
    }

    private void TickAttackPrep()
    {
        Stop();

        Transform player = GetPlayerTransform();
        if (!CanSensePlayerInAwareness(player))
        {
            StartLostPlayer();
            return;
        }

        lastKnownPlayerPos = player.position;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            StartAttack();
    }

    private void StartAttack()
    {
        if (attack == null)
            return;

        StopAttackDash();
        state = State.Attack;
        timer = attackStateDuration;
        cooldownTimer = 0f;
        Stop();
        Play(attackStateName);
        attack.Begin(transform, bodyCollider, facing);
    }

    private void TickAttack()
    {
        Stop();

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        attack?.DisableHitbox();
        StopAttackDash();
        StartAttackRecovery();
    }

    private void StartAttackRecovery()
    {
        state = State.AttackRecovery;
        timer = attackRecoveryTime;
        Stop();
        Play(idleStateName);
    }

    private void TickAttackRecovery()
    {
        Stop();
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        Transform player = GetPlayerTransform();
        if (CanSensePlayerInAwareness(player) || CanDetectPlayerAhead())
            StartChase(player);
        else
            StartLostPlayer();
    }

    public void InterruptForHit(float duration)
    {
        state = State.HitStun;
        timer = Mathf.Max(0f, duration);
        waitingAtTurn = false;
        currentAnim = "";
        Stop();
        StopAttackDash();
        attack?.DisableHitbox();
    }

    private void TickHitStun()
    {
        Stop();
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            currentAnim = "";
            StartChasePrep();
        }
    }

    private void StartParried()
    {
        state = State.Parried;
        timer = parriedStunTime;
        StopAttackDash();
        Stop();
        Play(parriedStateName);
    }

    public void OnParried(PlayerParry parry, Vector3 attackerPosition)
    {
        if (state == State.Parried)
            return;

        StartParried();
    }

    private void TickParried()
    {
        Stop();
        timer -= Time.deltaTime;

        if (timer <= 0f)
            StartReturnToPatrol();
    }

    private void StartPatrolTurn()
    {
        Stop();
        waitingAtTurn = true;
        timer = turnPause;
    }

    public void BeginAttackDash()
    {
        if (state != State.Attack || attackDashDistance <= 0f || attackDashDuration <= 0f)
            return;

        StopAttackDash();
        attackDashRoutine = StartCoroutine(DashForward(attackDashDistance, attackDashDuration));
    }

    public void EndAttackDash()
    {
        StopAttackDash();
    }

    public void FinishAttackFromAnimation()
    {
        if (state != State.Attack)
            return;

        attack?.DisableHitbox();
        StopAttackDash();
        StartAttackRecovery();
    }

    private IEnumerator DashForward(float distance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 target = start + Vector3.right * facing * distance;
        target = ClampPositionToPatrolBounds(target);
        target = ClampAttackDashTargetBeforeWall(start, target);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = attackDashCurve != null ? attackDashCurve.Evaluate(normalizedTime) : normalizedTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        attackDashRoutine = null;
    }

    private Vector3 ClampAttackDashTargetBeforeWall(Vector3 start, Vector3 target)
    {
        if (bodyCollider == null)
            return target;

        float deltaX = target.x - start.x;
        if (Mathf.Abs(deltaX) <= 0.001f)
            return target;

        float direction = Mathf.Sign(deltaX);
        float travelDistance = Mathf.Abs(deltaX);
        int wallMask = groundLayers | obstructionLayers;
        if (wallMask == 0)
            return target;

        Bounds bounds = bodyCollider.bounds;
        float frontOffset = direction > 0f
            ? bounds.max.x - transform.position.x
            : transform.position.x - bounds.min.x;

        float rayStartX = direction > 0f ? bounds.max.x : bounds.min.x;
        float rayDistance = travelDistance + attackDashWallSkin;
        RaycastHit2D nearestHit = default;
        bool hasHit = false;

        ProbeDashWall(rayStartX, bounds.center.y, direction, rayDistance, wallMask, ref nearestHit, ref hasHit);

        float inset = Mathf.Min(attackDashWallProbeInset, Mathf.Max(0f, bounds.extents.y - 0.01f));
        ProbeDashWall(rayStartX, bounds.max.y - inset, direction, rayDistance, wallMask, ref nearestHit, ref hasHit);

        float lowerProbeY = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.35f);
        ProbeDashWall(rayStartX, lowerProbeY, direction, rayDistance, wallMask, ref nearestHit, ref hasHit);

        if (!hasHit)
            return target;

        float clampedX = nearestHit.point.x - direction * (frontOffset + attackDashWallSkin);
        clampedX = direction > 0f
            ? Mathf.Clamp(clampedX, start.x, target.x)
            : Mathf.Clamp(clampedX, target.x, start.x);

        target.x = clampedX;
        return target;
    }

    private void ProbeDashWall(
        float rayStartX,
        float rayStartY,
        float direction,
        float rayDistance,
        int wallMask,
        ref RaycastHit2D nearestHit,
        ref bool hasHit)
    {
        Vector2 rayDirection = direction > 0f ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(rayStartX, rayStartY), rayDirection, rayDistance, wallMask);

        if (!IsAttackDashWallHit(hit, rayDirection))
            return;

        if (hasHit && hit.distance >= nearestHit.distance)
            return;

        nearestHit = hit;
        hasHit = true;
    }

    private bool IsAttackDashWallHit(RaycastHit2D hit, Vector2 rayDirection)
    {
        if (hit.collider == null || hit.collider.isTrigger)
            return false;

        if (hit.collider == bodyCollider || hit.collider.transform.IsChildOf(transform))
            return false;

        if (hit.normal.sqrMagnitude <= 0.0001f)
            return false;

        return Vector2.Dot(hit.normal, -rayDirection) >= 0.5f;
    }

    private void StopAttackDash()
    {
        if (attackDashRoutine == null)
            return;

        StopCoroutine(attackDashRoutine);
        attackDashRoutine = null;
    }

    private bool CanDetectPlayerAhead()
    {
        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Vector2 direction = facing > 0 ? Vector2.right : Vector2.left;
        int mask = playerLayer | obstructionLayers;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, detectionDistance, mask);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private bool CanReachPlayerWithAttackRay()
    {
        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Vector2 direction = facing > 0 ? Vector2.right : Vector2.left;
        int mask = playerLayer | obstructionLayers;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, attackDistance, mask);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private bool CanAttackActuallyReachPlayer()
    {
        Transform player = GetPlayerTransform();
        if (player == null || attack == null)
            return false;

        float toPlayerX = player.position.x - transform.position.x;
        if (Mathf.Approximately(toPlayerX, 0f))
            return true;

        if (Mathf.Sign(toPlayerX) != facing)
            return false;

        Vector3 dashTarget = transform.position + Vector3.right * facing * attackDashDistance;
        dashTarget = ClampPositionToPatrolBounds(dashTarget);

        float effectiveDashDistance = Mathf.Abs(dashTarget.x - transform.position.x);
        float effectiveReach = effectiveDashDistance + attack.ForwardReach;

        return Mathf.Abs(toPlayerX) <= effectiveReach;
    }

    private bool CanSensePlayerInAwareness(Transform player)
    {
        if (player == null)
            return false;

        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Vector2 target = player.position;

        if (Vector2.Distance(origin, target) > GetEffectiveAwarenessRadius())
            return false;

        if (obstructionLayers.value == 0)
            return true;

        RaycastHit2D wallHit = Physics2D.Linecast(origin, target, obstructionLayers);
        return wallHit.collider == null;
    }

    private float GetEffectiveAwarenessRadius()
    {
        return Mathf.Max(awarenessRadius, detectionDistance + awarenessPadding);
    }

    private void ApplyFacing()
    {
        if (visual == null)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        visual.localScale = scale;

        attack?.SetFacing(facing);
    }

    private void FacePlayerIfAvailable()
    {
        if (!PlayerReference.IsAvailable)
            return;

        Transform player = PlayerReference.Player;
        if (player == null)
            return;

        FacePlayer(player);
    }

    private void FacePlayer(Transform player)
    {
        int nextFacing = player.position.x >= transform.position.x ? 1 : -1;
        if (nextFacing == facing)
            return;

        facing = nextFacing;
        ApplyFacing();
    }

    private Transform GetPlayerTransform()
    {
        if (!PlayerReference.IsAvailable)
            return null;

        return PlayerReference.Player;
    }

    private bool IsPlayerBehind(Transform player)
    {
        if (player == null)
            return false;

        float toPlayerX = player.position.x - transform.position.x;
        if (Mathf.Approximately(toPlayerX, 0f))
            return false;

        return Mathf.Sign(toPlayerX) != facing;
    }

    private void Stop()
    {
        rb.velocity = Vector2.zero;
    }

    private bool CanMoveInDirection(int direction, bool useCombatBounds)
    {
        return IsWithinMovementBounds(direction, useCombatBounds) && HasGroundAhead(direction);
    }

    private bool IsWithinMovementBounds(int direction, bool useCombatBounds)
    {
        if (leftPoint == null || rightPoint == null)
            return true;

        float minX = useCombatBounds ? leftX - chaseLeashExtra : leftX;
        float maxX = useCombatBounds ? rightX + chaseLeashExtra : rightX;
        float probeX = transform.position.x + Mathf.Sign(direction) * groundCheckForwardOffset;

        return probeX >= minX && probeX <= maxX;
    }

    private bool IsInsidePatrolBounds()
    {
        if (leftPoint == null || rightPoint == null)
            return true;

        return transform.position.x >= leftX && transform.position.x <= rightX;
    }

    private bool HasGroundAhead(int direction)
    {
        if (groundLayers.value == 0)
            return true;

        Vector2 origin = GetGroundCheckOrigin(direction);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDownDistance, groundLayers);
        return hit.collider != null;
    }

    private Vector2 GetGroundCheckOrigin(int direction)
    {
        Bounds bounds = bodyCollider != null ? bodyCollider.bounds : new Bounds(transform.position, Vector3.one);
        Vector2 foot = new Vector2(bounds.center.x, bounds.min.y + groundCheckVerticalOffset);
        return foot + Vector2.right * Mathf.Sign(direction) * groundCheckForwardOffset;
    }

    private void CachePatrolBounds()
    {
        if (leftPoint == null || rightPoint == null)
            return;

        leftX = leftPoint.position.x;
        rightX = rightPoint.position.x;

        if (leftX > rightX)
        {
            float swap = leftX;
            leftX = rightX;
            rightX = swap;
        }
    }

    private void ClampToPatrolBounds()
    {
        if (leftPoint == null || rightPoint == null)
            return;

        float clampedX = Mathf.Clamp(transform.position.x, leftX, rightX);
        if (Mathf.Approximately(clampedX, transform.position.x))
            return;

        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        Stop();
    }

    public Vector3 ClampPositionToPatrolBounds(Vector3 position)
    {
        if (leftPoint == null || rightPoint == null)
            return position;

        position.x = Mathf.Clamp(position.x, leftX - chaseLeashExtra, rightX + chaseLeashExtra);
        return position;
    }

    private void ClampToCombatBounds()
    {
        if (leftPoint == null || rightPoint == null)
            return;

        float clampedX = Mathf.Clamp(transform.position.x, leftX - chaseLeashExtra, rightX + chaseLeashExtra);
        if (Mathf.Approximately(clampedX, transform.position.x))
            return;

        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        Stop();
    }

    private void Play(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || anim == null || anim.runtimeAnimatorController == null)
            return;

        if (currentAnim == stateName)
            return;

        currentAnim = stateName;
        anim.Play(stateName);
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTf = detectionOrigin != null ? detectionOrigin : transform;
        int drawFacing = Application.isPlaying ? facing : (transform.localScale.x >= 0f ? 1 : -1);
        Vector3 direction = drawFacing > 0 ? Vector3.right : Vector3.left;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(originTf.position, originTf.position + direction * detectionDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(originTf.position, GetEffectiveAwarenessRadius());

        Gizmos.color = Color.red;
        Gizmos.DrawLine(originTf.position, originTf.position + direction * attackDistance);

        Gizmos.color = Color.green;
        Vector3 groundOrigin = GetGroundCheckOrigin(drawFacing);
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundCheckDownDistance);
    }
}
