using UnityEngine;

public class EnemyProjectile : EnemyDamage
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    [SerializeField] private float speed;
    [SerializeField] private float resetTime;
    private float lifetime;

    private void Awake()
    {
        ApplyEnemyAttackLayer();
    }

    private void OnValidate()
    {
        ApplyEnemyAttackLayer();
    }

    public void ActivateProjectile()
    {
        lifetime = 0;
        gameObject.SetActive(true);
    }
    private void Update()
    {
        float movementSpeed = speed * Time.deltaTime;
        transform.Translate(movementSpeed, 0, 0);

        lifetime += Time.deltaTime;
        if (lifetime > resetTime)
            gameObject.SetActive(false);
    }

    private new void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision); //Execute logic from parent script first
        gameObject.SetActive(false); //When this hits any object deactivate arrow
    }

    private void ApplyEnemyAttackLayer()
    {
        int enemyAttackLayer = LayerMask.NameToLayer(EnemyAttackLayerName);
        if (enemyAttackLayer >= 0 && gameObject.layer != enemyAttackLayer)
            gameObject.layer = enemyAttackLayer;
    }
}
