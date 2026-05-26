using System;
using UnityEngine;

[Serializable]
public class ActivationTarget
{
    [SerializeField] private MonoBehaviour target;
    [SerializeField] private ActivationAction action = ActivationAction.Activate;

    public bool HasTarget => target != null;

    public void Invoke(GameObject source = null)
    {
        Invoke(action, source);
    }

    public void Invoke(ActivationAction overrideAction, GameObject source = null)
    {
        if (target == null)
            return;

        if (target is not IActivatable activatable)
        {
            Debug.LogWarning($"[ActivationTarget] {target.name} does not implement IActivatable.", target);
            return;
        }

        switch (overrideAction)
        {
            case ActivationAction.Activate:
                activatable.Activate(source);
                break;
            case ActivationAction.Deactivate:
                activatable.Deactivate(source);
                break;
            case ActivationAction.Toggle:
                activatable.Toggle(source);
                break;
        }
    }
}
