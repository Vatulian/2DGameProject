using UnityEngine;

public enum MerchantUpgradeType
{
    MaximumHealth,
    MaximumMana,
    ManaRegeneration,
    ManaCostReduction
}

[CreateAssetMenu(fileName = "Merchant Offer", menuName = "Merchant/Upgrade Offer")]
public class MerchantOfferDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private Sprite icon;

    [Header("Price")]
    [SerializeField, Min(0)] private int basePrice = 10;
    [SerializeField, Min(0)] private int priceIncreasePerPurchase;
    [SerializeField, Min(1)] private int maximumPurchases = 1;

    [Header("Upgrade")]
    [SerializeField] private MerchantUpgradeType upgradeType;
    [SerializeField, Min(0.01f)] private float amount = 1f;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaximumPurchases => maximumPurchases;
    public MerchantUpgradeType UpgradeType => upgradeType;
    public float Amount => amount;

    public int GetPrice(int purchaseCount)
    {
        long price = (long)basePrice + (long)priceIncreasePerPurchase * Mathf.Max(0, purchaseCount);
        if (price <= 0L)
            return 0;

        return price >= int.MaxValue ? int.MaxValue : (int)price;
    }

    private void OnValidate()
    {
        basePrice = Mathf.Max(0, basePrice);
        priceIncreasePerPurchase = Mathf.Max(0, priceIncreasePerPurchase);
        maximumPurchases = Mathf.Max(1, maximumPurchases);
        amount = Mathf.Max(0.01f, amount);
    }
}
