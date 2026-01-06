using UnityEngine;
using UnityEngine.UI;

public class KeyUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private KeyItem watchKey;
    [SerializeField] private Image iconImage;

    private void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (inventory == null || iconImage == null || watchKey == null) return;

        int count = inventory.GetCount(watchKey);
        iconImage.enabled = count > 0;
        iconImage.sprite = watchKey.icon;
    }
}