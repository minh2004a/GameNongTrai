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

    static readonly int HoldingItemHash = Animator.StringToHash("HoldingItem");

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerSprite) playerSprite = GetComponentInChildren<SpriteRenderer>();
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
    }

    void LateUpdate()
    {
        if (!displayRenderer || !displayRenderer.enabled) return;
        var t = displayRenderer.transform;
        t.localPosition = (Vector3)displayOffset;
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
}
