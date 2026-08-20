using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantOfferView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text purchaseCountText;
    [SerializeField] private Button buyButton;
    [SerializeField] private string priceFormat = "{0} Essence";
    [SerializeField] private string purchaseCountFormat = "{0} / {1}";
    [SerializeField] private string soldOutText = "SOLD OUT";
    [SerializeField] private Color selectedColor = new Color(0.38f, 0.24f, 0.08f, 1f);

    private Action buyAction;
    private Action selectAction;
    private Color normalColor;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            normalColor = backgroundImage.color;

        if (buyButton != null)
            buyButton.onClick.AddListener(HandleBuyClicked);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(HandleBuyClicked);
    }

    public void Bind(
        MerchantOfferDefinition offer,
        int price,
        int purchaseCount,
        bool canPurchase,
        Action onBuy,
        Action onSelect)
    {
        buyAction = onBuy;
        selectAction = onSelect;
        bool soldOut = purchaseCount >= offer.MaximumPurchases;

        if (iconImage != null)
        {
            iconImage.sprite = offer.Icon;
            iconImage.enabled = offer.Icon != null;
        }

        if (priceText != null)
            priceText.text = soldOut ? soldOutText : string.Format(priceFormat, price);
        if (purchaseCountText != null)
            purchaseCountText.text = string.Format(purchaseCountFormat, purchaseCount, offer.MaximumPurchases);
        if (buyButton != null)
            buyButton.interactable = canPurchase;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible)
        {
            buyAction = null;
            selectAction = null;
        }
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    public void Submit()
    {
        if (buyButton != null && buyButton.interactable)
            buyAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        selectAction?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        selectAction?.Invoke();
    }

    private void HandleBuyClicked()
    {
        selectAction?.Invoke();
        buyAction?.Invoke();
    }
}
