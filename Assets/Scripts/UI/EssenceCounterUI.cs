using UnityEngine;
using UnityEngine.UI;

public class EssenceCounterUI : MonoBehaviour
{
    [SerializeField] private PlayerEssenceWallet wallet = null;
    [SerializeField] private Text amountText = null;
    [SerializeField] private string amountFormat = "{0}";

    private void Awake()
    {
        ResolveWallet();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveWallet();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (wallet != null)
            return;

        ResolveWallet();
        Subscribe();
        Refresh();
    }

    private void ResolveWallet()
    {
        if (wallet != null)
            return;

        if (PlayerReference.Player != null)
        {
            wallet = PlayerReference.Player.GetComponent<PlayerEssenceWallet>();
            if (wallet != null)
                return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            wallet = player.GetComponent<PlayerEssenceWallet>();
    }

    private void Subscribe()
    {
        if (wallet != null)
            wallet.OnEssenceChanged += HandleEssenceChanged;
    }

    private void Unsubscribe()
    {
        if (wallet != null)
            wallet.OnEssenceChanged -= HandleEssenceChanged;
    }

    private void HandleEssenceChanged(int currentEssence)
    {
        SetAmount(currentEssence);
    }

    private void Refresh()
    {
        SetAmount(wallet != null ? wallet.CurrentEssence : 0);
    }

    private void SetAmount(int amount)
    {
        if (amountText != null)
            amountText.text = string.Format(amountFormat, amount);
    }
}
