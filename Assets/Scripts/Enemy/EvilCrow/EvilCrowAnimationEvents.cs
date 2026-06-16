using UnityEngine;

public class EvilCrowAnimationEvents : MonoBehaviour
{
    [SerializeField] private EvilCrowRangedAttack rangedAttack;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }

    public void FireProjectile()
    {
        rangedAttack?.FireProjectileFromAnimation();
    }

    public void FinishAttack()
    {
        rangedAttack?.FinishAttackFromAnimation();
    }

    private void ResolveReferences()
    {
        if (rangedAttack == null)
            rangedAttack = GetComponentInParent<EvilCrowRangedAttack>();

        if (rangedAttack == null && transform.parent != null)
            rangedAttack = transform.parent.GetComponentInChildren<EvilCrowRangedAttack>(true);
    }
}
