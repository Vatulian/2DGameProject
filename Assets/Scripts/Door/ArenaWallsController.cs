using UnityEngine;

public class ArenaWallsController : MonoBehaviour, IActivatable
{
    public bool IsActive => gameObject.activeSelf;

    public void ActivateWalls()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateWalls()
    {
        gameObject.SetActive(false);
    }

    public void Activate(GameObject source = null)
    {
        ActivateWalls();
    }

    public void Deactivate(GameObject source = null)
    {
        DeactivateWalls();
    }

    public void Toggle(GameObject source = null)
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
