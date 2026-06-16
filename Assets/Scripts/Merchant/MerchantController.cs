using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MerchantController : MonoBehaviour
{
    [Header("Merchant")]
    [SerializeField] private string merchantName = "Merchant";
    [SerializeField] private MerchantUI merchantUI;
    [SerializeField] private MerchantOfferDefinition[] offers;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactionPrompt;

    private PlayerEssenceWallet playerWallet;
    private int[] purchaseCounts;
    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();

    public string MerchantName => merchantName;
    public int OfferCount => offers != null ? offers.Length : 0;

    private void Awake()
    {
        if (merchantUI == null)
            merchantUI = FindObjectOfType<MerchantUI>(includeInactive: true);

        purchaseCounts = new int[OfferCount];
        SetPromptVisible(false);
    }

    private void OnValidate()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void Update()
    {
        if (playerWallet == null
            || merchantUI == null
            || MerchantUI.HasOpenMerchant
            || Time.timeScale <= 0f)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            SetPromptVisible(false);
            merchantUI.Open(this, playerWallet);
        }
    }

    private void OnDisable()
    {
        if (merchantUI != null && merchantUI.IsShowing(this))
            merchantUI.Close();

        playerColliders.Clear();
        playerWallet = null;
        SetPromptVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerEssenceWallet wallet = other.GetComponentInParent<PlayerEssenceWallet>();
        if (wallet == null)
            return;

        playerColliders.Add(other);
        playerWallet = wallet;
        SetPromptVisible(!MerchantUI.HasOpenMerchant);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerEssenceWallet wallet = other.GetComponentInParent<PlayerEssenceWallet>();
        if (wallet == null || wallet != playerWallet || !playerColliders.Remove(other))
            return;

        if (playerColliders.Count > 0)
            return;

        if (merchantUI != null && merchantUI.IsShowing(this))
            merchantUI.Close();

        playerWallet = null;
        SetPromptVisible(false);
    }

    public MerchantOfferDefinition GetOffer(int index)
    {
        return index >= 0 && index < OfferCount ? offers[index] : null;
    }

    public int GetPurchaseCount(int index)
    {
        EnsurePurchaseCounts();
        return index >= 0 && index < purchaseCounts.Length ? purchaseCounts[index] : 0;
    }

    public int GetPrice(int index)
    {
        MerchantOfferDefinition offer = GetOffer(index);
        return offer != null ? offer.GetPrice(GetPurchaseCount(index)) : 0;
    }

    public bool CanPurchase(int index, PlayerEssenceWallet wallet)
    {
        MerchantOfferDefinition offer = GetOffer(index);
        if (offer == null || wallet == null)
            return false;

        int purchaseCount = GetPurchaseCount(index);
        return purchaseCount < offer.MaximumPurchases
               && wallet.CanSpendEssence(offer.GetPrice(purchaseCount))
               && CanApply(offer, wallet.gameObject);
    }

    public bool TryPurchase(int index, PlayerEssenceWallet wallet, out string message)
    {
        MerchantOfferDefinition offer = GetOffer(index);
        if (offer == null || wallet == null)
        {
            message = "Offer or wallet is missing.";
            return false;
        }

        int purchaseCount = GetPurchaseCount(index);
        if (purchaseCount >= offer.MaximumPurchases)
        {
            message = "This upgrade is sold out.";
            return false;
        }

        int price = offer.GetPrice(purchaseCount);
        if (!wallet.CanSpendEssence(price))
        {
            message = "Not enough Essence.";
            return false;
        }

        if (!CanApply(offer, wallet.gameObject))
        {
            message = "This upgrade cannot be applied.";
            return false;
        }

        if (!wallet.TrySpendEssence(price) || !Apply(offer, wallet.gameObject))
        {
            message = "Purchase failed.";
            return false;
        }

        purchaseCounts[index]++;
        message = offer.DisplayName + " purchased.";
        return true;
    }

    private static bool CanApply(MerchantOfferDefinition offer, GameObject player)
    {
        if (offer == null || player == null)
            return false;

        Health health = player.GetComponent<Health>();
        PlayerMana mana = player.GetComponent<PlayerMana>();

        switch (offer.UpgradeType)
        {
            case MerchantUpgradeType.MaximumHealth:
                return health != null;
            case MerchantUpgradeType.MaximumMana:
            case MerchantUpgradeType.ManaRegeneration:
                return mana != null;
            case MerchantUpgradeType.ManaCostReduction:
                return mana != null && mana.ManaCostReduction < mana.MaximumManaCostReduction;
            default:
                return false;
        }
    }

    private static bool Apply(MerchantOfferDefinition offer, GameObject player)
    {
        Health health = player.GetComponent<Health>();
        PlayerMana mana = player.GetComponent<PlayerMana>();

        switch (offer.UpgradeType)
        {
            case MerchantUpgradeType.MaximumHealth:
                health.IncreaseMaximumHealth(offer.Amount);
                return true;
            case MerchantUpgradeType.MaximumMana:
                mana.IncreaseMaximumMana(Mathf.Max(1, Mathf.RoundToInt(offer.Amount)));
                return true;
            case MerchantUpgradeType.ManaRegeneration:
                mana.IncreaseRegenerationAmount(Mathf.Max(1, Mathf.RoundToInt(offer.Amount)));
                return true;
            case MerchantUpgradeType.ManaCostReduction:
                return mana.AddManaCostReduction(offer.Amount / 100f);
            default:
                return false;
        }
    }

    private void EnsurePurchaseCounts()
    {
        if (purchaseCounts == null || purchaseCounts.Length != OfferCount)
            purchaseCounts = new int[OfferCount];
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }
}
