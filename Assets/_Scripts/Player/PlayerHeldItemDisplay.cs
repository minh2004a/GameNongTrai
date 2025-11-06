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
    [SerializeField, FormerlySerializedAs("displaySprite"), FormerlySerializedAs("displayRenderer")] SpriteRenderer iconRenderer;
    [SerializeField, FormerlySerializedAs("body")] Rigidbody2D playerBody;
    [SerializeField] PlayerStamina stamina;

    [Header("Hiển thị")]
    [Tooltip("Vị trí icon hiển thị so với nhân vật khi cầm vật phẩm.")]
    [SerializeField] Vector2 displayOffset = new Vector2(0f, 1.35f);
    [Tooltip("Độ ưu tiên sorting order so với sprite nhân vật để icon luôn nằm phía trước.")]
    [SerializeField] int sortingOrderOffset = 10;
    [Tooltip("Chiều cao chuẩn của icon khi tự co giãn theo kích thước sprite.")]
    [SerializeField] float targetIconHeight = 0.75f;
    [Tooltip("Tự động tạo SpriteRenderer con nếu chưa có sẵn.")]
    [SerializeField] bool autoCreateRenderer = true;
    [Tooltip("Lật icon theo hướng quay của nhân vật.")]
    [SerializeField] bool flipWithFacing = false;
    [Header("Thu hoạch bằng tay")]
    [Tooltip("Vị trí bắt đầu nâng icon khi nhổ cây bằng tay (tọa độ cục bộ).")]
    [SerializeField] Vector2 handHarvestStartOffset = new Vector2(0f, -0.45f);
    [Tooltip("Khoảng cách icon xuất hiện phía trước mặt người chơi theo hướng đang nhổ.")]
    [SerializeField, Min(0f)] float handHarvestForwardDistance = 0.6f;
    [Tooltip("Quãng đường tối thiểu icon cần đi lên khi nhổ bằng tay.")]
    [SerializeField, Min(0f)] float handHarvestMinLift = 0.4f;
    [Tooltip("Độ lệch ngang tối đa icon được phép di chuyển về phía người chơi.")]
    [SerializeField, Min(0f)] float handHarvestMaxHorizontal = 0.85f;
    [Tooltip("Đường cong easing điều khiển chuyển động nâng icon từ đất lên đầu.")]
    [SerializeField] AnimationCurve handHarvestLiftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
    bool hasOverride;
    ItemSO overrideItem;
    float overrideHideAt;
    bool isAnimatingHandHarvest;
    Vector3 handHarvestStartLocal;
    Vector3 handHarvestWorldStart;
    Vector2 handHarvestFacing;
    float handHarvestStartTime;
    float handHarvestDuration;

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

        EnsureIconRenderer();
        CacheAnimatorParameters();
        ApplySorting();
        HideIcon();
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
        hasOverride = false;
        overrideItem = null;
        overrideHideAt = 0f;
        HideIcon();
        SetHoldLayerWeight(0f);
        RestoreAnimatorSpeed();
    }

    void Update()
    {
        if (iconRenderer && isVisible)
        {
            UpdateHandHarvestAnimation();
            if (!isAnimatingHandHarvest)
            {
                iconRenderer.transform.localPosition = displayOffset;
            }
            if (flipWithFacing && playerSprite)
            {
                iconRenderer.flipX = playerSprite.flipX;
            }
            ApplySorting();
        }

        UpdateAnimatorState();
        UpdateTemporaryOverride();
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
        currentItem = ShouldShow(nextItem) ? nextItem : null;

        if (hasOverride)
        {
            ShowItem(overrideItem);
            return;
        }

        if (!currentItem)
        {
            HideIcon();
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
        EnsureIconRenderer();
        if (!iconRenderer) return;

        isAnimatingHandHarvest = false;
        iconRenderer.sprite = item.icon;
        iconRenderer.enabled = iconRenderer.sprite != null;
        iconRenderer.transform.localPosition = displayOffset;
        ScaleIcon();

        isVisible = iconRenderer.enabled;
        if (isVisible) ApplySorting();
        UpdateAnimatorState();
    }

    void HideIcon()
    {
        if (iconRenderer)
        {
            iconRenderer.sprite = null;
            iconRenderer.enabled = false;
        }
        isVisible = false;
        isAnimatingHandHarvest = false;
        UpdateAnimatorState();
    }

    void ScaleIcon()
    {
        if (!iconRenderer || !iconRenderer.sprite) return;

        float worldHeight = iconRenderer.sprite.rect.height / iconRenderer.sprite.pixelsPerUnit;
        float scale = 1f;
        if (worldHeight > 0.001f)
        {
            scale = Mathf.Max(0.01f, targetIconHeight / worldHeight);
        }
        iconRenderer.transform.localScale = Vector3.one * scale;
    }

    void EnsureIconRenderer()
    {
        if (iconRenderer) return;
        if (!autoCreateRenderer) return;

        var go = new GameObject("PlayerHeldItemDisplayIcon");
        go.transform.SetParent(transform);
        go.transform.localPosition = displayOffset;
        iconRenderer = go.AddComponent<SpriteRenderer>();
        iconRenderer.enabled = false;
    }

    void ApplySorting()
    {
        if (!iconRenderer) return;
        if (playerSprite)
        {
            iconRenderer.sortingLayerID = playerSprite.sortingLayerID;
            iconRenderer.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
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

    void UpdateTemporaryOverride()
    {
        if (!hasOverride) return;
        if (Time.time < overrideHideAt) return;

        hasOverride = false;
        overrideItem = null;
        overrideHideAt = 0f;
        RefreshDisplay();
    }

    public void ShowTemporaryItem(ItemSO item, float seconds)
    {
        if (!item || !item.icon) return;
        if (seconds <= 0f) seconds = 0.01f;

        overrideItem = item;
        overrideHideAt = Time.time + seconds;
        hasOverride = true;
        isAnimatingHandHarvest = false;
        ShowItem(item);
    }

    public void ShowHandHarvestedItem(ItemSO item, Vector3 worldStartPosition, Vector2 facingDirection, float liftDuration, float visibleDuration)
    {
        if (!item || !item.icon) return;

        float duration = Mathf.Max(0.01f, visibleDuration);
        float raiseTime = Mathf.Max(0.01f, liftDuration);

        overrideItem = item;
        overrideHideAt = Time.time + Mathf.Max(duration, raiseTime);
        hasOverride = true;

        isAnimatingHandHarvest = false;
        ShowItem(item);

        if (!iconRenderer) return;

        handHarvestWorldStart = worldStartPosition;
        handHarvestFacing = facingDirection;
        handHarvestStartLocal = CalculateHandHarvestLocalStart();
        iconRenderer.transform.localPosition = handHarvestStartLocal;
        handHarvestStartTime = Time.time;
        handHarvestDuration = raiseTime;
        isAnimatingHandHarvest = true;
    }

    Vector3 CalculateHandHarvestLocalStart()
    {
        Vector3 localStart = Vector3.zero;
        bool hasFacing = handHarvestFacing.sqrMagnitude > 0.0001f;

        if (hasFacing)
        {
            Vector3 forwardWorld = new Vector3(handHarvestFacing.x, handHarvestFacing.y, 0f);
            Vector3 localForward = transform.InverseTransformVector(forwardWorld);
            localForward.z = 0f;

            float distance = Mathf.Max(0f, handHarvestForwardDistance);
            if (localForward.sqrMagnitude > 0.0001f && distance > 0f)
            {
                localForward = localForward.normalized * distance;
            }
            else
            {
                localForward = Vector3.zero;
            }

            localStart = localForward + (Vector3)handHarvestStartOffset;
        }
        else
        {
            localStart = transform.InverseTransformPoint(handHarvestWorldStart);
            localStart += (Vector3)handHarvestStartOffset;
        }

        localStart.z = 0f;

        float targetY = displayOffset.y;
        float minTargetY = targetY - Mathf.Max(0.01f, handHarvestMinLift);
        if (localStart.y > minTargetY)
        {
            localStart.y = minTargetY;
        }

        float clampX = Mathf.Max(0.01f, handHarvestMaxHorizontal);
        localStart.x = Mathf.Clamp(localStart.x, -clampX, clampX);

        return localStart;
    }

    void UpdateHandHarvestAnimation()
    {
        if (!isAnimatingHandHarvest || !iconRenderer)
            return;

        float elapsed = Time.time - handHarvestStartTime;
        float t = handHarvestDuration <= 0.0001f ? 1f : Mathf.Clamp01(elapsed / handHarvestDuration);
        float curved = handHarvestLiftCurve != null ? Mathf.Clamp01(handHarvestLiftCurve.Evaluate(t)) : t;
        Vector3 pos = Vector3.Lerp(handHarvestStartLocal, (Vector3)displayOffset, curved);
        iconRenderer.transform.localPosition = pos;

        if (t >= 0.999f)
        {
            iconRenderer.transform.localPosition = displayOffset;
            isAnimatingHandHarvest = false;
        }
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