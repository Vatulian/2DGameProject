using UnityEngine;

public class PlayerSpecialMoveFxEventForwarder : MonoBehaviour
{
    private PlayerSpecialMove owner;
    private bool useSwordHitbox;
    private bool damageApplied;

    public void Begin(PlayerSpecialMove moveOwner, bool isSwordAttack)
    {
        owner = moveOwner;
        useSwordHitbox = isSwordAttack;
        damageApplied = false;
    }

    public void ApplySpecialMoveDamage()
    {
        if (damageApplied || owner == null)
            return;

        damageApplied = true;
        owner.ApplyFxDamage(transform.position, useSwordHitbox);
    }
}
