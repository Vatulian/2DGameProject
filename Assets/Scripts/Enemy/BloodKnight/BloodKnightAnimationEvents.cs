using UnityEngine;

public class BloodKnightAnimationEvents : MonoBehaviour
{
    [SerializeField] private BloodKnightAttack attack;

    private void Awake()
    {
        if (attack != null)
            return;

        Transform parent = transform.parent;
        if (parent != null)
            attack = parent.GetComponentInChildren<BloodKnightAttack>(true);

        if (attack == null)
            attack = GetComponentInParent<BloodKnightAttack>();
    }

    public void EnableHitbox()
    {
        attack?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        attack?.DisableHitbox();
    }
}
