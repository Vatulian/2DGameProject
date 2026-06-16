using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyEssenceDropper : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private EssencePickup essencePickupPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Amount")]
    [SerializeField] private int minDrops = 2;
    [SerializeField] private int maxDrops = 10;
    [SerializeField] private int essencePerPickup = 1;
    [SerializeField] private bool dropOnlyOnce = true;

    [Header("Scatter")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.35f);
    [SerializeField] private float horizontalScatterSpeed = 2.8f;
    [SerializeField] private float upwardScatterSpeed = 4.2f;
    [SerializeField] private float randomScatterVariance = 0.8f;

    private bool dropped;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();
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
    }

    private void HandleDeath()
    {
        DropEssence();
    }

    public void DropEssence()
    {
        if (dropOnlyOnce && dropped)
            return;

        dropped = true;

        if (essencePickupPrefab == null)
        {
            Debug.LogWarning($"[EnemyEssenceDropper] Essence pickup prefab is missing on {name}.", this);
            return;
        }

        int min = Mathf.Max(0, minDrops);
        int max = Mathf.Max(min, maxDrops);
        int count = Random.Range(min, max + 1);
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + (Vector3)spawnOffset;

        for (int i = 0; i < count; i++)
        {
            EssencePickup pickup = Instantiate(essencePickupPrefab, origin, Quaternion.identity);
            pickup.Initialize(Mathf.Max(1, essencePerPickup), GetScatterVelocity(i, count));
        }
    }

    private Vector2 GetScatterVelocity(int index, int count)
    {
        float center = (count - 1) * 0.5f;
        float normalized = count <= 1 ? 0f : (index - center) / center;
        float horizontal = normalized * horizontalScatterSpeed;
        horizontal += Random.Range(-randomScatterVariance, randomScatterVariance);

        float upward = upwardScatterSpeed + Random.Range(0f, randomScatterVariance);
        return new Vector2(horizontal, upward);
    }
}
