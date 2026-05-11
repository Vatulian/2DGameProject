using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerReferenceProvider : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        PlayerReference.Register(gameObject);

        if (health != null)
        {
            health.OnDeath += HandleDeath;
            health.OnRespawned += HandleRespawned;
        }
    }

    private void OnDisable()
    {
        PlayerReference.Unregister();

        if (health != null)
        {
            health.OnDeath -= HandleDeath;
            health.OnRespawned -= HandleRespawned;
        }
    }

    private void HandleDeath()
    {
        PlayerReference.Unregister();
    }

    private void HandleRespawned()
    {
        PlayerReference.Register(gameObject);
    }
}
