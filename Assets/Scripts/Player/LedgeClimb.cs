using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[DefaultExecutionOrder(10)]
public class LedgeClimb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private Health health;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform visualRoot;

    [Header("Detection")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private float lowerRayHeight = -0.35f;
    [SerializeField] private float upperRayHeight = 0.3f;
    [SerializeField] private float rayDistance = 0.6f;
    [SerializeField] private float surfaceProbeTopPadding = 0.25f;
    [SerializeField] private float surfaceProbeBelowWallHit = 0.15f;
    [SerializeField] private float surfaceProbeInset = 0.35f;

    [Header("Climb")]
    [SerializeField] private float grabDuration = 0.32f;
    [SerializeField] private float climbDuration = 0.5f;
    [SerializeField, Range(0.5f, 3f)] private float climbAnimationSpeed = 1.6f;
    [Tooltip("Player pivotundan sprite uzerindeki tutunma/kesik noktasina olan mesafe. Bu nokta ledge kosesine oturtulur.")]
    [SerializeField] private Vector2 grabCornerAnchorOffset = new Vector2(0.27f, 0.26f);
    [Tooltip("Climb animasyonundaki koseye degmesi gereken noktanin player pivotuna gore mesafesi.")]
    [SerializeField] private Vector2 climbCornerAnchorOffset = new Vector2(0.22f, 0.26f);
    [SerializeField] private float horizontalClearance = 0.08f;
    [SerializeField] private float verticalClearance = 0.02f;
    [SerializeField] private float maxUpwardSpeedForGrab = 1f;
    [SerializeField] private bool allowWhileSliding = true;

    [Header("Animation State Names")]
    [SerializeField] private string grabStateName = "Swordmaster_LedgeClimb_Ledge_Grab";
    [SerializeField] private string climbStateName = "Swordmaster_LedgeClimb_Ledge_Climb";

    private Rigidbody2D rb;
    private PlayerAttack rangedAttack;
    private PlayerMeleeAttack meleeAttack;
    private PlayerParry parry;
    private Coroutine climbRoutine;
    private float gravityBeforeClimb;
    private bool hasLastLedgeDebug;
    private Vector2 lastLowerRayStart;
    private Vector2 lastLowerRayEnd;
    private Vector2 lastUpperRayStart;
    private Vector2 lastUpperRayEnd;
    private Vector2 lastSurfaceRayStart;
    private Vector2 lastSurfaceRayEnd;
    private Vector2 lastCornerPoint;

    public bool IsClimbing { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!playerMovement)
            playerMovement = GetComponent<PlayerMovement>();

        if (!animationController)
            animationController = GetComponentInChildren<PlayerAnimationController>();

        if (!health)
            health = GetComponent<Health>();

        if (!bodyCollider)
            bodyCollider = GetComponent<Collider2D>();

        if (!visualRoot)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.transform != transform)
                visualRoot = spriteRenderer.transform;
        }

        rangedAttack = GetComponent<PlayerAttack>();
        meleeAttack = GetComponent<PlayerMeleeAttack>();
        parry = GetComponent<PlayerParry>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        ResetVisualOffset();

        if (health == null)
            return;

        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (IsClimbing || !CanAttemptClimb())
            return;

        TryStartClimb();
    }

    public void CancelClimb(bool returnToLocomotion = true)
    {
        if (climbRoutine != null)
        {
            StopCoroutine(climbRoutine);
            climbRoutine = null;
        }

        if (!IsClimbing)
            return;

        IsClimbing = false;
        ResetVisualOffset();
        playerMovement?.SetGravityScale(gravityBeforeClimb);
        if (returnToLocomotion)
            animationController?.ReturnToLocomotionState();
    }

    private bool CanAttemptClimb()
    {
        if (playerMovement == null || rb == null)
            return false;

        if (health != null && health.IsDead)
            return false;

        if (playerMovement.IsDashing || playerMovement.IsWallJumping)
            return false;

        if (playerMovement.IsLedgeGrabBlocked)
            return false;

        if ((rangedAttack != null && rangedAttack.IsAttacking)
            || (meleeAttack != null && meleeAttack.IsAttacking)
            || (parry != null && parry.IsParryActive))
        {
            return false;
        }

        if (!allowWhileSliding && playerMovement.IsSliding)
            return false;

        if (playerMovement.LastOnGroundTime > 0f)
            return false;

        if (rb.velocity.y > maxUpwardSpeedForGrab)
            return false;

        return true;
    }

    private void TryStartClimb()
    {
        float dirSign = playerMovement.IsFacingRight ? 1f : -1f;
        Vector2 direction = playerMovement.IsFacingRight ? Vector2.right : Vector2.left;
        Vector2 origin = transform.position;

        Vector2 lowerOrigin = origin + Vector2.up * lowerRayHeight;
        Vector2 upperOrigin = origin + Vector2.up * upperRayHeight;

        RaycastHit2D lowerHit = CastLedgeRay(lowerOrigin, direction, rayDistance);
        bool upperBlocked = CastLedgeRay(upperOrigin, direction, rayDistance).collider != null;

        if (lowerHit.collider == null || upperBlocked)
            return;

        if (!IsMovingTowardLedge(dirSign))
            return;

        if (!TryFindSurfacePoint(lowerHit.point, dirSign, out Vector2 surfacePoint))
            return;

        StoreLedgeDebug(lowerOrigin, upperOrigin, direction, lowerHit.point, surfacePoint);
        climbRoutine = StartCoroutine(ClimbRoutine(lowerHit.point, surfacePoint, dirSign));
    }

    private bool IsMovingTowardLedge(float dirSign)
    {
        if (Mathf.Abs(rb.velocity.x) <= 0.05f)
            return true;

        return Mathf.Sign(rb.velocity.x) == dirSign;
    }

    private bool TryFindSurfacePoint(Vector2 wallHitPoint, float dirSign, out Vector2 surfacePoint)
    {
        GetSurfaceProbeLine(wallHitPoint, dirSign, out Vector2 probeStart, out Vector2 probeEnd);

        RaycastHit2D surfaceHit = CastSurfaceProbe(probeStart, probeEnd);
        if (surfaceHit.collider == null || surfaceHit.normal.y < 0.5f)
        {
            surfacePoint = default;
            return false;
        }

        surfacePoint = surfaceHit.point;
        return true;
    }

    private void StoreLedgeDebug(Vector2 lowerOrigin, Vector2 upperOrigin, Vector2 direction, Vector2 wallHitPoint, Vector2 surfacePoint)
    {
        hasLastLedgeDebug = true;
        lastLowerRayStart = lowerOrigin;
        lastLowerRayEnd = lowerOrigin + direction * rayDistance;
        lastUpperRayStart = upperOrigin;
        lastUpperRayEnd = upperOrigin + direction * rayDistance;

        GetSurfaceProbeLine(wallHitPoint, Mathf.Sign(direction.x), out lastSurfaceRayStart, out lastSurfaceRayEnd);
        lastCornerPoint = new Vector2(wallHitPoint.x, surfacePoint.y);
    }

    private void GetSurfaceProbeLine(Vector2 wallHitPoint, float dirSign, out Vector2 probeStart, out Vector2 probeEnd)
    {
        Bounds bounds = GetBodyBounds();
        float startY = Mathf.Max(bounds.max.y + surfaceProbeTopPadding, wallHitPoint.y + surfaceProbeTopPadding);
        float endY = wallHitPoint.y - surfaceProbeBelowWallHit;

        if (endY >= startY)
            endY = startY - 0.05f;

        float probeX = wallHitPoint.x + surfaceProbeInset * dirSign;
        probeStart = new Vector2(probeX, startY);
        probeEnd = new Vector2(probeX, endY);
    }

    private RaycastHit2D CastSurfaceProbe(Vector2 probeStart, Vector2 probeEnd)
    {
        return CastLedgeRay(probeStart, Vector2.down, Vector2.Distance(probeStart, probeEnd));
    }

    private RaycastHit2D CastLedgeRay(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, ledgeLayer);
        RaycastHit2D closestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || !IsValidLedgeCollider(hit.collider))
                continue;

            if (hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
            }
        }

        return closestHit;
    }

    private bool IsValidLedgeCollider(Collider2D hit)
    {
        if (hit == null)
            return false;

        if (bodyCollider != null && Physics2D.GetIgnoreCollision(bodyCollider, hit))
            return false;

        if (hit.GetComponentInParent<LandMovement>() != null)
            return false;

        if (hit.GetComponent<PlatformEffector2D>() != null || hit.GetComponentInParent<PlatformEffector2D>() != null)
            return false;

        return true;
    }

    private Bounds GetBodyBounds()
    {
        Collider2D currentBodyCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        return currentBodyCollider != null ? currentBodyCollider.bounds : new Bounds(transform.position, Vector3.one);
    }

    private IEnumerator ClimbRoutine(Vector2 wallHitPoint, Vector2 surfacePoint, float dirSign)
    {
        IsClimbing = true;
        gravityBeforeClimb = rb.gravityScale;

        playerMovement.ClearForcedHorizontalVelocity();
        playerMovement.ClearAirAttackFloat();
        playerMovement.SetGravityScale(0f);
        rb.velocity = Vector2.zero;
        animationController?.SetAnimatorSpeed(climbAnimationSpeed);

        Vector3 grabPosition = GetCornerAnchoredPosition(wallHitPoint, surfacePoint, dirSign, grabCornerAnchorOffset);
        MoveBodyTo(grabPosition);

        PlayLockedState(grabStateName, grabDuration);
        yield return HoldPositionForSeconds(grabPosition, grabDuration);

        Vector3 climbPosition = GetCornerAnchoredPosition(wallHitPoint, surfacePoint, dirSign, climbCornerAnchorOffset);
        MoveBodyTo(climbPosition);

        PlayLockedState(climbStateName, climbDuration);
        yield return HoldPositionForSeconds(climbPosition, climbDuration);

        ResetVisualOffset();
        Vector3 endPosition = GetClimbEndPosition(wallHitPoint, surfacePoint, dirSign);
        MoveBodyTo(endPosition);

        rb.velocity = Vector2.zero;

        float defaultGravity = playerMovement.Data != null ? playerMovement.Data.gravityScale : gravityBeforeClimb;
        playerMovement.SetGravityScale(defaultGravity);

        animationController?.ResetAnimatorSpeed();
        animationController?.ReturnToIdleAfterClimb(0f);

        IsClimbing = false;
        climbRoutine = null;
    }

    private IEnumerator HoldPositionForSeconds(Vector3 position, float duration)
    {
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            MoveBodyTo(position);
            rb.velocity = Vector2.zero;
            yield return null;
        }
    }

    private Vector3 GetCornerAnchoredPosition(Vector2 wallHitPoint, Vector2 surfacePoint, float dirSign, Vector2 anchorOffset)
    {
        Vector2 cornerPoint = new Vector2(wallHitPoint.x, surfacePoint.y);
        Vector2 facingAwareAnchor = new Vector2(anchorOffset.x * dirSign, anchorOffset.y);

        return new Vector3(
            cornerPoint.x - facingAwareAnchor.x,
            cornerPoint.y - facingAwareAnchor.y,
            transform.position.z);
    }

    private Vector3 GetClimbEndPosition(Vector2 wallHitPoint, Vector2 surfacePoint, float dirSign)
    {
        Bounds bounds = bodyCollider != null ? bodyCollider.bounds : new Bounds(transform.position, Vector3.one);
        float centerToFront = dirSign > 0f
            ? bounds.max.x - transform.position.x
            : transform.position.x - bounds.min.x;
        float centerToFeet = transform.position.y - bounds.min.y;

        return new Vector3(
            wallHitPoint.x + dirSign * (centerToFront + horizontalClearance),
            surfacePoint.y + centerToFeet + verticalClearance,
            transform.position.z);
    }

    private void PlayLockedState(string stateName, float lockDuration)
    {
        if (animationController == null || string.IsNullOrWhiteSpace(stateName))
            return;

        animationController.PlayLockedStateImmediate(stateName, lockDuration);
    }

    private void MoveBodyTo(Vector3 position)
    {
        if (rb != null)
            rb.position = position;

        transform.position = position;
        Physics2D.SyncTransforms();
    }

    private void ResetVisualOffset()
    {
        if (visualRoot != null && visualRoot != transform)
            visualRoot.localPosition = Vector3.zero;

        animationController?.ResetAnimatorSpeed();
    }

    private void HandleDamaged(float remainingHealth)
    {
        CancelClimb(false);
    }

    private void HandleDeath()
    {
        CancelClimb(false);
    }

    private void OnDrawGizmosSelected()
    {
        float dirSign = GetDebugFacingSign();
        Vector3 direction = dirSign > 0f ? Vector3.right : Vector3.left;
        Vector3 origin = transform.position;

        if (IsClimbing && hasLastLedgeDebug)
        {
            DrawStoredLedgeDebug();
        }
        else
        {
            DrawCurrentDetectionDebug(dirSign);
        }

        Gizmos.color = Color.magenta;
        Vector3 grabAnchor = origin + Vector3.up * grabCornerAnchorOffset.y + direction * grabCornerAnchorOffset.x;
        Gizmos.DrawWireSphere(grabAnchor, 0.04f);

        Gizmos.color = Color.green;
        Vector3 climbAnchor = origin + Vector3.up * climbCornerAnchorOffset.y + direction * climbCornerAnchorOffset.x;
        Gizmos.DrawWireSphere(climbAnchor, 0.035f);
    }

    private void DrawCurrentDetectionDebug(float dirSign)
    {
        Vector2 direction = dirSign > 0f ? Vector2.right : Vector2.left;
        Vector2 origin = transform.position;
        Vector2 lowerOrigin = origin + Vector2.up * lowerRayHeight;
        Vector2 upperOrigin = origin + Vector2.up * upperRayHeight;

        RaycastHit2D lowerHit = Physics2D.Raycast(lowerOrigin, direction, rayDistance, ledgeLayer);
        RaycastHit2D upperHit = Physics2D.Raycast(upperOrigin, direction, rayDistance, ledgeLayer);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(lowerOrigin, lowerOrigin + direction * rayDistance);
        if (lowerHit.collider != null)
            Gizmos.DrawWireSphere(lowerHit.point, 0.035f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(upperOrigin, upperOrigin + direction * rayDistance);
        if (upperHit.collider != null)
            Gizmos.DrawWireSphere(upperHit.point, 0.035f);

        if (lowerHit.collider == null || upperHit.collider != null)
            return;

        if (Application.isPlaying && (!CanAttemptClimb() || !IsMovingTowardLedge(dirSign)))
            return;

        GetSurfaceProbeLine(lowerHit.point, dirSign, out Vector2 probeStart, out Vector2 probeEnd);
        RaycastHit2D surfaceHit = CastSurfaceProbe(probeStart, probeEnd);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(probeStart, probeEnd);

        if (surfaceHit.collider == null || surfaceHit.normal.y < 0.5f)
            return;

        Vector2 cornerPoint = new Vector2(lowerHit.point.x, surfaceHit.point.y);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(cornerPoint, 0.055f);
    }

    private void DrawStoredLedgeDebug()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lastLowerRayStart, lastLowerRayEnd);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(lastUpperRayStart, lastUpperRayEnd);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(lastSurfaceRayStart, lastSurfaceRayEnd);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(lastCornerPoint, 0.055f);
    }

    private float GetDebugFacingSign()
    {
        if (Application.isPlaying && playerMovement != null)
            return playerMovement.IsFacingRight ? 1f : -1f;

        Transform debugVisualRoot = visualRoot;
        if (debugVisualRoot == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                debugVisualRoot = spriteRenderer.transform;
        }

        Transform facingTransform = debugVisualRoot != null ? debugVisualRoot : transform;
        return facingTransform.localScale.x < 0f ? -1f : 1f;
    }
}
