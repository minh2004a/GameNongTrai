using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Hiển thị vật phẩm đang chọn trên đầu người chơi (trừ vũ khí và công cụ)
/// đồng thời đồng bộ các tham số Animator cho tư thế cầm vật.
/// </summary>
[DefaultExecutionOrder(200)]
public class PlayerHeldItemDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerInventory inventory;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField, FormerlySerializedAs("displaySprite")] SpriteRenderer displayRenderer;
    [SerializeField, FormerlySerializedAs("body")] Rigidbody2D playerBody;
    [SerializeField] PlayerStamina stamina;

    [Header("Hiển thị")]
    [SerializeField] Vector2 displayOffset = new Vector2(0f, 1.35f);
    [SerializeField] int sortingOrderOffset = 10;
    [SerializeField] float targetIconHeight = 0.75f;
    [SerializeField] bool autoCreateRenderer = true;
    [SerializeField] bool flipWithFacing = false;

    [Header("Animator Holding Layer")]
    [SerializeField] string holdingBool = "HoldingItem";
    [SerializeField] string holdMovingBool = "HoldMoving";
    [SerializeField] string holdStateParam = "HoldState";
    [SerializeField] string holdLayerName = "HoldingItem";
    [SerializeField] float holdLayerWeight = 1f;
    [SerializeField] float movingSpeedThreshold = 0.15f;
    [SerializeField] float runningSpeedThreshold = 2.6f;
    [SerializeField] float normalAnimatorSpeedMultiplier = 1f;
    [SerializeField] float exhaustedAnimatorSpeedMultiplier = 0.6f;

    ItemSO currentItem;
    bool isVisible;

    int holdLayerIndex = -1;
    int holdingBoolHash;
    int holdMovingBoolHash;
    int holdStateHash;
    bool hasHoldingBool;
    bool hasHoldMovingBool;
    bool hasHoldState;

    float baseAnimatorSpeed = 1f;

    void Reset()
    {
        inventory = GetComponent<PlayerInventory>();
        animator = GetComponentInChildren<Animator>();
        playerSprite = GetComponentInChildren<SpriteRenderer>();
        playerBody = GetComponent<Rigidbody2D>();
        stamina = GetComponent<PlayerStamina>();
    }

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerSprite) playerSprite = GetComponentInChildren<SpriteRenderer>();
        if (!playerBody) playerBody = GetComponent<Rigidbody2D>();
        if (!stamina) stamina = GetComponent<PlayerStamina>();

        baseAnimatorSpeed = animator ? animator.speed : 1f;

        EnsureDisplayRenderer();
        CacheAnimatorParameters();
        ApplySorting();
        HideDisplay();
    }

    void OnEnable()
    {
        if (inventory != null)
        {
            inventory.SelectedChanged += OnSelectedChanged;
            inventory.HotbarChanged += OnHotbarChanged;
        }
        RefreshDisplay();
        UpdateAnimatorState();
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.SelectedChanged -= OnSelectedChanged;
            inventory.HotbarChanged -= OnHotbarChanged;
        }
        SetHoldLayerWeight(0f);
        RestoreAnimatorSpeed();
    }

    void Update()
    {
        if (displayRenderer && isVisible)
        {
            displayRenderer.transform.localPosition = displayOffset;
            if (flipWithFacing && playerSprite)
            {
                displayRenderer.flipX = playerSprite.flipX;
            }
            ApplySorting();
        }

        UpdateAnimatorState();
    }

    void OnSelectedChanged(int _)
    {
        RefreshDisplay();
    }

    void OnHotbarChanged()
    {
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
        ItemSO nextItem = inventory ? inventory.CurrentItem : null;
        if (nextItem == currentItem)
        {
            if (!ShouldShow(nextItem)) HideDisplay();
            return;
        }

        currentItem = nextItem;
        if (!ShouldShow(currentItem))
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
        EnsureDisplayRenderer();
        if (!displayRenderer) return;

        displayRenderer.sprite = item.icon;
        displayRenderer.enabled = displayRenderer.sprite != null;
        displayRenderer.transform.localPosition = displayOffset;
        ScaleIcon();

        isVisible = displayRenderer.enabled;
        if (isVisible) ApplySorting();
        UpdateAnimatorState();
    }

    void HideDisplay()
    {
        if (displayRenderer)
        {
            displayRenderer.sprite = null;
            displayRenderer.enabled = false;
        }
        isVisible = false;
        UpdateAnimatorState();
    }

    void ScaleIcon()
    {
        if (!displayRenderer || !displayRenderer.sprite) return;

        float worldHeight = displayRenderer.sprite.rect.height / displayRenderer.sprite.pixelsPerUnit;
        float scale = 1f;
        if (worldHeight > 0.001f)
        {
            scale = Mathf.Max(0.01f, targetIconHeight / worldHeight);
        }
        displayRenderer.transform.localScale = Vector3.one * scale;
    }

    void EnsureDisplayRenderer()
    {
        if (displayRenderer) return;
        if (!autoCreateRenderer) return;

        var go = new GameObject("HeldItemDisplay");
        go.transform.SetParent(transform);
        go.transform.localPosition = displayOffset;
        displayRenderer = go.AddComponent<SpriteRenderer>();
        displayRenderer.enabled = false;
    }

    void ApplySorting()
    {
        if (!displayRenderer) return;
        if (playerSprite)
        {
            displayRenderer.sortingLayerID = playerSprite.sortingLayerID;
            displayRenderer.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
        }
    }

    void CacheAnimatorParameters()
    {
        if (!animator) return;

        holdingBoolHash = Animator.StringToHash(holdingBool);
        holdMovingBoolHash = Animator.StringToHash(holdMovingBool);
        holdStateHash = Animator.StringToHash(holdStateParam);

        foreach (var param in animator.parameters)
        {
            if (param.nameHash == holdingBoolHash) hasHoldingBool = true;
            if (param.nameHash == holdMovingBoolHash) hasHoldMovingBool = true;
            if (param.nameHash == holdStateHash && param.type == AnimatorControllerParameterType.Int)
                hasHoldState = true;
        }

        holdLayerIndex = string.IsNullOrEmpty(holdLayerName) ? -1 : animator.GetLayerIndex(holdLayerName);
        if (holdLayerIndex < 0) holdLayerIndex = -1;
    }

    void UpdateAnimatorState()
    {
        if (!animator)
            return;

        float speed = playerBody ? playerBody.velocity.magnitude : 0f;
        bool exhausted = stamina && stamina.IsExhausted;
        bool moving = speed > movingSpeedThreshold;
        bool running = speed > runningSpeedThreshold && !exhausted;

        if (hasHoldingBool)
            animator.SetBool(holdingBoolHash, isVisible);
        if (hasHoldMovingBool)
            animator.SetBool(holdMovingBoolHash, isVisible && moving);
        if (hasHoldState)
        {
            int stateValue = 0;
            if (isVisible)
            {
                if (running) stateValue = 2;
                else if (moving) stateValue = 1;
            }
            animator.SetInteger(holdStateHash, stateValue);
        }

        SetHoldLayerWeight(isVisible ? holdLayerWeight : 0f);
        ApplyAnimatorSpeed(exhausted && isVisible);
    }

    void SetHoldLayerWeight(float weight)
    {
        if (!animator) return;
        if (holdLayerIndex < 0) return;
        animator.SetLayerWeight(holdLayerIndex, weight);
    }

    void ApplyAnimatorSpeed(bool exhausted)
    {
        if (!animator) return;
        float targetMultiplier = exhausted ? exhaustedAnimatorSpeedMultiplier : normalAnimatorSpeedMultiplier;
        animator.speed = baseAnimatorSpeed * Mathf.Max(0.01f, targetMultiplier);
    }

    void RestoreAnimatorSpeed()
    {
        if (!animator) return;
        animator.speed = baseAnimatorSpeed;
    }
}
