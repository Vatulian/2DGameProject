using UnityEngine;
using UnityEngine.UI;

public class KeyUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Image iconTemplate;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private Vector2 iconSize = new Vector2(10f, 32f);
    [SerializeField] private float spacing = 14f;
    [SerializeField] private int maxVisibleIcons = 8;
    [SerializeField] private bool duplicateIconsForCount = true;

    private readonly System.Collections.Generic.List<Image> activeIcons = new();

    private void OnEnable()
    {
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();

        if (inventory != null) inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
        ClearIcons();
    }

    private void Refresh()
    {
        ClearIcons();

        if (inventory == null || iconTemplate == null)
            return;

        iconTemplate.enabled = false;

        RectTransform container = iconContainer != null ? iconContainer : transform as RectTransform;
        if (container == null)
            return;

        int drawn = 0;
        foreach (var keyCount in inventory.KeyCounts)
        {
            KeyItem key = keyCount.Key;
            int count = keyCount.Value;

            if (key == null || key.icon == null || count <= 0)
                continue;

            int drawCount = duplicateIconsForCount ? count : 1;
            for (int i = 0; i < drawCount && drawn < maxVisibleIcons; i++)
            {
                Image icon = CreateIcon(container, key.icon, drawn);
                activeIcons.Add(icon);
                drawn++;
            }

            if (drawn >= maxVisibleIcons)
                break;
        }
    }

    private Image CreateIcon(RectTransform container, Sprite sprite, int index)
    {
        GameObject iconObject = new GameObject($"{sprite.name}_KeyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(container, false);

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = sprite;
        icon.enabled = true;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = iconTemplate.color;
        icon.material = iconTemplate.material;

        RectTransform rect = icon.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = iconSize;
        rect.anchoredPosition = new Vector2(index * spacing, 0f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        return icon;
    }

    private void ClearIcons()
    {
        for (int i = 0; i < activeIcons.Count; i++)
        {
            if (activeIcons[i] != null)
            {
                activeIcons[i].gameObject.SetActive(false);
                Destroy(activeIcons[i].gameObject);
            }
        }

        activeIcons.Clear();
    }
}
