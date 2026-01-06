using UnityEngine;

[CreateAssetMenu(menuName = "Game/Key Item")]
public class KeyItem : ScriptableObject
{
    public string keyId;   // e.g. "red_key"
    public Sprite icon;
}