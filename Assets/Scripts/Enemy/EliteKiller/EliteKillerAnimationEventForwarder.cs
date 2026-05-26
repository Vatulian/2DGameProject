using UnityEngine;

public class EliteKillerAnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private EliteKillerBossAI ai;
    [SerializeField] private EliteKillerAttackHitbox comboHitbox;
    [SerializeField] private EliteKillerAttackHitbox chainHitbox;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (ai == null)
            ai = GetComponentInParent<EliteKillerBossAI>();

        if (ai == null && transform.parent != null)
            ai = transform.parent.GetComponentInChildren<EliteKillerBossAI>(true);

        EliteKillerAttackHitbox[] hitboxes = ai != null
            ? ai.GetComponentsInChildren<EliteKillerAttackHitbox>(true)
            : GetComponentsInParent<EliteKillerAttackHitbox>(true);

        if (comboHitbox == null && hitboxes.Length > 0)
            comboHitbox = hitboxes[0];

        if (chainHitbox == null && hitboxes.Length > 1)
            chainHitbox = hitboxes[1];
    }

    public void ComboHit1()
    {
        ai?.BeginComboHitFromAnimation(1);
    }

    public void ComboHit2()
    {
        ai?.BeginComboHitFromAnimation(2);
    }

    public void ComboHit3()
    {
        ai?.BeginComboHitFromAnimation(3);
    }

    public void DisableComboHitbox()
    {
        if (ai != null)
            ai.DisableComboHitboxFromAnimation();
        else
            comboHitbox?.DisableHitbox();
    }

    public void PlayChainHitbox()
    {
        ai?.PlayChainHitboxFromAnimation();
    }

    public void FinishAttack()
    {
        ai?.FinishCurrentAttackFromAnimation();
    }
}
