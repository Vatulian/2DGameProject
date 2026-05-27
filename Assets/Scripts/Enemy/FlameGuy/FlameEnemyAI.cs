using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlameEnemyAI : MonoBehaviour, IParryReceiver
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    private enum State
    {
        Patrol,
        WaitAtEnd,
        Chase,
        TurnPrep,
        DamageChasePrep,
        ReturnToPatrol,
        Prep,
        Flame,
        Recovery
    }

    [Header("Refs")]
    [SerializeField] private Transform sprite;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform flameAreaTf;
    [SerializeField] private BoxCollider2D flameArea;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private Transform detectionOrigin;

    [Header("Detection (Cone)")]
    [SerializeField] private float viewDistance = 3.6f;

    [Tooltip("Degrees ABOVE the horizontal baseline (0 degrees).")]
    [SerializeField] private float upperAngle = 70f;

    [Tooltip("Degrees BELOW the horizontal baseline (0 degrees).")]
    [SerializeField] private float lowerAngle = 0f;

    [SerializeField] private float awarenessRadius = 4.75f;
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float endIdleTime = 1f;

    [Header("Movement Safety")]
    [SerializeField] private float chaseSpeed = 2.7f;
    [SerializeField] private float chaseLeashExtra = 2f;
    [SerializeField] private LayerMask groundLayers = 1 << 8;
    [SerializeField] private float groundCheckForwardOffset = 0.37f;
    [SerializeField] private float groundCheckDownDistance = 1f;
    [SerializeField] private float groundCheckVerticalOffset = 0.08f;
    [SerializeField] private float turnPrepTime = 0.3f;
    [SerializeField] private float damageAggroTime = 1.5f;

    [Header("Attack")]
    [SerializeField] private float prepTime = 0.5f;
    [SerializeField] private float flameDuration = 1.2f;
    [Tooltip("Minimum delay between separate flame entries. Staying inside the flame does not keep ticking damage.")]
    [SerializeField] private float damageInterval = 0.25f;
    [SerializeField] private float recoveryTime = 1.1f;
    [SerializeField] private int damage = 1;
    [SerializeField] private ParryAttackSettings flameParry = new ParryAttackSettings();
    [SerializeField] private float flameKnockbackSpeed = 13f;
    [SerializeField] private float flameKnockbackDuration = 0.22f;
    [SerializeField] private float flameKnockbackUpwardVelocity = 2.5f;

    [Tooltip("If true, once Prep starts the enemy will flame even if the player leaves the cone.")]
    [SerializeField] private bool commitAttack = true;

    [Header("Attack Facing")]
    [Tooltip("If true, once Prep starts the enemy will NOT turn until Flame ends.")]
    [SerializeField] private bool lockFacingDuringAttack = true;

    [Header("Spacing")]
    [SerializeField] private float retreatDistance = 1.15f;
    [SerializeField] private float retreatSpeed = 1.5f;

    private Rigidbody2D rb;
    private Health health;
    private Transform player;
    private Health playerHealth;

    private State state = State.Patrol;
    private int dir = 1;
    private float timer;
    private float damageAggroTimer;
    private float leftX;
    private float rightX;
    private readonly HashSet<Health> flameTargetsInside = new HashSet<Health>();
    private readonly HashSet<Health> currentFlameTargets = new HashSet<Health>();
    private readonly Dictionary<Health, float> nextFlameDamageTimes = new Dictionary<Health, float>();

    private int attackDir = 1;
    private Vector2 flameBaseLocalPos;
    private float flameAbsX;
    private float flameBaseY;
    private string currentAnim = "";

    private void OnValidate()
    {
        CacheFlameAreaReferences();
        ApplyFlameAreaLayer();

        viewDistance = Mathf.Max(0.05f, viewDistance);
        awarenessRadius = Mathf.Max(awarenessRadius, viewDistance);
        attackDistance = Mathf.Max(0.05f, attackDistance);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        chaseLeashExtra = Mathf.Max(0f, chaseLeashExtra);
        groundCheckForwardOffset = Mathf.Max(0f, groundCheckForwardOffset);
        groundCheckDownDistance = Mathf.Max(0.05f, groundCheckDownDistance);
        turnPrepTime = Mathf.Max(0f, turnPrepTime);
        damageAggroTime = Mathf.Max(0f, damageAggroTime);
        damageInterval = Mathf.Max(0f, damageInterval);
        flameKnockbackSpeed = Mathf.Max(0f, flameKnockbackSpeed);
        flameKnockbackDuration = Mathf.Max(0f, flameKnockbackDuration);
        flameKnockbackUpwardVelocity = Mathf.Max(0f, flameKnockbackUpwardVelocity);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        bodyCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();

        if (sprite == null)
            sprite = transform.Find("Sprite") ?? GetComponentInChildren<SpriteRenderer>()?.transform;

        if (anim == null)
            anim = sprite != null ? sprite.GetComponent<Animator>() : GetComponentInChildren<Animator>();

        CacheFlameAreaReferences();
        ApplyFlameAreaLayer();

        if (detectionOrigin == null)
            detectionOrigin = transform;

        if (flameArea != null)
            flameArea.enabled = false;

        if (flameAreaTf != null)
        {
            flameBaseLocalPos = flameAreaTf.localPosition;
            flameAbsX = Mathf.Abs(flameBaseLocalPos.x);
            flameBaseY = flameBaseLocalPos.y;

            if (flameArea != null)
                flameArea.offset = Vector2.zero;
        }

        CachePatrolBounds();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        ApplyFacing();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (damageAggroTimer > 0f)
            damageAggroTimer -= Time.deltaTime;

        if (!TryResolvePlayer() || leftPoint == null || rightPoint == null)
        {
            Stop();
            return;
        }

        switch (state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.WaitAtEnd:
                TickWait();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.TurnPrep:
                TickTurnPrep();
                break;
            case State.DamageChasePrep:
                TickDamageChasePrep();
                break;
            case State.ReturnToPatrol:
                TickReturnToPatrol();
                break;
            case State.Prep:
                TickPrep();
                break;
            case State.Flame:
                TickFlame();
                break;
            case State.Recovery:
                TickRecovery();
                break;
        }
    }

    private void TickPatrol()
    {
        if (CanSeePlayerCone(viewDistance))
        {
            StartChase();
            return;
        }

        if (!IsInsidePatrolBounds())
        {
            StartReturnToPatrol();
            return;
        }

        if (!CanMoveInDirection(dir, false))
        {
            StartWaitAtEnd();
            return;
        }

        PlayOnce("run");
        Move(dir, patrolSpeed);
        ApplyFacing();

        float targetX = dir == 1 ? rightX : leftX;
        if ((dir == 1 && transform.position.x >= targetX) || (dir == -1 && transform.position.x <= targetX))
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            StartWaitAtEnd();
        }
    }

    private void TickWait()
    {
        if (CanSeePlayerCone(viewDistance))
        {
            StartChase();
            return;
        }

        Stop();
        PlayOnce("idle");

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            dir *= -1;
            ApplyFacing();
            state = State.Patrol;
        }
    }

    private void StartChase()
    {
        state = State.Chase;
        FacePlayerIfAllowed();
    }

    private void TickChase()
    {
        if (!CanSensePlayer())
        {
            StartReturnToPatrol();
            return;
        }

        if (IsPlayerBehind())
        {
            StartTurnPrep();
            return;
        }

        if (CanReachPlayerForAttack())
        {
            StartPrep();
            return;
        }

        if (!CanMoveInDirection(dir, true))
        {
            Stop();
            PlayOnce("idle");
            return;
        }

        PlayOnce("run");
        Move(dir, chaseSpeed);
        ClampToCombatBounds();
    }

    private void StartTurnPrep()
    {
        state = State.TurnPrep;
        timer = turnPrepTime;
        Stop();
        PlayOnce("idle");
    }

    private void TickTurnPrep()
    {
        Stop();
        PlayOnce("idle");

        if (!CanSensePlayer())
        {
            StartReturnToPatrol();
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        FacePlayerIfAllowed();
        state = State.Chase;
    }

    private void StartDamageChasePrep()
    {
        state = State.DamageChasePrep;
        timer = turnPrepTime;
        damageAggroTimer = damageAggroTime;
        Stop();
        PlayOnce("idle");
    }

    private void TickDamageChasePrep()
    {
        Stop();
        PlayOnce("idle");

        if (player == null)
        {
            StartReturnToPatrol();
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        FacePlayerIfAllowed();
        damageAggroTimer = damageAggroTime;
        state = State.Chase;
    }

    private void StartReturnToPatrol()
    {
        state = State.ReturnToPatrol;
        Stop();
    }

    private void TickReturnToPatrol()
    {
        if (IsInsidePatrolBounds())
        {
            state = State.Patrol;
            return;
        }

        float targetX = Mathf.Clamp(transform.position.x, leftX, rightX);
        int returnDir = targetX >= transform.position.x ? 1 : -1;
        dir = returnDir;
        ApplyFacing();

        if (!HasGroundAhead(returnDir))
        {
            Stop();
            PlayOnce("idle");
            return;
        }

        PlayOnce("run");
        Move(returnDir, patrolSpeed);

        if ((returnDir == 1 && transform.position.x >= targetX) || (returnDir == -1 && transform.position.x <= targetX))
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            Stop();
            state = State.Patrol;
        }
    }

    private void StartWaitAtEnd()
    {
        Stop();
        state = State.WaitAtEnd;
        timer = endIdleTime;
        PlayOnce("idle");
    }

    private void StartPrep()
    {
        state = State.Prep;
        timer = prepTime;
        Stop();

        attackDir = player.position.x >= transform.position.x ? 1 : -1;
        if (lockFacingDuringAttack)
            dir = attackDir;

        ApplyFacing();
        PlayOnce("prep attack");
        flameParry?.PlayCue(this, flameAreaTf != null ? flameAreaTf : transform);
    }

    private void TickPrep()
    {
        Stop();

        if (!lockFacingDuringAttack)
            FacePlayerIfAllowed();
        else
            dir = attackDir;

        ApplyFacing();

        if (!commitAttack && !CanReachPlayerForAttack())
        {
            StartChase();
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            StartFlame();
    }

    private void StartFlame()
    {
        state = State.Flame;
        timer = flameDuration;
        flameTargetsInside.Clear();
        currentFlameTargets.Clear();
        Stop();

        if (lockFacingDuringAttack)
            dir = attackDir;

        ApplyFacing();
        PlayOnce("flame");

        if (flameArea != null)
            flameArea.enabled = true;
    }

    private void TickFlame()
    {
        Stop();

        if (!lockFacingDuringAttack)
            FacePlayerIfAllowed();
        else
            dir = attackDir;

        ApplyFacing();

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (flameArea != null)
                flameArea.enabled = false;

            flameTargetsInside.Clear();
            currentFlameTargets.Clear();
            state = State.Recovery;
            timer = recoveryTime;
            return;
        }

        UpdateFlameContactDamage();
    }

    private void TickRecovery()
    {
        Stop();

        if (player != null && Mathf.Abs(player.position.x - transform.position.x) < retreatDistance)
        {
            int retreatDir = player.position.x >= transform.position.x ? -1 : 1;
            if (CanMoveInDirection(retreatDir, true))
            {
                dir = retreatDir;
                ApplyFacing();
                Move(retreatDir, retreatSpeed);
                ClampToCombatBounds();
            }
        }

        PlayOnce("idle");

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        if (CanSensePlayer())
            StartChase();
        else
            StartReturnToPatrol();
    }

    private bool CanSeePlayerCone(float distance)
    {
        if (player == null)
            return false;

        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist <= Mathf.Epsilon || dist > distance)
            return false;

        Vector2 dirToPlayer = toPlayer / dist;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        float up = Mathf.Clamp(upperAngle, 0f, 179f);
        float down = Mathf.Clamp(lowerAngle, 0f, 179f);
        bool inCone;

        if (dir >= 0)
        {
            float minAngle = 360f - down;
            inCone = angle >= minAngle || angle <= up;
        }
        else
        {
            float minAngle = 180f - up;
            float maxAngle = 180f + down;
            inCone = angle >= minAngle && angle <= maxAngle;
        }

        if (!inCone)
            return false;

        return HasLineOfSight(origin, dirToPlayer, dist);
    }

    private bool CanReachPlayerForAttack()
    {
        return CanSeePlayerCone(Mathf.Min(viewDistance, attackDistance));
    }

    private bool CanSensePlayer()
    {
        if (player == null)
            return false;

        if (damageAggroTimer > 0f)
            return true;

        Vector2 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Vector2 target = player.position;
        Vector2 toPlayer = target - origin;
        float distance = toPlayer.magnitude;

        if (distance > awarenessRadius)
            return false;

        if (distance <= Mathf.Epsilon)
            return true;

        if (HasLineOfSight(origin, toPlayer / distance, distance))
            return true;

        return CanSeePlayerCone(viewDistance);
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 direction, float distance)
    {
        if (!useLineOfSight)
            return true;

        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, mask);
        return hit.collider == null || hit.collider.CompareTag("Player");
    }

    private void ApplyFacing()
    {
        if (sprite != null)
        {
            Vector3 scale = sprite.localScale;
            scale.x = Mathf.Abs(scale.x) * dir;
            sprite.localScale = scale;
        }

        if (flameAreaTf != null)
            flameAreaTf.localPosition = new Vector3(flameAbsX * dir, flameBaseY, flameAreaTf.localPosition.z);
    }

    private void CacheFlameAreaReferences()
    {
        if (flameAreaTf == null && flameArea != null)
            flameAreaTf = flameArea.transform;

        if (flameArea == null && flameAreaTf != null)
            flameArea = flameAreaTf.GetComponent<BoxCollider2D>();
    }

    private void ApplyFlameAreaLayer()
    {
        if (flameAreaTf == null)
            return;

        int enemyAttackLayer = LayerMask.NameToLayer(EnemyAttackLayerName);
        if (enemyAttackLayer >= 0 && flameAreaTf.gameObject.layer != enemyAttackLayer)
            flameAreaTf.gameObject.layer = enemyAttackLayer;
    }

    private void UpdateFlameContactDamage()
    {
        if (flameArea == null)
            return;

        currentFlameTargets.Clear();
        Collider2D[] hits = Physics2D.OverlapBoxAll(flameArea.bounds.center, flameArea.bounds.size, 0f, playerLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.GetComponent<PlayerMeleeHitbox>() != null)
                continue;

            Health targetHealth = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth.IsDead)
                continue;

            if (hit.GetComponentInParent<PlayerMovement>() == null && !targetHealth.CompareTag("Player"))
                continue;

            currentFlameTargets.Add(targetHealth);

            if (!flameTargetsInside.Contains(targetHealth))
                TryDamageFlameTarget(targetHealth, hit);
        }

        flameTargetsInside.Clear();
        foreach (Health target in currentFlameTargets)
            flameTargetsInside.Add(target);
    }

    private void TryDamageFlameTarget(Health targetHealth, Collider2D hitCollider)
    {
        if (targetHealth == null || targetHealth.IsDead || targetHealth.IsInvulnerable)
            return;

        if (nextFlameDamageTimes.TryGetValue(targetHealth, out float nextAllowedTime) && Time.time < nextAllowedTime)
            return;

        nextFlameDamageTimes[targetHealth] = Time.time + damageInterval;

        float previousHealth = targetHealth.CurrentHealth;
        Vector3 hitPoint = hitCollider.bounds.ClosestPoint(flameArea.bounds.center);
        if (flameParry != null && flameParry.TryParry(hitCollider, GetFlameKnockbackSource(targetHealth.transform.position), this))
            return;

        targetHealth.TakeDamageAt(damage, hitPoint);

        if (targetHealth.IsDead || targetHealth.CurrentHealth >= previousHealth)
            return;

        ApplyFlameKnockback(targetHealth);
    }

    private void ApplyFlameKnockback(Health targetHealth)
    {
        Vector3 sourcePosition = GetFlameKnockbackSource(targetHealth.transform.position);
        PlayerMovement movement = targetHealth.GetComponent<PlayerMovement>() ?? targetHealth.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            movement.ApplyKnockbackFrom(sourcePosition, flameKnockbackSpeed, flameKnockbackDuration, flameKnockbackUpwardVelocity);
            return;
        }

        Rigidbody2D targetRb = targetHealth.GetComponent<Rigidbody2D>() ?? targetHealth.GetComponentInParent<Rigidbody2D>();
        if (targetRb == null)
            return;

        float knockDirection = targetHealth.transform.position.x >= sourcePosition.x ? 1f : -1f;
        targetRb.velocity = new Vector2(knockDirection * flameKnockbackSpeed, Mathf.Max(targetRb.velocity.y, flameKnockbackUpwardVelocity));
    }

    private Vector3 GetFlameKnockbackSource(Vector3 targetPosition)
    {
        float knockDirection = targetPosition.x >= transform.position.x ? 1f : -1f;
        if (Mathf.Approximately(targetPosition.x, transform.position.x))
            knockDirection = attackDir >= 0 ? 1f : -1f;

        return targetPosition - Vector3.right * knockDirection;
    }

    private void PlayOnce(string stateName)
    {
        if (anim == null || anim.runtimeAnimatorController == null || currentAnim == stateName)
            return;

        currentAnim = stateName;
        anim.Play(stateName);
    }

    public void InterruptForHit()
    {
        // FlameGuy is intentionally stubborn: this method exists for legacy callers,
        // but hit reactions should not knock him back or cancel committed attacks.
    }

    public void OnParried(PlayerParry parry, Vector3 attackerPosition)
    {
        if (state != State.Prep && state != State.Flame)
            return;

        if (flameArea != null)
            flameArea.enabled = false;

        flameTargetsInside.Clear();
        currentFlameTargets.Clear();
        Stop();
        state = State.Recovery;
        timer = recoveryTime;
        PlayOnce("idle");
    }

    public void AlertFromDamage()
    {
        if (!TryResolvePlayer())
            return;

        if (state == State.Prep || state == State.Flame)
            return;

        if (IsPlayerBehind())
            StartDamageChasePrep();
        else
        {
            damageAggroTimer = damageAggroTime;
            StartChase();
        }
    }

    private void HandleDamaged(float remainingHealth)
    {
        if (remainingHealth <= 0f)
            return;

        AlertFromDamage();
    }

    private bool TryResolvePlayer()
    {
        if (!PlayerReference.IsAvailable)
        {
            player = null;
            playerHealth = null;
            return false;
        }

        if (player != PlayerReference.Player)
        {
            player = PlayerReference.Player;
            playerHealth = PlayerReference.Health;
        }

        return player != null && playerHealth != null;
    }

    private void Move(int direction, float speed)
    {
        rb.velocity = new Vector2(Mathf.Sign(direction) * speed, 0f);
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
        float minX = useCombatBounds ? leftX - chaseLeashExtra : leftX;
        float maxX = useCombatBounds ? rightX + chaseLeashExtra : rightX;
        float probeX = transform.position.x + Mathf.Sign(direction) * groundCheckForwardOffset;
        return probeX >= minX && probeX <= maxX;
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

    private bool IsInsidePatrolBounds()
    {
        return transform.position.x >= leftX && transform.position.x <= rightX;
    }

    private void ClampToCombatBounds()
    {
        float clampedX = Mathf.Clamp(transform.position.x, leftX - chaseLeashExtra, rightX + chaseLeashExtra);
        if (Mathf.Approximately(clampedX, transform.position.x))
            return;

        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        Stop();
    }

    private bool IsPlayerBehind()
    {
        if (player == null)
            return false;

        float toPlayerX = player.position.x - transform.position.x;
        if (Mathf.Approximately(toPlayerX, 0f))
            return false;

        return Mathf.Sign(toPlayerX) != dir;
    }

    private void FacePlayerIfAllowed()
    {
        if (player == null)
            return;

        int nextDir = player.position.x >= transform.position.x ? 1 : -1;
        if (nextDir == dir)
            return;

        dir = nextDir;
        ApplyFacing();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        float facing = Application.isPlaying ? dir : 1f;

        float upperZ = facing >= 0f ? upperAngle : 180f - upperAngle;
        float lowerZ = facing >= 0f ? -lowerAngle : 180f + lowerAngle;
        float baseZ = facing >= 0f ? 0f : 180f;

        Vector3 baseDir = Quaternion.Euler(0f, 0f, baseZ) * Vector3.right;
        Vector3 upperDir = Quaternion.Euler(0f, 0f, upperZ) * Vector3.right;
        Vector3 lowerDir = Quaternion.Euler(0f, 0f, lowerZ) * Vector3.right;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + baseDir * viewDistance);
        Gizmos.DrawLine(origin, origin + upperDir * viewDistance);
        Gizmos.DrawLine(origin, origin + lowerDir * viewDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, awarenessRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + baseDir * attackDistance);

        Gizmos.color = Color.green;
        Vector3 groundOrigin = GetGroundCheckOrigin(Application.isPlaying ? dir : 1);
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundCheckDownDistance);
    }
}
