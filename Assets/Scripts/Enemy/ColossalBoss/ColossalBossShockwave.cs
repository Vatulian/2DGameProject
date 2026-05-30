using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColossalBossShockwave : MonoBehaviour
{
    private Vector2 direction;
    private Vector3 origin;
    private Vector3 damageSource;
    private HashSet<Health> hitTargets = new HashSet<Health>();
    private ColossalBossShockwaveSettings settings;

    public void Initialize(
        Vector3 spawnOrigin,
        Vector2 moveDirection,
        ColossalBossShockwaveSettings waveSettings,
        Vector3 source,
        HashSet<Health> sharedHitTargets = null)
    {
        origin = spawnOrigin;
        transform.position = origin;
        direction = moveDirection.normalized;
        settings = waveSettings;
        damageSource = source;
        hitTargets = sharedHitTargets ?? new HashSet<Health>();

        if (settings == null || direction == Vector2.zero)
        {
            Destroy(gameObject);
            return;
        }

        settings.Validate();
        StartCoroutine(SpawnPieces());
    }

    private IEnumerator SpawnPieces()
    {
        float travelled = 0f;

        while (travelled <= settings.maxDistance)
        {
            Vector3 piecePosition = origin + (Vector3)(direction * travelled) + (Vector3)settings.pieceSpawnOffset;
            SpawnPiece(piecePosition);

            float waitTime = settings.pieceSpawnSpacing / settings.moveSpeed;
            travelled += settings.pieceSpawnSpacing;
            yield return new WaitForSeconds(waitTime);
        }

        Destroy(gameObject);
    }

    private void SpawnPiece(Vector3 position)
    {
        if (settings.shockwavePiecePrefab == null)
            return;

        GameObject pieceObject = Instantiate(settings.shockwavePiecePrefab, position, Quaternion.identity);
        pieceObject.name = "ColossalBoss Shockwave Piece";
        pieceObject.SetActive(true);

        ColossalBossShockwavePiece piece = pieceObject.GetComponent<ColossalBossShockwavePiece>();
        if (piece == null)
            piece = pieceObject.AddComponent<ColossalBossShockwavePiece>();

        piece.Configure(settings, damageSource, hitTargets);
    }
}
