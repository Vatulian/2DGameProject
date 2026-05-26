using UnityEngine;

public class PlayerAnimationEventForwarder : MonoBehaviour
{
    private PlayerMeleeAttack meleeAttack;
    private PlayerRespawn respawn;

    private void Awake()
    {
        meleeAttack = GetComponentInParent<PlayerMeleeAttack>();
        respawn = GetComponentInParent<PlayerRespawn>();
    }

    public void OpenComboWindow()
    {
        meleeAttack?.OpenComboWindow();
    }

    public void CloseComboWindow()
    {
        meleeAttack?.CloseComboWindow();
    }

    public void EnableHitbox()
    {
        meleeAttack?.EnableHitbox();
    }

    public void EnableHitboxWindow(int windowIndex)
    {
        meleeAttack?.EnableHitboxWindow(windowIndex);
    }

    public void DisableHitbox()
    {
        meleeAttack?.DisableHitbox();
    }

    public void CompleteAttackPhase()
    {
        meleeAttack?.CompleteAttackPhase();
    }

    public void CheckRespawn()
    {
        respawn?.CheckRespawn();
    }
}
