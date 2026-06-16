using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Text amountText;

    [Header("Optional Text")]
    [SerializeField] private string amountFormat = "{0} / {1}";

    private PlayerMana subscribedMana;

    private void Awake()
    {
        ResolvePlayerMana();
        Refresh();
    }

    private void OnEnable()
    {
        ResolvePlayerMana();
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        // PlayerMana.Awake has completed before Start, so the initial value is reliable.
        ResolvePlayerMana();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (playerMana != null)
            return;

        ResolvePlayerMana();
        Subscribe();
        Refresh();
    }

    private void ResolvePlayerMana()
    {
        if (playerMana != null)
            return;

        if (PlayerReference.Player != null)
        {
            playerMana = PlayerReference.Player.GetComponent<PlayerMana>();
            if (playerMana != null)
                return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMana = player.GetComponent<PlayerMana>();
    }

    private void Subscribe()
    {
        if (playerMana == null || subscribedMana == playerMana)
            return;

        Unsubscribe();
        subscribedMana = playerMana;
        subscribedMana.OnManaChanged += HandleManaChanged;
    }

    private void Unsubscribe()
    {
        if (subscribedMana != null)
            subscribedMana.OnManaChanged -= HandleManaChanged;

        subscribedMana = null;
    }

    private void HandleManaChanged(int currentMana, int maximumMana)
    {
        SetDisplay(currentMana, maximumMana);
    }

    private void Refresh()
    {
        if (playerMana == null)
        {
            SetDisplay(0, 1);
            return;
        }

        SetDisplay(playerMana.CurrentMana, playerMana.MaximumMana);
    }

    private void SetDisplay(int currentMana, int maximumMana)
    {
        float normalizedMana = maximumMana > 0
            ? Mathf.Clamp01((float)currentMana / maximumMana)
            : 0f;

        if (fillRect != null)
        {
            Vector3 fillScale = fillRect.localScale;
            fillScale.x = normalizedMana;
            fillRect.localScale = fillScale;
        }

        if (amountText != null)
            amountText.text = string.Format(amountFormat, currentMana, maximumMana);
    }
}
