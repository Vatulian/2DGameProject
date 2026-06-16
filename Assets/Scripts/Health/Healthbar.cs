using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image totalhealthBar;
    [SerializeField] private Image currenthealthBar;
    [SerializeField] private float displayedMaxHealth = 10f;

    private void Awake()
    {
        ResolvePlayerHealth();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (playerHealth == null)
            ResolvePlayerHealth();

        RefreshAll();
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth != null)
            return;

        if (PlayerReference.Health != null)
        {
            playerHealth = PlayerReference.Health;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void RefreshAll()
    {
        if (playerHealth == null)
            ResolvePlayerHealth();

        if (playerHealth == null)
            return;

        float maxHealth = GetDisplayedMaxHealth();

        if (totalhealthBar != null)
            totalhealthBar.fillAmount = Mathf.Clamp01(playerHealth.StartingHealth / maxHealth);

        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        if (playerHealth == null || currenthealthBar == null)
            return;

        currenthealthBar.fillAmount = Mathf.Clamp01(playerHealth.CurrentHealth / GetDisplayedMaxHealth());
    }

    private float GetDisplayedMaxHealth()
    {
        return Mathf.Max(1f, displayedMaxHealth);
    }
}
