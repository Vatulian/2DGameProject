using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<KeyItem, int> keyCounts = new();
    public event Action OnChanged;
    public IEnumerable<KeyValuePair<KeyItem, int>> KeyCounts => keyCounts;

    public int GetCount(KeyItem key) => key != null && keyCounts.TryGetValue(key, out int count) ? count : 0;
    public bool HasKey(KeyItem key, int required = 1) => GetCount(key) >= required;

    public void AddKey(KeyItem key, int amount = 1)
    {
        if (key == null)
        {
            Debug.LogError("[Inventory] AddKey failed: key is NULL!");
            return;
        }

        keyCounts[key] = GetCount(key) + amount;
        Debug.Log($"[Inventory] Key ADDED -> {key.name} | Count = {keyCounts[key]}");
        OnChanged?.Invoke();
    }

    public bool TryConsumeKey(KeyItem key, int amount = 1)
    {
        if (key == null)
        {
            Debug.LogError("[Inventory] TryConsumeKey failed: key is NULL!");
            return false;
        }

        if (!HasKey(key, amount))
        {
            Debug.Log($"[Inventory] NOT enough key -> {key.name} | Have={GetCount(key)} Need={amount}");
            return false;
        }

        int remaining = GetCount(key) - amount;
        if (remaining > 0)
            keyCounts[key] = remaining;
        else
            keyCounts.Remove(key);

        Debug.Log($"[Inventory] Key USED -> {key.name} | Remaining = {remaining}");
        OnChanged?.Invoke();
        return true;
    }

    public void DebugPrintAllKeys()
    {
        foreach (var kvp in keyCounts)
            Debug.Log($"[Inventory] KEY LIST -> {kvp.Key.name} = {kvp.Value}");
    }
}
