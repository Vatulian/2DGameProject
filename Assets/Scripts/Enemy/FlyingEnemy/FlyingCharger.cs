using UnityEngine;

public class FlyingCharger : MonoBehaviour, IParryReceiver
{
    private enum State
    {
        Patrol,
        Windup,
        Charge,
        Return
    }

    [Header("Refs")]
    [SerializeField] private Transform model;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;
    [SerializeField] private Animator anim;

    [Header("Speeds")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float returnSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private float windupTime = 0.4f;
    [SerializeField] private float maxChargeTime = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private ParryAttackSettings chargeParry = new ParryAttackSettings();

    [Header("Field of View")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 60f;

    private Rigidbody2D rb;
    private State state = State.Patrol;
    private int currentPatrolIndex;
    private Vector3 startPosition;
    private Vector3 storedTargetPos;
    private float stateTimer;
    private Transform player;
    private Health playerHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (!anim)
            anim = GetComponentInChildren<Animator>();

        startPosition = transform.position;
    }

    private void Update()
    {
        TryResolvePlayer();

        switch (state)
        {
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Windup:
                HandleWindup();
                break;
            case State.Charge:
                HandleCharge();
                break;
            case State.Return:
                HandleReturn();
                break;
        }

        if ((state == State.Patrol || state == State.Return) && CanSeePlayer())
            StartWindup();
    }

    private void HandlePatrol()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Transform target = patrolPoints[currentPatrolIndex];
            MoveTowards(target.position, patrolSpeed);

            if (Vector2.Distance(transform.position, target.position) < 0.05f)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void StartWindup()
    {
        if (player == null)
            return;

        storedTargetPos = player.position;
        state = State.Windup;
        stateTimer = windupTime;

        if (rb != null)
            rb.velocity = Vector2.zero;

        FaceTowards(storedTargetPos);
        chargeParry?.PlayCue(this, model != null ? model : transform);
    }

    private void HandleWindup()
    {
        stateTimer -= Time.deltaTime;

        if (rb != null)
            rb.velocity = Vector2.zero;

        FaceTowards(storedTargetPos);

        if (stateTimer <= 0f)
        {
            if (anim != null)
                anim.SetTrigger("AttackTrigger");

            state = State.Charge;
            stateTimer = maxChargeTime;
        }
    }

    private void HandleCharge()
    {
        stateTimer -= Time.deltaTime;

        Vector2 dir = (storedTargetPos - transform.position).normalized;
        MoveTowardsDir(dir, chargeSpeed);

        if (Vector2.Distance(transform.position, storedTargetPos) < 0.1f || stateTimer <= 0f)
            state = State.Return;
    }

    private void HandleReturn()
    {
        Vector3 target = patrolPoints != null && patrolPoints.Length > 0
            ? patrolPoints[0].position
            : startPosition;

        MoveTowards(target, returnSpeed);

        if (Vector2.Distance(transform.position, target) < 0.1f)
            state = State.Patrol;
    }

    private void MoveTowards(Vector3 targetPos, float speed)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        MoveTowardsDir(dir, speed);
        FaceTowards(targetPos);
    }

    private void MoveTowardsDir(Vector2 dir, float speed)
    {
        if (rb != null)
        {
            rb.velocity = dir * speed;
        }
        else
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
        }
    }

    private void FaceTowards(Vector3 targetPos)
    {
        if (model == null)
            return;

        Vector3 dir = targetPos - model.position;
        if (dir.x > 0.01f)
        {
            model.localScale = new Vector3(Mathf.Abs(model.localScale.x), model.localScale.y, model.localScale.z);
        }
        else if (dir.x < -0.01f)
        {
            model.localScale = new Vector3(-Mathf.Abs(model.localScale.x), model.localScale.y, model.localScale.z);
        }
    }

    private bool IsPlayerInvulnerable()
    {
        return playerHealth != null && playerHealth.IsInvulnerable;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        if (IsPlayerInvulnerable())
            return false;

        Vector2 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance)
            return false;

        Vector2 dirToPlayer = toPlayer.normalized;
        Vector2 forward = model != null
            ? new Vector2(Mathf.Sign(model.localScale.x), 0f)
            : Vector2.right;

        if (Vector2.Angle(forward, dirToPlayer) > viewAngle)
            return false;

        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, mask);
        if (hit && !hit.collider.CompareTag("Player"))
            return false;

        return true;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Charge)
            return;

        if (collision.gameObject.CompareTag("Player") && !IsPlayerInvulnerable())
        {
            if (chargeParry == null || !chargeParry.TryParry(collision.collider, transform.position, this))
            {
                Health hp = collision.gameObject.GetComponent<Health>();
                if (hp != null)
                    hp.TakeDamage(damage);
            }
        }

        state = State.Return;
    }

    public void OnParried(PlayerParry parry, Vector3 attackerPosition)
    {
        if (state != State.Charge)
            return;

        if (rb != null)
            rb.velocity = Vector2.zero;

        state = State.Return;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        Gizmos.color = Color.yellow;
        Vector2 forward = Vector2.right;
        if (model != null)
            forward = new Vector2(Mathf.Sign(model.localScale.x), 0f);

        Vector2 leftDir = Quaternion.Euler(0, 0, viewAngle) * forward;
        Vector2 rightDir = Quaternion.Euler(0, 0, -viewAngle) * forward;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(forward * viewDistance));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(leftDir * viewDistance));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(rightDir * viewDistance));
    }
}
