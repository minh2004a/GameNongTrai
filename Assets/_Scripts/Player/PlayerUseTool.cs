
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cho phép người chơi sử dụng các công cụ (ví dụ: cuốc đất) bằng chuột.
/// Hiện tại hỗ trợ cuốc trong bán kính 1 ô xung quanh người chơi và quay mặt theo hướng click.
/// </summary>
[RequireComponent(typeof(PlayerInventory))]
public class PlayerUseTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerInventory inventory;
    [SerializeField] PlayerController controller;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] SoilManager soilManager;
    [SerializeField] Rigidbody2D body;

    [Header("Range")]
    [SerializeField, Min(1)] int baseRangeTiles = 1;
    [SerializeField, Min(0)] int bonusRangeTiles = 0;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] float minToolCooldown = 0.15f;
    [SerializeField, Min(0.1f)] float toolFailSafeSeconds = 3f;

    static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    static readonly int VerticalHash = Animator.StringToHash("Vertical");
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int UseHoeHash = Animator.StringToHash("UseHoe");

    readonly List<Vector2Int> pendingCells = new();

    Camera cachedCamera;
    ItemSO activeTool;
    ToolType activeToolType = ToolType.None;
    Vector2 activeFacing = Vector2.down;
    bool toolLocked;
    float toolFailSafeTimer;
    float cooldownTimer;

    int TotalRangeTiles => Mathf.Max(1, baseRangeTiles + bonusRangeTiles);

    void Reset()
    {
        inventory = GetComponent<PlayerInventory>();
        controller = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!controller) controller = GetComponent<PlayerController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sprite)
        {
            sprite = controller ? controller.GetComponentInChildren<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }
        if (!body) body = GetComponent<Rigidbody2D>();
        cachedCamera = Camera.main;
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (toolLocked)
        {
            toolFailSafeTimer -= Time.deltaTime;
            if (toolFailSafeTimer <= 0f)
            {
                CancelToolUse();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginToolUse();
        }
    }

    public void SetBonusRange(int bonusTiles)
    {
        bonusRangeTiles = Mathf.Max(0, bonusTiles);
    }

    void TryBeginToolUse()
    {
        if (toolLocked || cooldownTimer > 0f) return;
        if (UIInputGuard.BlockInputNow()) return;

        var item = inventory ? inventory.CurrentItem : null;
        if (!item || item.category != ItemCategory.Tool) return;

        cachedCamera = cachedCamera ? cachedCamera : Camera.main;
        if (!cachedCamera) return;

        Vector3 mp = Input.mousePosition;
        Vector3 world3 = cachedCamera.ScreenToWorldPoint(mp);
        Vector2 clickWorld = new(world3.x, world3.y);

        SoilManager soil = GetSoilManager();
        if (!soil) return;

        Vector2Int playerCell = soil.WorldToCell(transform.position);
        Vector2Int targetCell = soil.WorldToCell(clickWorld);
        Vector2Int delta = targetCell - playerCell;

        if (!IsWithinRange(delta)) return;

        Vector2 facing = DetermineFacing(delta);

        pendingCells.Clear();
        BuildTargetCells(item.toolType, targetCell, facing, pendingCells);
        if (pendingCells.Count == 0) return;

        StartToolUse(item, facing);
    }

    bool IsWithinRange(Vector2Int delta)
    {
        int r = TotalRangeTiles;
        return !(delta == Vector2Int.zero) && Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) <= r;
    }

    Vector2 DetermineFacing(Vector2Int delta)
    {
        if (delta == Vector2Int.zero)
        {
            if (controller)
            {
                var f = controller.Facing4;
                if (f.sqrMagnitude > 0.0001f) return f;
            }
            return Vector2.down;
        }

        if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
        {
            return delta.y >= 0 ? Vector2.up : Vector2.down;
        }

        return delta.x >= 0 ? Vector2.right : Vector2.left;
    }

    void BuildTargetCells(ToolType type, Vector2Int anchorCell, Vector2 facing, List<Vector2Int> results)
    {
        results.Clear();

        switch (type)
        {
            case ToolType.Hoe:
                results.Add(anchorCell);
                break;
            default:
                break;
        }
    }

    void StartToolUse(ItemSO item, Vector2 facing)
    {
        activeTool = item;
        activeToolType = item.toolType;
        activeFacing = facing;
        toolLocked = true;
        toolFailSafeTimer = toolFailSafeSeconds;
        cooldownTimer = Mathf.Max(minToolCooldown, item ? item.cooldown : minToolCooldown);

        ApplyFacing();
        TriggerToolAnimation(activeToolType);
        LockMove(true);
    }

    void ApplyFacing()
    {
        if (controller) controller.ForceFace(activeFacing);
        if (animator)
        {
            animator.SetFloat(HorizontalHash, activeFacing.x);
            animator.SetFloat(VerticalHash, activeFacing.y);
            animator.SetFloat(SpeedHash, 0f);
        }
        if (sprite) sprite.flipX = activeFacing.x < 0f;
    }

    void TriggerToolAnimation(ToolType type)
    {
        if (!animator) return;

        switch (type)
        {
            case ToolType.Hoe:
                animator.ResetTrigger(UseHoeHash);
                animator.SetTrigger(UseHoeHash);
                break;
            default:
                animator.ResetTrigger(UseHoeHash);
                break;
        }
    }

    void LockMove(bool on)
    {
        if (controller)
        {
            controller.SetMoveLock(on);
        }
        if (body && on)
        {
            body.velocity = Vector2.zero;
        }
    }

    SoilManager GetSoilManager()
    {
        if (soilManager && soilManager.isActiveAndEnabled) return soilManager;
        soilManager = FindFirstObjectByType<SoilManager>();
        return soilManager;
    }

    void CancelToolUse()
    {
        if (!toolLocked) return;
        toolLocked = false;
        pendingCells.Clear();
        activeTool = null;
        activeToolType = ToolType.None;
        LockMove(false);
        controller?.ApplyPendingMove();
    }

    // Animation Event: đảm bảo Animator luôn giữ hướng khoá
    public void ApplyToolFacingLockFrame()
    {
        if (!toolLocked) return;
        ApplyFacing();
    }

    // Animation Event: thực thi tác dụng của công cụ
    public void Tool_DoHit()
    {
        if (!toolLocked || activeToolType == ToolType.None) return;

        switch (activeToolType)
        {
            case ToolType.Hoe:
                PerformHoeHit();
                break;
            default:
                break;
        }
    }

    void PerformHoeHit()
    {
        var soil = GetSoilManager();
        if (!soil) return;

        foreach (var cell in pendingCells)
        {
            soil.TryTillCell(cell);
        }
    }

    // Animation Event: kết thúc hành động
    public void Tool_End()
    {
        if (!toolLocked) return;

        toolLocked = false;
        pendingCells.Clear();
        activeTool = null;
        activeToolType = ToolType.None;
        LockMove(false);
        controller?.ApplyPendingMove();
    }
}
