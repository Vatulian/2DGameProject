using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EvilCrowAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Patrol,
        Attacking,
        Dead
    }

    private static readonly int FlyStateHash = Animator.StringToHash("Fly");
    private static readonly int DeathStateHash = Animator.StringToHash("Death");

    [Header("References")]
    [SerializeField] private Transform model;
    [SerializeField] private Transform visionOrigin;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Animator animator;
    [SerializeField] private EvilCrowRangedAttack rangedAttack;
    [SerializeField] private Health health;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0.01f)] private float patrolPointReachDistance = 0.1f;
    [SerializeField, Min(0f)] private float idleAtPatrolPoint = 0.75f;

    [Header("Cone Detection")]
    [SerializeField, Min(0f)] private float viewDistance = 7f;
    [SerializeField, Range(0f, 180f)] private float viewAngle = 70f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;

    [Header("Death")]
    [SerializeField, Min(0f)] private float destroyDelay = 1.7f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody2D rb;
    private Transform player;
    private Health playerHealth;
    private State state;
    private int patrolIndex;
    private float idleTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (model == null)
            model = transform;

        if (visionOrigin == null)
            visionOrigin = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rangedAttack == null)
            rangedAttack = GetComponent<EvilCrowRangedAttack>();

        if (health == null)
            health = GetComponent<Health>();

        SelectNearestPatrolPoint();
        state = HasPatrolRoute() ? State.Patrol : State.Idle;
        PlayLocomotionAnimation();
    }

    private void OnEnable()
    {
        if (rangedAttack != null)
            rangedAttack.AttackFinished += HandleAttackFinished;

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (rangedAttack != null)
            rangedAttack.AttackFinished -= HandleAttackFinished;

        if (health != null)
            health.OnDeath -= HandleDeath;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void Update()
    {
        if (state == State.Dead)
            return;

        ResolvePlayer();

        if (state == State.Attacking)
        {
            StopMovement();
            FaceTarget(player);
            return;
        }

        if (CanSeePlayer() && rangedAttack != null && rangedAttack.TryStartAttack(player))
        {
            state = State.Attacking;
            StopMovement();
            FaceTarget(player);
            return;
        }

        if (state == State.Idle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f && HasPatrolRoute())
            {
                state = State.Patrol;
                PlayLocomotionAnimation();
            }
        }
    }

    private void FixedUpdate()
    {
        if (state != State.Patrol)
        {
            StopMovement();
            return;
        }

        MoveAlongPatrol();
    }

    private void MoveAlongPatrol()
    {
        if (!HasPatrolRoute())
        {
            EnterIdle(0f);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            AdvancePatrolPoint();
            return;
        }

        Vector2 toTarget = (Vector2)target.position - rb.position;
        if (toTarget.magnitude <= patrolPointReachDistance)
        {
            AdvancePatrolPoint();
            EnterIdle(idleAtPatrolPoint);
            return;
        }

        Vector2 direction = toTarget.normalized;
        rb.velocity = direction * patrolSpeed;
        FaceDirection(direction.x);
    }

    private bool CanSeePlayer()
    {
        if (player == null || playerHealth == null || playerHealth.IsDead || playerHealth.IsInvulnerable)
            return false;

        Vector2 origin = visionOrigin.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float distance = toPlayer.magnitude;
        if (distance <= 0.001f || distance > viewDistance)
            return false;

        Vector2 direction = toPlayer / distance;
        if (Vector2.Angle(GetForwardDirection(), direction) > viewAngle * 0.5f)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, playerLayer | obstructionLayers);
        if (!hit)
            return false;

        return hit.collider.GetComponentInParent<Health>() == playerHealth;
    }

    private void ResolvePlayer()
    {
        if (!PlayerReference.IsAvailable)
        {
            player = null;
            playerHealth = null;
            return;
        }

        player = PlayerReference.Player;
        playerHealth = PlayerReference.Health;
    }

    private void HandleAttackFinished()
    {
        if (state == State.Dead)
            return;

        EnterIdle(idleAtPatrolPoint);
    }

    private void HandleDeath()
    {
        state = State.Dead;
        StopMovement();

        if (rangedAttack != null)
            rangedAttack.CancelAttack();

        if (animator != null)
            animator.Play(DeathStateHash, 0, 0f);

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        GameObject destroyTarget = transform.parent != null
            ? transform.parent.gameObject
            : gameObject;
        Destroy(destroyTarget, destroyDelay);
    }

    private void EnterIdle(float duration)
    {
        state = State.Idle;
        idleTimer = duration;
        StopMovement();
        PlayLocomotionAnimation();
    }

    private void PlayLocomotionAnimation()
    {
        if (animator != null)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash == FlyStateHash)
                return;

            animator.Play(FlyStateHash, 0, 0f);
        }
    }

    private void StopMovement()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void FaceTarget(Transform target)
    {
        if (target != null)
            FaceDirection(target.position.x - transform.position.x);
    }

    private void FaceDirection(float horizontalDirection)
    {
        if (model == null || Mathf.Abs(horizontalDirection) <= 0.001f)
            return;

        Vector3 scale = model.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontalDirection);
        model.localScale = scale;
    }

    private Vector2 GetForwardDirection()
    {
        float direction = model != null ? Mathf.Sign(model.localScale.x) : 1f;
        return direction >= 0f ? Vector2.right : Vector2.left;
    }

    private bool HasPatrolRoute()
    {
        return patrolPoints != null && patrolPoints.Length > 0;
    }

    private void AdvancePatrolPoint()
    {
        if (HasPatrolRoute())
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void SelectNearestPatrolPoint()
    {
        if (!HasPatrolRoute())
            return;

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float distance = ((Vector2)patrolPoints[i].position - (Vector2)transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                patrolIndex = i;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Transform originTransform = visionOrigin != null ? visionOrigin : transform;
        Vector2 origin = originTransform.position;
        Vector2 forward = Application.isPlaying ? GetForwardDirection() : GetEditorForwardDirection();
        Vector2 upperEdge = Quaternion.Euler(0f, 0f, viewAngle * 0.5f) * forward;
        Vector2 lowerEdge = Quaternion.Euler(0f, 0f, -viewAngle * 0.5f) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + upperEdge * viewDistance);
        Gizmos.DrawLine(origin, origin + lowerEdge * viewDistance);

        if (patrolPoints == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in patrolPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, patrolPointReachDistance);
        }
    }

    private Vector2 GetEditorForwardDirection()
    {
        float direction = model != null ? Mathf.Sign(model.localScale.x) : 1f;
        return direction >= 0f ? Vector2.right : Vector2.left;
    }
}
