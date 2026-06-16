using System;
using UnityEngine;

public class PlayerEssenceWallet : MonoBehaviour
{
    [SerializeField] private int startingEssence;
    [SerializeField] private bool clampToMaximum;
    [SerializeField] private int maximumEssence = 9999;

    private int currentEssence;

    public int CurrentEssence => currentEssence;
    public event Action<int> OnEssenceChanged;
    public event Action<int> OnEssenceAdded;
    public event Action<int> OnEssenceSpent;

    private void Awake()
    {
        currentEssence = Mathf.Max(0, startingEssence);

        if (clampToMaximum)
            currentEssence = Mathf.Min(currentEssence, Mathf.Max(0, maximumEssence));
    }

    public void AddEssence(int amount)
    {
        if (amount <= 0)
            return;

        int previous = currentEssence;
        currentEssence += amount;

        if (clampToMaximum)
            currentEssence = Mathf.Min(currentEssence, Mathf.Max(0, maximumEssence));

        int added = currentEssence - previous;
        if (added <= 0)
            return;

        OnEssenceAdded?.Invoke(added);
        OnEssenceChanged?.Invoke(currentEssence);
    }

    public bool CanSpendEssence(int amount)
    {
        return amount >= 0 && currentEssence >= amount;
    }

    public bool TrySpendEssence(int amount)
    {
        if (amount < 0 || currentEssence < amount)
            return false;

        if (amount == 0)
            return true;

        currentEssence -= amount;
        OnEssenceSpent?.Invoke(amount);
        OnEssenceChanged?.Invoke(currentEssence);
        return true;
    }

    public void SetEssence(int amount)
    {
        currentEssence = Mathf.Max(0, amount);

        if (clampToMaximum)
            currentEssence = Mathf.Min(currentEssence, Mathf.Max(0, maximumEssence));

        OnEssenceChanged?.Invoke(currentEssence);
    }
}
