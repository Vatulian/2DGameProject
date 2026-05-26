using UnityEngine;

public interface IActivatable
{
    bool IsActive { get; }
    void Activate(GameObject source = null);
    void Deactivate(GameObject source = null);
    void Toggle(GameObject source = null);
}
