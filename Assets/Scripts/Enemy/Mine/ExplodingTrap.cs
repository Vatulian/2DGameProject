using UnityEngine;

public class ExplodingTrap : MonoBehaviour
{
    public enum TrapMode
    {
        Homing,
        DropAlongSight
    }

    private enum State
    {
        Idle,
        Windup,
        Active,
        Exploded
    }

    [Header("Mode")]
    [SerializeField] private TrapMode mode = TrapMode.Homing;

    [Header("Refs")]
    [SerializeField] private Transform model;
    [SerializeField] private Transform visionOrigin;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string explodeTriggerName = "Explode";
    [SerializeField] private float destroyDelayAfterExplosion = 0.5f;

    [Header("Detection (FOV)")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngleLeft = 45f;
    [SerializeField] private float viewAngleRight = 45f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;

    [Header("FOV Orientation")]
    [SerializeField] private Vector2 forwardDirection = Vector2.right;

    [Header("Timing")]
    [SerializeField] private float windupTime = 0.4f;
    [SerializeField] private float activeDuration = 1.5f;

    [Header("Movement")]
    [SerializeField] private float homingSpeed = 8f;
    [SerializeField] private float dropSpeed = 12f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip explosionSfx;

    [Header("Collision")]
    [SerializeField] private LayerMask explodeOnCollisionLayers;

    private State state = State.Idle;
    private float stateTimer;
    private Rigidbody2D rb;
    private Transform player;
    private bool hasExploded;
    private Vector2 dropDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (visionOrigin == null)
            visionOrigin = transform;

        if (animator == null)
        {
            if (model != null)
                animator = model.GetComponent<Animator>();
            else
                animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        TryResolvePlayer();

        if (state == State.Exploded)
            return;

        switch (state)
        {
            case State.Idle:
                if (CanSeePlayer())
                    StartWindup();
                break;
            case State.Windup:
                HandleWindup();
                break;
            case State.Active:
                HandleActive();
                break;
        }
    }

    private void StartWindup()
    {
        if (player == null)
            return;

        if (mode == TrapMode.DropAlongSight)
        {
            Vector2 toPlayer = player.position - transform.position;
            dropDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : GetForward();
        }

        state = State.Windup;
        stateTimer = windupTime;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void HandleWindup()
    {
        stateTimer -= Time.deltaTime;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (stateTimer <= 0f)
        {
            state = State.Active;
            stateTimer = activeDuration;
        }
    }

    private void HandleActive()
    {
        stateTimer -= Time.deltaTime;

        if (mode == TrapMode.Homing)
        {
            if (player != null)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                Move(dir, homingSpeed);
            }
        }
        else
        {
            if (dropDirection.sqrMagnitude < 0.0001f)
                dropDirection = GetForward();

            Move(dropDirection, dropSpeed);
        }

        if (stateTimer <= 0f)
            Explode();
    }

    private void Move(Vector2 dir, float speed)
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

    private Vector2 GetForward()
    {
        return forwardDirection.sqrMagnitude > 0.0001f ? forwardDirection.normalized : Vector2.right;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = visionOrigin.position;
        Vector2 toPlayer = player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance)
            return false;

        Vector2 dirToPlayer = toPlayer.normalized;
        Vector2 forward = GetForward();
        float signedAngle = Vector2.SignedAngle(forward, dirToPlayer);

        if (signedAngle > viewAngleLeft || signedAngle < -viewAngleRight)
            return false;

        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(origin, dirToPlayer, dist, mask);
        if (hit && !hit.collider.CompareTag("Player"))
            return false;

        return true;
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        state = State.Exploded;

        if (rb != null)
            rb.velocity = Vector2.zero;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        if (explosionRadius > 0f && damage > 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);
            foreach (Collider2D c in hits)
            {
                if (!c.CompareTag("Player"))
                    continue;

                Health hp = c.GetComponent<Health>();
                if (hp != null)
                    hp.TakeDamage(damage);
            }
        }

        if (animator != null && !string.IsNullOrEmpty(explodeTriggerName))
            animator.SetTrigger(explodeTriggerName);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (explosionSfx != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(explosionSfx);

        Destroy(gameObject, destroyDelayAfterExplosion);
    }

    private bool TryResolvePlayer()
    {
        if (!PlayerReference.IsAvailable)
        {
            player = null;
            return false;
        }

        if (player != PlayerReference.Player)
            player = PlayerReference.Player;

        return player != null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded)
            return;

        if (collision.collider.CompareTag("Player"))
        {
            Explode();
            return;
        }

        if (((1 << collision.gameObject.layer) & explodeOnCollisionLayers) != 0)
            Explode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = visionOrigin != null ? visionOrigin.position : transform.position;
        Vector2 forward = forwardDirection.sqrMagnitude > 0.0001f ? forwardDirection.normalized : Vector2.right;
        Vector2 leftDir = Quaternion.Euler(0, 0, viewAngleLeft) * forward;
        Vector2 rightDir = Quaternion.Euler(0, 0, -viewAngleRight) * forward;

        Gizmos.DrawLine(origin, origin + (Vector3)(leftDir * viewDistance));
        Gizmos.DrawLine(origin, origin + (Vector3)(rightDir * viewDistance));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
