using UnityEngine;

public class BloodKnightAnimationEvents : MonoBehaviour
{
    [SerializeField] private BloodKnightAI ai;
    [SerializeField] private BloodKnightAttack attack;
    [SerializeField] private BloodKnightDamageReaction damageReaction;

    private void Awake()
    {
        Transform parent = transform.parent;
        if (attack == null && parent != null)
            attack = parent.GetComponentInChildren<BloodKnightAttack>(true);

        if (attack == null)
            attack = GetComponentInParent<BloodKnightAttack>();

        if (damageReaction == null)
            damageReaction = GetComponentInParent<BloodKnightDamageReaction>();

        if (ai == null)
            ai = GetComponentInParent<BloodKnightAI>();
    }

    public void EnableHitbox()
    {
        attack?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        attack?.DisableHitbox();
    }

    public void OnDashBegin()
    {
        ai?.BeginAttackDash();
    }

    public void OnDashEnd()
    {
        ai?.EndAttackDash();
    }

    public void BeginAttackDash()
    {
        ai?.BeginAttackDash();
    }

    public void EndAttackDash()
    {
        ai?.EndAttackDash();
    }

    public void OnAttackEnd()
    {
        ai?.FinishAttackFromAnimation();
    }

    public void FinishAttack()
    {
        ai?.FinishAttackFromAnimation();
    }

    public void DeactivateSelf()
    {
        damageReaction?.DeactivateSelf();
    }
}
