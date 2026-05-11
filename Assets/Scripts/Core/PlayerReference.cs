using UnityEngine;

public static class PlayerReference
{
    public static Transform Player { get; private set; }
    public static Health Health { get; private set; }

    public static bool IsAvailable =>
        Player != null && Health != null && !Health.IsDead;

    public static void Register(GameObject player)
    {
        if (player == null)
            return;

        Player = player.transform;
        Health = player.GetComponent<Health>();
    }

    public static void Unregister()
    {
        Player = null;
        Health = null;
    }
}
