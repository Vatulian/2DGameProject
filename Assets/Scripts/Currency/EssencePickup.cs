using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EssencePickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;

    [Header("Timing")]
    [SerializeField] private float collectDelay = 0.08f;
    [SerializeField] private float lifetime = 14f;

    [Header("Homing")]
    [SerializeField] private float homingSpeed = 15f;
    [SerializeField] private float collectDistance = 0.28f;
    [SerializeField] private bool disableGravityWhileHoming = true;

    [Header("Feedback")]
    [SerializeField] private AudioClip collectSound;

    private Rigidbody2D rb;
    private Collider2D pickupCollider;
    private Transform target;
    private PlayerEssenceWallet targetWallet;
    private float spawnTime;
    private bool collected;
    private float originalGravityScale;
    private bool hasOriginalGravityScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pickupCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            hasOriginalGravityScale = true;
        }
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
        collected = false;
        target = null;
        targetWallet = null;

        if (rb != null && hasOriginalGravityScale)
            rb.gravityScale = originalGravityScale;

        StartHoming();
    }

    private void Update()
    {
        if (collected)
            return;

        if (lifetime > 0f && Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        AcquireTargetIfNeeded();
        if (target == null || targetWallet == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= collectDistance && Time.time >= spawnTime + collectDelay)
            Collect(targetWallet);
    }

    private void FixedUpdate()
    {
        if (collected)
            return;

        AcquireTargetIfNeeded();
        if (target == null || targetWallet == null)
            return;

        StartHoming();

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - currentPosition;
        Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

        if (rb != null)
            rb.velocity = direction * homingSpeed;
        else
            transform.position = Vector2.MoveTowards(currentPosition, targetPosition, homingSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < spawnTime + collectDelay)
            return;

        PlayerEssenceWallet wallet = FindWallet(other);
        if (wallet != null)
            Collect(wallet);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < spawnTime + collectDelay)
            return;

        PlayerEssenceWallet wallet = FindWallet(collision.collider);
        if (wallet != null)
            Collect(wallet);
    }

    public void Initialize(int pickupAmount, Vector2 initialVelocity)
    {
        amount = Mathf.Max(1, pickupAmount);
        spawnTime = Time.time;
        collected = false;
        target = null;
        targetWallet = null;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();

        StartHoming();
    }

    private void StartHoming()
    {
        if (pickupCollider != null)
            pickupCollider.isTrigger = true;

        if (disableGravityWhileHoming && rb != null)
            rb.gravityScale = 0f;
    }

    private void AcquireTargetIfNeeded()
    {
        if (targetWallet != null && target != null)
            return;

        if (PlayerReference.IsAvailable)
        {
            target = PlayerReference.Player;
            targetWallet = target.GetComponent<PlayerEssenceWallet>();
            if (targetWallet != null)
                return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        target = player.transform;
        targetWallet = player.GetComponent<PlayerEssenceWallet>();
    }

    private static PlayerEssenceWallet FindWallet(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return null;

        return other.GetComponent<PlayerEssenceWallet>() ?? other.GetComponentInParent<PlayerEssenceWallet>();
    }

    private void Collect(PlayerEssenceWallet wallet)
    {
        if (collected || wallet == null)
            return;

        collected = true;
        wallet.AddEssence(amount);

        if (collectSound != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(collectSound);

        Destroy(gameObject);
    }
}