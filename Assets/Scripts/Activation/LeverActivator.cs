using UnityEngine;

public class LeverActivator : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private ActivationTarget[] targets;
    [SerializeField] private ActivationAction action = ActivationAction.Toggle;
    [SerializeField] private bool useTargetActions;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool requirePlayerInside = true;
    [SerializeField] private bool oneShot;
    [SerializeField] private float cooldown = 0.15f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private string sortingLayerName = "Decor";
    [SerializeField] private int sortingOrder = 20;

    private bool playerInside;
    private bool activated;
    private bool used;
    private float nextUseTime;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        ApplySorting();
    }

    private void Awake()
    {
        ResolveSpriteRenderer();

        if (inactiveSprite == null && spriteRenderer != null)
            inactiveSprite = spriteRenderer.sprite;

        ApplySorting();
        ApplyVisualState();
    }

    private void OnValidate()
    {
        ResolveSpriteRenderer();
        ApplySorting();
    }

    private void Update()
    {
        if (requirePlayerInside && !playerInside)
            return;

        if (Input.GetKeyDown(interactKey))
            Use();
    }

    public void Use()
    {
        if (oneShot && used)
            return;

        if (Time.time < nextUseTime)
            return;

        used = true;
        nextUseTime = Time.time + Mathf.Max(0f, cooldown);

        if (action == ActivationAction.Toggle)
            activated = !activated;
        else
            activated = action == ActivationAction.Activate;

        ApplyVisualState();

        InvokeTargets();
    }

    private void ApplyVisualState()
    {
        if (spriteRenderer == null)
            return;

        Sprite nextSprite = activated ? activeSprite : inactiveSprite;
        if (nextSprite != null)
            spriteRenderer.sprite = nextSprite;
    }

    private void ResolveSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
    }

    private void ApplySorting()
    {
        if (spriteRenderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
            spriteRenderer.sortingLayerName = sortingLayerName;

        spriteRenderer.sortingOrder = sortingOrder;
    }

    private void InvokeTargets()
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (useTargetActions)
                targets[i]?.Invoke(gameObject);
            else
                targets[i]?.Invoke(action, gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }
}
