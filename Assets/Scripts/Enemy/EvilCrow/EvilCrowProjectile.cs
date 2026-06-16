using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EvilCrowProjectile : MonoBehaviour
{
    private static readonly int SpawnStateHash = Animator.StringToHash("Attack Start");
    private static readonly int TravelStateHash = Animator.StringToHash("Attack Idle");
    private static readonly int ImpactStateHash = Animator.StringToHash("Attack Explode");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 7f;
    [SerializeField, Min(0f)] private float lifetime = 5f;
    [SerializeField] private bool rotateTowardsDirection = true;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private LayerMask impactLayers;

    [Header("Parry")]
    [SerializeField] private ParryAttackSettings parry = new ParryAttackSettings();

    [Header("Animation")]
    [SerializeField, Min(0f)] private float spawnAnimationDuration = 0.45f;
    [SerializeField, Min(0f)] private float impactAnimationDuration = 0.45f;

    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private GameObject owner;
    private Vector2 direction = Vector2.right;
    private Vector2 previousPosition;
    private bool launched;
    private bool impacted;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector2 launchDirection, GameObject projectileOwner)
    {
        owner = projectileOwner;
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.right;

        if (rotateTowardsDirection)
            transform.right = direction;
        else if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0f;

        launched = true;
        previousPosition = rb.position;
        rb.velocity = direction * speed;

        if (animator != null)
            animator.Play(SpawnStateHash, 0, 0f);

        StartCoroutine(EnterTravelAnimation());
        lifetimeCoroutine = StartCoroutine(DestroyAfterLifetime());
    }

    private void FixedUpdate()
    {
        if (!launched || impacted)
            return;

        CheckSweptImpact();

        if (impacted)
            return;

        rb.velocity = direction * speed;
        previousPosition = rb.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryImpact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryImpact(collision.collider);
    }

    private void TryImpact(Collider2D other)
    {
        if (impacted || other == null || IsOwnerCollider(other))
            return;

        int otherLayerMask = 1 << other.gameObject.layer;
        bool canDamage = (damageLayers.value & otherLayerMask) != 0;
        bool blocksProjectile = (impactLayers.value & otherLayerMask) != 0;

        if (!canDamage && !blocksProjectile)
            return;

        if (canDamage && parry != null)
        {
            Vector3 attackerPosition = owner != null ? owner.transform.position : transform.position;
            if (parry.TryParry(other, attackerPosition, this))
            {
                Impact();
                return;
            }
        }

        if (canDamage)
        {
            Health targetHealth = other.GetComponentInParent<Health>();
            if (targetHealth != null)
                targetHealth.TakeDamage(damage, transform.position);
        }

        Impact();
    }

    private void CheckSweptImpact()
    {
        LayerMask blockingLayers = damageLayers | impactLayers;
        if (blockingLayers.value == 0)
            return;

        Vector2 currentPosition = rb.position;
        float radius = GetSweepRadius();

        Collider2D overlap = Physics2D.OverlapCircle(currentPosition, radius, blockingLayers);
        if (overlap != null)
        {
            TryImpact(overlap);
            return;
        }

        Vector2 travel = currentPosition - previousPosition;
        float distance = travel.magnitude;
        if (distance <= 0.001f)
            return;

        RaycastHit2D hit = Physics2D.CircleCast(previousPosition, radius, travel / distance, distance, blockingLayers);
        if (hit.collider == null)
            return;

        rb.position = hit.centroid;
        transform.position = hit.centroid;
        TryImpact(hit.collider);
    }

    private float GetSweepRadius()
    {
        if (projectileCollider is CircleCollider2D circle)
            return circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        Bounds bounds = projectileCollider.bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        if (owner == null)
            return false;

        return other.gameObject == owner || other.transform.IsChildOf(owner.transform);
    }

    private void Impact()
    {
        impacted = true;
        launched = false;

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        rb.velocity = Vector2.zero;
        rb.simulated = false;
        projectileCollider.enabled = false;

        if (animator != null)
            animator.Play(ImpactStateHash, 0, 0f);

        Destroy(gameObject, impactAnimationDuration);
    }

    private IEnumerator DestroyAfterLifetime()
    {
        if (lifetime > 0f)
            yield return new WaitForSeconds(lifetime);

        if (!impacted)
            Destroy(gameObject);
    }

    private IEnumerator EnterTravelAnimation()
    {
        if (spawnAnimationDuration > 0f)
            yield return new WaitForSeconds(spawnAnimationDuration);

        if (!impacted && animator != null)
            animator.Play(TravelStateHash, 0, 0f);
    }
}
