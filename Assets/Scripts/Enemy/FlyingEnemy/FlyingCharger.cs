using UnityEngine;

public class FlyingCharger : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Windup,
        Charge,
        Return
    }

    [Header("Refs")]
    [SerializeField] private Transform model;              // Sprite/anim child (flip için)
    [SerializeField] private Transform[] patrolPoints;     // Boşsa sabit durur
    [SerializeField] private LayerMask playerLayer;        // Player layer
    [SerializeField] private LayerMask obstructionLayers;  // Ground/Wall gibi engeller
    [SerializeField] private Animator anim;                // <-- YENİ: Animator (Sprite child)

    [Header("Speeds")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float returnSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private float windupTime = 0.4f;  // Charge öncesi bekleme
    [SerializeField] private float maxChargeTime = 1.2f;
    [SerializeField] private int damage = 1;

    [Header("Field of View")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 60f; // yarım açı (sağa/sola)

    private Rigidbody2D rb;
    private State state = State.Patrol;

    private int currentPatrolIndex;
    private Vector3 startPosition;      // Patrol yoksa buraya döner
    private Vector3 storedTargetPos;    // Player’ı gördüğü ilk nokta
    private float stateTimer;

    private Transform player;
    private Health playerHealth;

    #region Unity

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Animator otomatik bul (Sprite child üzerinde)
        if (!anim)
            anim = GetComponentInChildren<Animator>();

        startPosition = transform.position;

        // Player referansı
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void Update()
    {
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

        // Patrol veya Return halindeyken, oyuncuyu görürse saldırı döngüsüne gir
        if ((state == State.Patrol || state == State.Return) && CanSeePlayer())
        {
            StartWindup();
        }
    }

    #endregion

    #region State Handlers

    private void HandlePatrol()
    {
        if (rb != null) rb.velocity = Vector2.zero;

        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Transform target = patrolPoints[currentPatrolIndex];
            MoveTowards(target.position, patrolSpeed);

            if (Vector2.Distance(transform.position, target.position) < 0.05f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            // Patrol yoksa olduğu yerde durur
        }
    }

    private void StartWindup()
    {
        if (player == null) return;

        storedTargetPos = player.position;
        state = State.Windup;
        stateTimer = windupTime;

        if (rb != null) rb.velocity = Vector2.zero;

        FaceTowards(storedTargetPos);
    }

    private void HandleWindup()
    {
        stateTimer -= Time.deltaTime;

        if (rb != null) rb.velocity = Vector2.zero;

        FaceTowards(storedTargetPos);

        if (stateTimer <= 0f)
        {
            // === BURASI ÖNEMLİ ===
            // Charge başlamadan hemen önce Attack animasyonunu tetikliyoruz
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

        // Hedefe yaklaştıysa veya süresi bittiyse geri dön
        if (Vector2.Distance(transform.position, storedTargetPos) < 0.1f || stateTimer <= 0f)
        {
            state = State.Return;
        }
    }

    private void HandleReturn()
    {
        Vector3 target;

        if (patrolPoints != null && patrolPoints.Length > 0)
            target = patrolPoints[0].position;
        else
            target = startPosition;

        MoveTowards(target, returnSpeed);

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            state = State.Patrol;
        }
    }

    #endregion

    #region Movement Helpers

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
        if (model == null) return;

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

    #endregion

    #region Detection & Damage

    private bool IsPlayerInvulnerable()
    {
        if (playerHealth == null) return false;
        return playerHealth.IsInvulnerable;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        // i-frame sırasında oyuncuyu hiç görmesin
        if (IsPlayerInvulnerable())
            return false;

        Vector2 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        Vector2 dirToPlayer = toPlayer.normalized;

        // Kuşun baktığı yön
        Vector2 forward = model != null
            ? new Vector2(Mathf.Sign(model.localScale.x), 0f)
            : Vector2.right;

        float angle = Vector2.Angle(forward, dirToPlayer);
        if (angle > viewAngle) return false;

        // Araya ground / wall giriyorsa görmesin
        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, mask);
        if (hit)
        {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }

        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Charge) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (!IsPlayerInvulnerable())
            {
                Health hp = collision.gameObject.GetComponent<Health>();
                if (hp != null)
                    hp.TakeDamage(damage);
            }
        }

        // Her türlü geri dönsün
        state = State.Return;
    }

    #endregion

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
