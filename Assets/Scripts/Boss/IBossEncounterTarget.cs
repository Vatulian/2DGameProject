using UnityEngine;

public interface IBossEncounterTarget
{
    bool IsEncounterDefeated { get; }

    void SetEncounterSpawnPosition(Vector3 position);
    void ActivateEncounter();
    void DeactivateEncounter();
    void ResetEncounter();
}
