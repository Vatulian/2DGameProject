using UnityEngine;

public class FlameEnemyAI : MonoBehaviour
{
    private enum State { Patrol, WaitAtEnd, Prep, Flame }

    [Header("Refs")]
    [SerializeField] private Transform sprite;           // Animator+Renderer child
    [SerializeField] private Animator anim;             // Animator on the sprite child
    [SerializeField] private Transform flameAreaTf;     // FlameArea transform under ROOT
    [SerializeField] private BoxCollider2D flameArea;   // FlameArea collider (isTrigger)
    [SerializeField] private Transform leftPoint;       // World patrol point (not a child)
    [SerializeField] private Transform rightPoint;      // World patrol point (not a child)

    [Header("Detection (Cone)")]
    [SerializeField] private float viewDistance = 3.6f;

    [Tooltip("Degrees ABOVE the horizontal baseline (0°).")]
    [SerializeField] private float upperAngle = 70f;

    [Tooltip("Degrees BELOW the horizontal baseline (0°).")]
    [SerializeField] private float lowerAngle = 0f;

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayers;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float endIdleTime = 1f;

    [Header("Attack")]
    [SerializeField] private float prepTime = 0.5f;
    [SerializeField] private float flameDuration = 1.2f;
    [SerializeField] private float damageInterval = 0.25f;
    [SerializeField] private int damage = 1;

    [Tooltip("If true, once Prep starts the enemy will flame even if the player leaves the cone.")]
    [SerializeField] private bool commitAttack = true;

    [Header("Attack Facing")]
    [Tooltip("If true, once Prep starts the enemy will NOT turn until Flame ends.")]
    [SerializeField] private bool lockFacingDuringAttack = true;

    private Rigidbody2D rb;
    private Transform player;
    private Health playerHealth;

    private State state = State.Patrol;
    private int dir = 1; // 1 right, -1 left
    private float timer = 0f;
    private float damageTimer = 0f;

    // When locking facing, we store the facing direction at the moment attack starts
    private int attackDir = 1;

    // FlameArea symmetric placement reference (place it on the RIGHT in the editor)
    private Vector2 flameBaseLocalPos;
    private float flameAbsX;
    private float flameBaseY;

