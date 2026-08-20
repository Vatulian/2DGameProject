using UnityEngine;

public class ArrowTrap : MonoBehaviour
{
    [SerializeField] private float attackCooldown;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] arrows;
    private float cooldownTimer;

    [Header("SFX")]
    [SerializeField] private AudioClip arrowSound;

    private void Attack()
    {
        cooldownTimer = 0;

        if (firePoint == null || arrows == null || arrows.Length == 0)
            return;

        int arrowIndex = FindArrow();
        GameObject arrow = arrows[arrowIndex];
        if (arrow == null)
            return;

        EnemyProjectile projectile = arrow.GetComponent<EnemyProjectile>();
        if (projectile == null)
            return;

        if (arrowSound != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(arrowSound);

        arrow.transform.position = firePoint.position;
        projectile.ActivateProjectile();
    }
    private int FindArrow()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            if (!arrows[i].activeInHierarchy)
                return i;
        }
        return 0;
    }
    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= attackCooldown)
            Attack();
    }
}
