using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class KamikazeEnemy : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Windup,
        Dive,
        Exploding
    }

    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int FlyStateHash = Animator.StringToHash("Fly");
    private static readonly int DiveStateHash = Animator.StringToHash("Dive");
    private static readonly int ExplodeStateHash = Animator.StringToHash("Explode");

    [Header("References")]
    [SerializeField] private Transform model;
    [SerializeField] private Transform visionOrigin;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;
    [SerializeField] private EnemyEssenceDropper essenceDropper;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0.01f)] private float patrolPointReachDistance = 0.08f;
    [SerializeField, Min(0f)] private float waitAtPatrolPoint = 0.25f;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;

    [Header("Dive")]
    [SerializeField, Min(0f)] private float windupTime = 0.35f;
    [SerializeField, Min(0f)] private float diveSpeed = 12f;
    [SerializeField, Min(0f)] private float minDiveHorizontalDistance = 0.75f;
    [SerializeField, Min(0f)] private float maxDiveHorizontalDistance = 5.5f;
    [SerializeField, Min(0f)] private float minDiveVerticalDrop = 0.5f;
    [SerializeField, Min(0f)] private float maxDiveVerticalDrop = 5.5f;
    [SerializeField, Min(0f)] private float diveLineTolerance = 1.25f;
    [SerializeField] private LayerMask impactLayers;

    [Header("Explosion")]
    [SerializeField, Min(0f)] private float explosionRadius = 1.25f;
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float destroyDelay = 0.7f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip explosionSfx;

    private Rigidbody2D rb;
    private Transform player;
    private Health playerHealth;
    private State state = State.Patrol;
    private Vector2 diveDirection;
    private int patrolIndex;
    private float windupTimer;
    private float patrolWaitTimer;

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

        if (health == null)
            health = GetComponent<Health>();

        if (essenceDropper == null)
            essenceDropper = GetComponent<EnemyEssenceDropper>();

        SelectNearestPatrolPoint();
        PlayAnimation(HasPatrolRoute() ? FlyStateHash : IdleStateHash);
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void Update()
    {
        if (state == State.Exploding)
            return;

        ResolvePlayer();

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;

            case State.Windup:
                UpdateWindup();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (state == State.Exploding)
            return;

        switch (state)
        {
            case State.Patrol:
                HandlePatrolMovement();
                break;

            case State.Windup:
                rb.velocity = Vector2.zero;
                break;

            case State.Dive:
                rb.velocity = diveDirection * diveSpeed;
                break;
        }
    }

    private void UpdatePatrol()
    {
        patrolWaitTimer -= Time.deltaTime;

        if (CanAcquireDiveTarget())
            StartWindup();
    }

    private void UpdateWindup()
    {
        if (!CanAcquireDiveTarget())
        {
            CancelWindup();
            return;
        }

        UpdateDiveDirection();
        windupTimer -= Time.deltaTime;

        if (windupTimer <= 0f)
            StartDive();
    }

    private void HandlePatrolMovement()
    {
        if (!HasPatrolRoute() || patrolWaitTimer > 0f)
        {
            rb.velocity = Vector2.zero;
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
            rb.velocity = Vector2.zero;
            patrolWaitTimer = waitAtPatrolPoint;
            AdvancePatrolPoint();
            return;
        }

        Vector2 direction = toTarget.normalized;
        rb.velocity = direction * patrolSpeed;
        FaceDirection(direction);
    }

    private void StartWindup()
    {
        UpdateDiveDirection();

        state = State.Windup;
        windupTimer = windupTime;
        rb.velocity = Vector2.zero;

        if (windupTime <= 0f)
            StartDive();
        else
            PlayAnimation(IdleStateHash);
    }

    private void CancelWindup()
    {
        state = State.Patrol;
        rb.velocity = Vector2.zero;
        PlayAnimation(HasPatrolRoute() ? FlyStateHash : IdleStateHash);
    }

    private void UpdateDiveDirection()
    {
        if (player == null)
            return;

        float horizontalDirection = Mathf.Sign(player.position.x - transform.position.x);
        if (Mathf.Approximately(horizontalDirection, 0f))
            horizontalDirection = GetForwardDirection().x;

        diveDirection = new Vector2(horizontalDirection, -1f).normalized;
        FaceDirection(diveDirection);
    }

    private void StartDive()
    {
        state = State.Dive;
        PlayAnimation(DiveStateHash);
    }

    private void Explode(bool damagePlayer)
    {
        if (state == State.Exploding)
            return;

        state = State.Exploding;
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (damagePlayer)
            DamagePlayersInRadius();

        if (essenceDropper != null)
            essenceDropper.DropEssence();

        PlayAnimation(ExplodeStateHash);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (explosionSfx != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(explosionSfx);

        Destroy(gameObject, destroyDelay);
    }

    private void DamagePlayersInRadius()
    {
        if (damage <= 0 || explosionRadius <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            Health targetHealth = hit.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth != playerHealth)
                continue;

            targetHealth.TakeDamage(damage, transform.position);
            return;
        }
    }

    private bool HasClearLineOfSight()
    {
        if (player == null || playerHealth == null || playerHealth.IsInvulnerable)
            return false;

        Vector2 origin = visionOrigin.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float distance = toPlayer.magnitude;
        if (distance <= 0.001f)
            return false;

        Vector2 direction = toPlayer / distance;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, obstructionLayers | playerLayer);
        if (!hit)
            return true;

        return hit.collider.GetComponentInParent<Health>() == playerHealth;
    }

    private bool CanAcquireDiveTarget()
    {
        return HasValidPlayerTarget() && IsPlayerInDiveRange() && HasClearLineOfSight();
    }

    private bool IsPlayerInDiveRange()
    {
        if (!HasValidPlayerTarget())
            return false;

        Vector2 offset = (Vector2)player.position - (Vector2)visionOrigin.position;
        float horizontalDistance = Mathf.Abs(offset.x);
        float verticalDrop = -offset.y;

        if (horizontalDistance < minDiveHorizontalDistance
            || horizontalDistance > maxDiveHorizontalDistance
            || verticalDrop < minDiveVerticalDrop
            || verticalDrop > maxDiveVerticalDrop)
        {
            return false;
        }

        return Mathf.Abs(horizontalDistance - verticalDrop) <= diveLineTolerance;
    }

    private bool HasValidPlayerTarget()
    {
        return player != null
               && playerHealth != null
               && !playerHealth.IsDead
               && visionOrigin != null;
    }

    private Vector2 GetForwardDirection()
    {
        float direction = model != null && model.localScale.x < 0f ? -1f : 1f;
        return Vector2.right * direction;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (model == null || Mathf.Abs(direction.x) <= 0.01f)
            return;

        Vector3 scale = model.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
        model.localScale = scale;
    }

    private void AdvancePatrolPoint()
    {
        if (!HasPatrolRoute())
            return;

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void SelectNearestPatrolPoint()
    {
        if (!HasPatrolRoute())
            return;

        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float distance = ((Vector2)patrolPoints[i].position - (Vector2)transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                patrolIndex = i;
            }
        }
    }

    private bool HasPatrolRoute()
    {
        return patrolPoints != null && patrolPoints.Length > 0;
    }

    private void PlayAnimation(int stateHash)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.Play(stateHash, 0, 0f);
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

    private void HandleDeath()
    {
        Explode(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Dive)
            return;

        bool hitPlayer = playerHealth != null
                         && collision.collider.GetComponentInParent<Health>() == playerHealth;
        bool hitImpactLayer = ((1 << collision.gameObject.layer) & impactLayers) != 0;

        if (hitPlayer || hitImpactLayer)
            Explode(true);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = visionOrigin != null ? visionOrigin.position : transform.position;

        Gizmos.color = Color.cyan;
        DrawDiveRangeGizmos(origin);

        if (patrolPoints != null)
        {
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, patrolPointReachDistance);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    private void DrawDiveRangeGizmos(Vector3 origin)
    {
        float minDistance = Mathf.Max(minDiveHorizontalDistance, minDiveVerticalDrop);
        float maxDistance = Mathf.Min(maxDiveHorizontalDistance, maxDiveVerticalDrop);
        Vector3 leftStart = origin + new Vector3(-minDistance, -minDistance);
        Vector3 leftEnd = origin + new Vector3(-maxDistance, -maxDistance);
        Vector3 rightStart = origin + new Vector3(minDistance, -minDistance);
        Vector3 rightEnd = origin + new Vector3(maxDistance, -maxDistance);

        Gizmos.DrawLine(leftStart, leftEnd);
        Gizmos.DrawLine(rightStart, rightEnd);
    }
}
