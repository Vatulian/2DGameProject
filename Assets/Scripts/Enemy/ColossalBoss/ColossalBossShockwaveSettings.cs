using UnityEngine;

[System.Serializable]
public class ColossalBossShockwaveSettings
{
    [Header("Wave")]
    public float maxDistance = 18f;
    public float moveSpeed = 8f;
    public float pieceSpawnSpacing = 1.2f;
    public float pieceLifetime = 1f;
    public float pieceColliderLifetime = 0.5f;
    public int damage = 1;

    [Header("Piece Prefab")]
    public GameObject shockwavePiecePrefab;
    public string visualStateName = "Water7 remake";
    public Vector2 pieceSpawnOffset = Vector2.zero;

    [Header("Debug")]
    public bool drawDebug = true;
    public Color pieceColliderGizmoColor = new Color(1f, 0f, 0f, 0.85f);
    public Color pathGizmoColor = new Color(1f, 0.9f, 0.1f, 0.85f);

    public void Validate()
    {
        maxDistance = Mathf.Max(0f, maxDistance);
        moveSpeed = Mathf.Max(0.05f, moveSpeed);
        pieceSpawnSpacing = Mathf.Max(0.05f, pieceSpawnSpacing);
        pieceLifetime = Mathf.Max(0.05f, pieceLifetime);
        pieceColliderLifetime = Mathf.Clamp(pieceColliderLifetime, 0.01f, pieceLifetime);
        damage = Mathf.Max(0, damage);
    }
}
