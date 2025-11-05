using UnityEngine;

// Hiển thị item trên tay nhân vật khi chọn vật phẩm không phải công cụ hay vũ khí
public class PlayerHeldItemDisplay : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField] SpriteRenderer displayRenderer;
    [SerializeField] Vector2 displayOffset = new Vector2(0f, 1.4f);
    [SerializeField] int sortingOrderOffset = 5;
    [SerializeField] string holdLayerName = "HoldItem";
    [SerializeField, Range(0f, 1f)] float holdLayerWeight = 1f;
    [SerializeField] bool autoCreateRenderer = true;
    [SerializeField] bool flipWithFacing = true;

    static readonly int HoldingItemHash = Animator.StringToHash("HoldingItem");
    bool isHolding;
    bool hasHoldLayer;
    int holdLayerIndex = -1;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerSprite) playerSprite = GetComponentInChildren<SpriteRenderer>();
        EnsureRenderer();
        CacheHoldLayer();
        if (displayRenderer) displayRenderer.enabled = false;
    }

    void OnEnable()
    {
        if (inventory != null)
        {
            inventory.SelectedChanged += HandleSelectionChanged;
            inventory.HotbarChanged += HandleHotbarChanged;
        }
        RefreshHeldItem();
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.SelectedChanged -= HandleSelectionChanged;
            inventory.HotbarChanged -= HandleHotbarChanged;
        }
        ApplyHoldLayer(false);
    }

    void LateUpdate()
    {
        if (!displayRenderer || !displayRenderer.enabled) return;
        var t = displayRenderer.transform;
        var offset = displayOffset;
        if (flipWithFacing && playerSprite)
        {
            float dir = playerSprite.flipX ? -1f : 1f;
            offset.x = Mathf.Abs(displayOffset.x) * dir;
            displayRenderer.flipX = playerSprite.flipX;
        }
        t.localPosition = (Vector3)offset;
        AlignSortingLayer();
    }

    void HandleSelectionChanged(int _)
    {
        RefreshHeldItem();
    }

    void HandleHotbarChanged()
    {
        RefreshHeldItem();
    }

    void RefreshHeldItem()
    {
        var item = inventory ? inventory.CurrentItem : null;
        bool shouldShow = item && item.category != ItemCategory.Tool && item.category != ItemCategory.Weapon;

        if (animator)
        {
            animator.SetBool(HoldingItemHash, shouldShow);
        }

        ApplyHoldLayer(shouldShow);

        if (!displayRenderer)
        {
            return;
        }

        if (shouldShow && item.icon)
        {
            displayRenderer.sprite = item.icon;
            displayRenderer.enabled = true;
            AlignSortingLayer();
        }
        else
        {
            displayRenderer.sprite = null;
            displayRenderer.enabled = false;
        }
    }

    void AlignSortingLayer()
    {
        if (!displayRenderer || !playerSprite) return;
        displayRenderer.sortingLayerID = playerSprite.sortingLayerID;
        displayRenderer.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
    }

    void EnsureRenderer()
    {
        if (displayRenderer || !autoCreateRenderer) return;

        Transform parent = playerSprite ? playerSprite.transform : transform;
        var go = new GameObject("HeldItemSprite");
        go.transform.SetParent(parent, false);
        displayRenderer = go.AddComponent<SpriteRenderer>();
        displayRenderer.enabled = false;
    }

    void CacheHoldLayer()
    {
        if (!animator || string.IsNullOrEmpty(holdLayerName)) return;

        int index = animator.GetLayerIndex(holdLayerName);
        if (index >= 0)
        {
            holdLayerIndex = index;
            hasHoldLayer = true;
            animator.SetLayerWeight(holdLayerIndex, 0f);
        }
    }

    void ApplyHoldLayer(bool active)
    {
        if (!hasHoldLayer || !animator) return;
        if (isHolding == active) return;
        isHolding = active;
        animator.SetLayerWeight(holdLayerIndex, active ? holdLayerWeight : 0f);
    }
}
