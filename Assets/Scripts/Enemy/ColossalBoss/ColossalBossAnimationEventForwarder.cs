using UnityEngine;

public class ColossalBossAnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private ColossalBossAI ai;

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
            ai = GetComponentInParent<ColossalBossAI>();

        if (ai == null && transform.parent != null)
            ai = transform.parent.GetComponentInChildren<ColossalBossAI>(true);
    }

    public void EnableHitbox()
    {
        ai?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        ai?.DisableHitbox();
    }

    public void EndAttack()
    {
        ai?.EndAttack();
    }

    public void SpawnShockwaves()
    {
        ai?.SpawnShockwaves();
    }

    public void EndBuff()
    {
        ai?.EndBuff();
    }
}
