using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BloodKnightAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        Attack,
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

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2.2f;
    [SerializeField] private float attackDistance = 0.9f;
    [SerializeField] private float chaseSpeed = 2.1f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;

    [Header("Attack Flow")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackStateDuration = 0.65f;
    [SerializeField] private float parriedStunTime = 0.7f;

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
    private string currentAnim = "";

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
            case State.Attack:
                TickAttack();
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
            state = State.Chase;
            return;
        }

        if (leftPoint == null || rightPoint == null)
        {
            Stop();
            Play(idleStateName);
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

        Play(runStateName);
        rb.velocity = new Vector2(facing * patrolSpeed, 0f);

        float targetX = facing > 0 ? rightX : leftX;
        bool reachedPoint = (facing > 0 && transform.position.x >= targetX)
            || (facing < 0 && transform.position.x <= targetX);

        if (reachedPoint)
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            waitingAtTurn = true;
            timer = turnPause;
        }
    }

    private void TickChase()
    {
        if (!CanDetectPlayerAhead())
        {
            state = State.Patrol;
            Stop();
            return;
        }

        if (cooldownTimer >= attackCooldown && CanReachPlayerWithAttackRay())
        {
            StartAttack();
            return;
        }

        Play(runStateName);
        rb.velocity = new Vector2(facing * chaseSpeed, 0f);
        ClampToPatrolBounds();
    }

    private void StartAttack()
    {
        if (attack == null)
            return;

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

        if (attack != null && attack.WasParried)
        {
            StartParried();
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        attack?.DisableHitbox();
        state = State.Patrol;
    }

    private void StartParried()
    {
        state = State.Parried;
        timer = parriedStunTime;
        Stop();
        Play(parriedStateName);
    }

    private void TickParried()
    {
        Stop();
        timer -= Time.deltaTime;

        if (timer <= 0f)
            state = State.Patrol;
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

    private void ApplyFacing()
    {
        if (visual == null)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        visual.localScale = scale;

        attack?.SetFacing(facing);
    }

    private void Stop()
    {
        rb.velocity = Vector2.zero;
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

        Gizmos.color = Color.red;
        Gizmos.DrawLine(originTf.position, originTf.position + direction * attackDistance);
    }
}
