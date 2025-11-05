using UnityEngine;

/// <summary>
/// Hiển thị vật phẩm đang chọn trên đầu người chơi (trừ vũ khí và công cụ)
/// đồng thời bật animation dơ tay.
/// </summary>
[DefaultExecutionOrder(200)]
public class PlayerHeldItemDisplay : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField] SpriteRenderer displaySprite;
    [SerializeField] Rigidbody2D body;
    [SerializeField] float displayHeight = 1.35f;
    [SerializeField] float targetIconHeight = 0.75f;

    ItemSO currentItem;
    bool isHolding;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerSprite) playerSprite = GetComponentInChildren<SpriteRenderer>();
        if (!body) body = GetComponent<Rigidbody2D>();
        EnsureDisplaySprite();
        HideDisplay();
        RefreshDisplay();
    }

    void OnEnable()
    {
        if (inventory)
        {
            inventory.SelectedChanged += OnSelectionChanged;
            inventory.HotbarChanged += OnHotbarChanged;
        }
        RefreshDisplay();
    }

    void OnDisable()
    {
        if (inventory)
        {
            inventory.SelectedChanged -= OnSelectionChanged;
            inventory.HotbarChanged -= OnHotbarChanged;
        }
    }

    void OnSelectionChanged(int _)
    {
        RefreshDisplay();
    }

    void OnHotbarChanged()
    {
        RefreshDisplay();
    }

    void EnsureDisplaySprite()
    {
        if (displaySprite) return;

        var go = new GameObject("HeldItemDisplay");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, displayHeight, 0f);
        displaySprite = go.AddComponent<SpriteRenderer>();
        displaySprite.enabled = false;
        displaySprite.sortingLayerName = playerSprite ? playerSprite.sortingLayerName : "Characters";
        displaySprite.sortingOrder = playerSprite ? playerSprite.sortingOrder + 10 : 10;
    }

    void RefreshDisplay()
    {
        var nextItem = inventory ? inventory.CurrentItem : null;
        if (nextItem == currentItem)
        {
            // Nếu item không đổi nhưng bị ẩn (ví dụ nhân vật vừa spawn) -> đảm bảo trạng thái đúng
            if (!ShouldShow(currentItem)) HideDisplay();
            return;
        }

        currentItem = nextItem;
        bool shouldShow = ShouldShow(currentItem);

        if (!shouldShow)
        {
            HideDisplay();
            return;
        }

        ShowItem(currentItem);
    }

    bool ShouldShow(ItemSO item)
    {
        if (!item) return false;
        if (item.category == ItemCategory.Tool) return false;
        if (item.category == ItemCategory.Weapon) return false;
        return item.icon != null;
    }

    void ShowItem(ItemSO item)
    {
        if (!displaySprite) EnsureDisplaySprite();
        if (!displaySprite) return;

        displaySprite.sprite = item ? item.icon : null;
        displaySprite.enabled = displaySprite.sprite != null;
        if (displaySprite.enabled)
        {
            displaySprite.transform.localPosition = new Vector3(0f, displayHeight, 0f);
            float worldHeight = 0f;
            if (displaySprite.sprite)
            {
                worldHeight = displaySprite.sprite.rect.height / displaySprite.sprite.pixelsPerUnit;
            }
            float scale = 1f;
            if (worldHeight > 0.001f)
            {
                scale = Mathf.Max(0.01f, targetIconHeight / worldHeight);
            }
            displaySprite.transform.localScale = Vector3.one * scale;
        }

        isHolding = true;
        UpdateAnimator();
    }

    void HideDisplay()
    {
        if (displaySprite)
        {
            displaySprite.sprite = null;
            displaySprite.enabled = false;
        }
        isHolding = false;
        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (!animator) return;
        animator.SetBool("HoldingItem", isHolding);
        animator.SetBool("HoldMoving", isHolding && IsMoving());
    }

    void Update()
    {
        if (!animator) return;
        if (!isHolding)
        {
            if (animator.GetBool("HoldMoving"))
                animator.SetBool("HoldMoving", false);
            return;
        }

        animator.SetBool("HoldMoving", IsMoving());
    }

    bool IsMoving()
    {
        if (!body) return false;
        return body.velocity.sqrMagnitude > 0.0001f;
    }
}
