using UnityEngine;

public interface IParryReceiver
{
    void OnParried(PlayerParry parry, Vector3 attackerPosition);
}
