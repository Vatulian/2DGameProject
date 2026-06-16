using System;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana")]
    [SerializeField, Min(1)] private int maximumMana = 100;
    [SerializeField, Min(0)] private int startingMana = 100;

    [Header("Regeneration")]
    [SerializeField, Min(0)] private int regenerationAmount = 1;
    [SerializeField, Min(0.01f)] private float regenerationInterval = 0.2f;
    [SerializeField, Min(0f)] private float regenerationDelayAfterSpend = 1f;

    [Header("Mana Cost")]
    [SerializeField, Range(0f, 0.9f)] private float manaCostReduction;
    [SerializeField, Range(0f, 0.9f)] private float maximumManaCostReduction = 0.75f;

    private int currentMana;
    private float regenerationTimer;
    private float regenerationDelayTimer;

    public int CurrentMana => currentMana;
    public int MaximumMana => maximumMana;
    public int RegenerationAmount => regenerationAmount;
    public float ManaCostReduction => manaCostReduction;
    public float MaximumManaCostReduction => maximumManaCostReduction;
    public float NormalizedMana => maximumMana > 0 ? (float)currentMana / maximumMana : 0f;

    public event Action<int, int> OnManaChanged;
    public event Action<int> OnManaSpent;
    public event Action<int> OnManaRestored;

    private void Awake()
    {
        maximumMana = Mathf.Max(1, maximumMana);
        currentMana = Mathf.Clamp(startingMana, 0, maximumMana);
    }

    private void Start()
    {
        // UI listeners are subscribed by this point, regardless of Awake order.
        NotifyManaChanged();
    }

    private void OnValidate()
    {
        maximumMana = Mathf.Max(1, maximumMana);
        startingMana = Mathf.Clamp(startingMana, 0, maximumMana);
        regenerationAmount = Mathf.Max(0, regenerationAmount);
        regenerationInterval = Mathf.Max(0.01f, regenerationInterval);
        regenerationDelayAfterSpend = Mathf.Max(0f, regenerationDelayAfterSpend);
        maximumManaCostReduction = Mathf.Clamp(maximumManaCostReduction, 0f, 0.9f);
        manaCostReduction = Mathf.Clamp(manaCostReduction, 0f, maximumManaCostReduction);
    }

    private void Update()
    {
        if (currentMana >= maximumMana || regenerationAmount <= 0)
        {
            regenerationTimer = 0f;
            return;
        }

        if (regenerationDelayTimer > 0f)
        {
            regenerationDelayTimer -= Time.deltaTime;
            return;
        }

        regenerationTimer += Time.deltaTime;
        int regenerationTicks = Mathf.FloorToInt(regenerationTimer / regenerationInterval);
        if (regenerationTicks <= 0)
            return;

        regenerationTimer -= regenerationTicks * regenerationInterval;
        RestoreMana(regenerationTicks * regenerationAmount);
    }

    public bool HasMana(int amount)
    {
        return amount >= 0 && currentMana >= amount;
    }

    public bool TrySpendMana(int amount)
    {
        if (amount < 0 || currentMana < amount)
            return false;

        if (amount == 0)
            return true;

        currentMana -= amount;
        regenerationTimer = 0f;
        regenerationDelayTimer = regenerationDelayAfterSpend;
        OnManaSpent?.Invoke(amount);
        NotifyManaChanged();
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0)
            return;

        int previousMana = currentMana;
        currentMana = Mathf.Min(currentMana + amount, maximumMana);
        int restoredAmount = currentMana - previousMana;

        if (restoredAmount <= 0)
            return;

        OnManaRestored?.Invoke(restoredAmount);
        NotifyManaChanged();
    }

    public void SetMana(int amount)
    {
        int clampedMana = Mathf.Clamp(amount, 0, maximumMana);
        if (currentMana == clampedMana)
            return;

        currentMana = clampedMana;
        NotifyManaChanged();
    }

    public void RefillMana()
    {
        RestoreMana(maximumMana - currentMana);
    }

    public void SetMaximumMana(int amount, bool refill = false)
    {
        maximumMana = Mathf.Max(1, amount);
        currentMana = refill ? maximumMana : Mathf.Min(currentMana, maximumMana);
        NotifyManaChanged();
    }

    public void IncreaseMaximumMana(int amount, bool restoreAddedAmount = true)
    {
        if (amount <= 0)
            return;

        maximumMana += amount;
        if (restoreAddedAmount)
            currentMana = Mathf.Min(currentMana + amount, maximumMana);

        NotifyManaChanged();
    }

    public void IncreaseRegenerationAmount(int amount)
    {
        if (amount > 0)
            regenerationAmount += amount;
    }

    public bool AddManaCostReduction(float amount)
    {
        if (amount <= 0f || manaCostReduction >= maximumManaCostReduction)
            return false;

        float previousReduction = manaCostReduction;
        manaCostReduction = Mathf.Min(manaCostReduction + amount, maximumManaCostReduction);
        return manaCostReduction > previousReduction;
    }

    public int GetModifiedManaCost(int baseCost)
    {
        if (baseCost <= 0)
            return 0;

        return Mathf.Max(1, Mathf.CeilToInt(baseCost * (1f - manaCostReduction)));
    }

    [ContextMenu("Mana Test/Spend 10 (Play Mode)")]
    private void SpendTenForTesting()
    {
        if (Application.isPlaying)
            TrySpendMana(10);
    }

    [ContextMenu("Mana Test/Restore 10 (Play Mode)")]
    private void RestoreTenForTesting()
    {
        if (Application.isPlaying)
            RestoreMana(10);
    }

    private void NotifyManaChanged()
    {
        OnManaChanged?.Invoke(currentMana, maximumMana);
    }
}