    private string currentAnim = "";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!anim)
            anim = sprite ? sprite.GetComponent<Animator>() : GetComponentInChildren<Animator>();

        if (!flameAreaTf && flameArea) flameAreaTf = flameArea.transform;
        if (!flameArea && flameAreaTf) flameArea = flameAreaTf.GetComponent<BoxCollider2D>();

        if (flameArea) flameArea.enabled = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }

        if (flameAreaTf)
        {
            flameBaseLocalPos = flameAreaTf.localPosition;
            flameAbsX = Mathf.Abs(flameBaseLocalPos.x);
            flameBaseY = flameBaseLocalPos.y;

            // We move the transform, so collider offset should be zero
            if (flameArea != null)
                flameArea.offset = Vector2.zero;
        }

        ApplyFacing();
    }

    private void Update()
    {
        if (player == null || leftPoint == null || rightPoint == null) return;

        switch (state)
        {
            case State.Patrol:    TickPatrol(); break;
            case State.WaitAtEnd: TickWait();   break;
            case State.Prep:      TickPrep();   break;
            case State.Flame:     TickFlame();  break;
        }
    }

    // ---------------- PATROL
    private void TickPatrol()
    {
        if (CanSeePlayerCone())
        {
            StartPrep();
            return;
        }

        PlayOnce("run");

        float targetX = (dir == 1) ? rightPoint.position.x : leftPoint.position.x;

        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
        ApplyFacing();

        if ((dir == 1 && transform.position.x >= targetX) ||
            (dir == -1 && transform.position.x <= targetX))
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

            state = State.WaitAtEnd;
            timer = endIdleTime;

            PlayOnce("idle");
        }
    }

    private void TickWait()
    {
        if (CanSeePlayerCone())
        {
            StartPrep();
            return;
        }

        rb.velocity = new Vector2(0f, rb.velocity.y);
        PlayOnce("idle");

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            dir *= -1;
            state = State.Patrol;
        }
    }

    // ---------------- PREP
    private void StartPrep()
    {
        state = State.Prep;
        timer = prepTime;

        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Decide attack direction once when attack begins
        attackDir = (player.position.x >= transform.position.x) ? 1 : -1;
        if (lockFacingDuringAttack) dir = attackDir;

        ApplyFacing();
        PlayOnce("prep attack");
    }

    private void TickPrep()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // While attacking, keep the same facing direction (no turning)
        if (!lockFacingDuringAttack)
        {
            // If not locking, still face player during prep
            dir = (player.position.x >= transform.position.x) ? 1 : -1;
            ApplyFacing();
        }

        // If not committing, cancel when the player leaves the cone
        if (!commitAttack && !CanSeePlayerCone())
        {
            state = State.Patrol;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            StartFlame();
    }

    // ---------------- FLAME
    private void StartFlame()
    {
        state = State.Flame;
        timer = flameDuration;
        damageTimer = 0f;

        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Ensure we start flame with the locked direction
        if (lockFacingDuringAttack) dir = attackDir;
        ApplyFacing();

        PlayOnce("flame");
        if (flameArea) flameArea.enabled = true;
    }

    private void TickFlame()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Do NOT turn during flame if locked
        if (!lockFacingDuringAttack)
        {
            dir = (player.position.x >= transform.position.x) ? 1 : -1;
            ApplyFacing();
        }
        else
        {
            // Keep consistent facing (optional safety)
            dir = attackDir;
            ApplyFacing();
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (flameArea) flameArea.enabled = false;

            // Attack ended; after this, patrol logic will manage direction again
            state = State.Patrol;
            return;
        }

        if (playerHealth == null) return;

        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;

            Collider2D hit = Physics2D.OverlapBox(
                flameArea.bounds.center,
                flameArea.bounds.size,
                0f,
                playerLayer
            );

            if (hit != null && hit.CompareTag("Player"))
                playerHealth.TakeDamage(damage);
        }
    }

    // ---------------- Detection: ONE cone with independent upper/lower angles around horizontal baseline
    private bool CanSeePlayerCone()
    {
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        Vector2 dirToPlayer = toPlayer / dist;

        // Absolute angle in degrees [0..360)
        float a = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        if (a < 0f) a += 360f;

        // Facing RIGHT baseline = 0°
        // Sector: [360-lowerAngle .. 360) U [0 .. upperAngle]
        //
        // Facing LEFT baseline = 180°
        // Sector: [180-upperAngle .. 180+lowerAngle]
        bool inCone;
        float up = Mathf.Clamp(upperAngle, 0f, 179f);
        float down = Mathf.Clamp(lowerAngle, 0f, 179f);

        // Use current facing direction (dir)
        if (dir >= 0)
        {
            float minA = 360f - down;
            float maxA = up;
            inCone = (a >= minA) || (a <= maxA);
        }
        else
        {
            float minA = 180f - up;
            float maxA = 180f + down;
            inCone = (a >= minA) && (a <= maxA);
        }

        if (!inCone) return false;

        if (!useLineOfSight) return true;

        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, mask);
        if (hit && !hit.collider.CompareTag("Player"))
            return false;

        return true;
    }

    // ---------------- Facing / FlameArea mirroring
    private void ApplyFacing()
    {
        // Flip sprite visually
        if (sprite)
        {
            Vector3 s = sprite.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            sprite.localScale = s;
        }

        // Keep FlameArea under ROOT, but mirror it symmetrically
        if (flameAreaTf)
        {
            flameAreaTf.localPosition = new Vector3(flameAbsX * dir, flameBaseY, flameAreaTf.localPosition.z);
        }
    }

    private void PlayOnce(string stateName)
    {
        if (!anim) return;
        if (currentAnim == stateName) return;
        currentAnim = stateName;
        anim.Play(stateName);
    }

    // Called externally when enemy takes a hit (to cancel attack/flame)
    public void InterruptForHit()
    {
        if (flameArea) flameArea.enabled = false;
        state = State.Patrol;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;
        float facing = Application.isPlaying ? dir : 1f;

        // Draw cone boundaries using the SAME rules as CanSeePlayerCone()
        float upperZ = (facing >= 0f) ? +upperAngle : (180f - upperAngle);
        float lowerZ = (facing >= 0f) ? -lowerAngle : (180f + lowerAngle);
        float baseZ  = (facing >= 0f) ? 0f : 180f;

        Vector3 baseDir  = Quaternion.Euler(0, 0, baseZ)  * Vector3.right;
        Vector3 upperDir = Quaternion.Euler(0, 0, upperZ) * Vector3.right;
        Vector3 lowerDir = Quaternion.Euler(0, 0, lowerZ) * Vector3.right;

        Gizmos.DrawLine(origin, origin + baseDir * viewDistance);
        Gizmos.DrawLine(origin, origin + upperDir * viewDistance);
        Gizmos.DrawLine(origin, origin + lowerDir * viewDistance);
    }
}
