using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MerchantUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text walletAmountText;
    [SerializeField] private string walletFormat = "ESSENCE: {0}";
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("Pre-created Offer Rows")]
    [SerializeField] private MerchantOfferView[] offerViews;

    private static MerchantUI activeUI;
    private static int lastClosedFrame = -1;

    private MerchantController merchant;
    private PlayerEssenceWallet wallet;
    private float previousTimeScale = 1f;
    private int selectedIndex;
    private int openedFrame = -1;

    public static bool HasOpenMerchant => activeUI != null;
    public static bool BlocksPauseInput => activeUI != null || lastClosedFrame == Time.frameCount;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (activeUI == this)
            activeUI = null;
    }

    private void Update()
    {
        if (activeUI != this)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (openedFrame == Time.frameCount)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)
                 || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return)
                 || Input.GetKeyDown(KeyCode.KeypadEnter)
                 || Input.GetKeyDown(KeyCode.Space)
                 || Input.GetKeyDown(KeyCode.E))
        {
            SubmitSelection();
        }
    }

    public void Open(MerchantController targetMerchant, PlayerEssenceWallet targetWallet)
    {
        if (targetMerchant == null || targetWallet == null || panel == null)
            return;

        if (activeUI != null && activeUI != this)
            activeUI.Close();

        merchant = targetMerchant;
        wallet = targetWallet;
        previousTimeScale = Time.timeScale;
        openedFrame = Time.frameCount;
        selectedIndex = 0;
        activeUI = this;

        wallet.OnEssenceChanged += HandleEssenceChanged;
        panel.SetActive(true);
        Time.timeScale = 0f;

        if (statusText != null)
            statusText.text = string.Empty;

        Refresh();
    }

    public void Close()
    {
        if (activeUI != this)
            return;

        if (wallet != null)
            wallet.OnEssenceChanged -= HandleEssenceChanged;

        if (panel != null)
            panel.SetActive(false);

        Time.timeScale = previousTimeScale;
        merchant = null;
        wallet = null;
        activeUI = null;
        lastClosedFrame = Time.frameCount;
    }

    public bool IsShowing(MerchantController targetMerchant)
    {
        return activeUI == this && merchant == targetMerchant;
    }

    public void Refresh()
    {
        if (merchant == null || wallet == null)
            return;

        if (walletAmountText != null)
            walletAmountText.text = string.Format(walletFormat, wallet.CurrentEssence);

        int viewCount = offerViews != null ? offerViews.Length : 0;
        for (int i = 0; i < viewCount; i++)
        {
            MerchantOfferView view = offerViews[i];
            if (view == null)
                continue;

            MerchantOfferDefinition offer = merchant.GetOffer(i);
            if (offer == null)
            {
                view.SetVisible(false);
                continue;
            }

            int offerIndex = i;
            view.SetVisible(true);
            view.Bind(
                offer,
                merchant.GetPrice(i),
                merchant.GetPurchaseCount(i),
                merchant.CanPurchase(i, wallet),
                () => Buy(offerIndex),
                () => Select(offerIndex));
            view.SetSelected(i == selectedIndex);
        }
    }

    private void Select(int index)
    {
        if (merchant == null || index < 0 || index >= merchant.OfferCount)
            return;

        selectedIndex = index;
        RefreshSelection();
    }

    private void MoveSelection(int direction)
    {
        int count = merchant != null ? merchant.OfferCount : 0;
        if (count <= 0)
            return;

        selectedIndex = (selectedIndex + direction + count) % count;
        RefreshSelection();
    }

    private void SubmitSelection()
    {
        if (offerViews == null || selectedIndex < 0 || selectedIndex >= offerViews.Length)
            return;

        offerViews[selectedIndex]?.Submit();
    }

    private void RefreshSelection()
    {
        if (offerViews == null)
            return;

        for (int i = 0; i < offerViews.Length; i++)
            offerViews[i]?.SetSelected(i == selectedIndex);
    }

    private void Buy(int offerIndex)
    {
        if (merchant == null || wallet == null)
            return;

        merchant.TryPurchase(offerIndex, wallet, out string message);
        if (statusText != null)
            statusText.text = message;

        Refresh();
    }

    private void HandleEssenceChanged(int currentEssence)
    {
        Refresh();
    }
}
