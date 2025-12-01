using UnityEngine;

public class ArenaWallsController : MonoBehaviour
{
    public void ActivateWalls()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateWalls()
    {
        gameObject.SetActive(false);
    }
}