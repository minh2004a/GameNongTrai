using UnityEngine;

/// <summary>
/// Hiển thị icon kết quả thu hoạch/bắt được vật phẩm ở gần nhân vật, cho biết
/// vật phẩm đã nằm ở hotbar, túi hay rơi xuống đất.
/// </summary>
[DefaultExecutionOrder(220)]
public class HarvestResultIndicator : MonoBehaviour
{
    enum StorageState { Hotbar, Bag, Ground }

    [Header("References")]
    [SerializeField] PlayerInventory inventory;
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] SpriteRenderer stateRenderer;

    [Header("Sprites")]
    [SerializeField] Sprite hotbarStateSprite;
    [SerializeField] Sprite bagStateSprite;
    [SerializeField] Sprite groundStateSprite;

    [Header("Hiển thị")]
    [SerializeField] Vector2 iconOffset = new Vector2(0f, 1.6f);
    [SerializeField] Vector2 stateOffset = new Vector2(0.45f, 0.35f);
    [SerializeField, Min(0.1f)] float stateScale = 0.5f;
    [SerializeField, Min(0f)] float displaySeconds = 2.5f;
    [SerializeField] int sortingOrderOffset = 25;
    [SerializeField] bool autoCreateRenderers = true;

    [Header("Màu sắc")]
    [SerializeField] Color hotbarColor = Color.white;
    [SerializeField] Color bagColor = new Color(0.85f, 0.95f, 1f, 1f);
    [SerializeField] Color groundColor = new Color(1f, 0.7f, 0.7f, 1f);

    float hideAtTime;
    bool visible;

    void Reset()
    {
        inventory = GetComponent<PlayerInventory>();
        playerSprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!playerSprite) playerSprite = GetComponentInChildren<SpriteRenderer>();
        EnsureRenderers();
        HideImmediate();
    }

    void OnEnable()
    {
        if (inventory) inventory.ItemAdded += OnItemAdded;
        ApplySorting();
    }

    void OnDisable()
    {
        if (inventory) inventory.ItemAdded -= OnItemAdded;
        HideImmediate();
    }

    void LateUpdate()
    {
        if (!visible) return;
        UpdatePositions();
        ApplySorting();
        if (Time.time >= hideAtTime) HideImmediate();
    }

    void EnsureRenderers()
    {
        if (!iconRenderer && autoCreateRenderers)
        {
            var go = new GameObject("HarvestResultIcon", typeof(SpriteRenderer));
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one;
            iconRenderer = go.GetComponent<SpriteRenderer>();
        }
        if (!stateRenderer && autoCreateRenderers)
        {
            var go = new GameObject("HarvestStateIcon", typeof(SpriteRenderer));
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one;
            stateRenderer = go.GetComponent<SpriteRenderer>();
        }
    }

    void OnItemAdded(ItemSO item, InventoryAddResult result)
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (!item || !item.icon) return;

        StorageState? state = ResolveState(result);
        if (!state.HasValue)
        {
            HideImmediate();
            return;
        }

        Show(item, state.Value);
    }

    StorageState? ResolveState(InventoryAddResult result)
    {
        if (result.requested <= 0 && result.AddedTotal <= 0) return null;
        if (result.remaining > 0) return StorageState.Ground;
        if (result.addedToHotbar > 0) return StorageState.Hotbar;
        if (result.addedToBag > 0) return StorageState.Bag;
        return null;
    }

    void Show(ItemSO item, StorageState state)
    {
        EnsureRenderers();
        if (!iconRenderer) return;

        iconRenderer.enabled = true;
        iconRenderer.sprite = item.icon;
        iconRenderer.color = StateColor(state);

        if (stateRenderer)
        {
            Sprite overlay = StateSprite(state);
            if (overlay)
            {
                stateRenderer.enabled = true;
                stateRenderer.sprite = overlay;
                stateRenderer.color = Color.white;
                stateRenderer.transform.localScale = Vector3.one * stateScale;
            }
            else
            {
                stateRenderer.enabled = false;
            }
        }

        visible = true;
        hideAtTime = Time.time + displaySeconds;
        UpdatePositions();
        ApplySorting();
    }

    void UpdatePositions()
    {
        if (iconRenderer)
        {
            iconRenderer.transform.localPosition = iconOffset;
        }
        if (stateRenderer)
        {
            stateRenderer.transform.localPosition = iconOffset + stateOffset;
        }
    }

    void ApplySorting()
    {
        if (!playerSprite) return;
        if (iconRenderer)
        {
            iconRenderer.sortingLayerID = playerSprite.sortingLayerID;
            iconRenderer.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
        }
        if (stateRenderer && iconRenderer)
        {
            stateRenderer.sortingLayerID = iconRenderer.sortingLayerID;
            stateRenderer.sortingOrder = iconRenderer.sortingOrder + 1;
        }
    }

    void HideImmediate()
    {
        visible = false;
        if (iconRenderer) iconRenderer.enabled = false;
        if (stateRenderer) stateRenderer.enabled = false;
    }

    Sprite StateSprite(StorageState state)
    {
        return state switch
        {
            StorageState.Hotbar => hotbarStateSprite,
            StorageState.Bag => bagStateSprite,
            StorageState.Ground => groundStateSprite,
            _ => null
        };
    }

    Color StateColor(StorageState state)
    {
        return state switch
        {
            StorageState.Hotbar => hotbarColor,
            StorageState.Bag => bagColor,
            StorageState.Ground => groundColor,
            _ => Color.white
        };
    }
}
