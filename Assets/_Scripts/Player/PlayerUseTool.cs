

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
    [SerializeField] PlayerStamina stamina;
    [SerializeField] SleepManager sleep;

    [Header("Range")]
    [SerializeField, Min(1)] int baseRangeTiles = 1;
    [SerializeField, Min(0)] int bonusRangeTiles = 0;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] float minToolCooldown = 0.15f;
    [SerializeField, Min(0.1f)] float toolFailSafeSeconds = 3f;
    [SerializeField] float exhaustedActionTimeMult = 1.5f;
    [SerializeField, Range(0.1f, 1f)] float exhaustedAnimSpeedMult = 0.7f;

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
    int activeToolRangeTiles = 1;

    public int CurrentToolRangeTiles => activeToolRangeTiles;

    void Reset()
    {
        inventory = GetComponent<PlayerInventory>();
        controller = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        stamina = GetComponent<PlayerStamina>();
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
        if (!stamina) stamina = GetComponent<PlayerStamina>();
        if (!sleep) sleep = FindFirstObjectByType<SleepManager>();
        cachedCamera = Camera.main;
        activeToolRangeTiles = Mathf.Max(1, baseRangeTiles);
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
        activeToolRangeTiles = GetToolRangeTiles(activeTool);
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

        Vector2 playerWorld = transform.position;
        Vector2Int playerCell = soil.WorldToCell(playerWorld);
        Vector2Int requestedCell = soil.WorldToCell(clickWorld);

        if (!TryResolveToolTarget(item, playerWorld, clickWorld, playerCell, requestedCell, out var targetCell, out var facing, out var rangeTiles))
        {
            return;
        }

        pendingCells.Clear();
        BuildTargetCells(item.toolType, targetCell, facing, pendingCells);
        if (pendingCells.Count == 0) return;

        if (!TryConsumeToolCost(item.toolType))
        {
            pendingCells.Clear();
            return;
        }

        StartToolUse(item, facing, rangeTiles);
    }

    int GetToolRangeTiles(ItemSO item)
    {
        int baseTiles = baseRangeTiles;
        if (item && item.toolRangeTiles > 0)
        {
            baseTiles = item.toolRangeTiles;
        }

        return Mathf.Max(1, baseTiles + bonusRangeTiles);
    }

    bool IsWithinRange(Vector2Int delta, int rangeTiles)
    {
        return !(delta == Vector2Int.zero) && Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) <= rangeTiles;
    }

    bool TryResolveToolTarget(ItemSO item, Vector2 playerWorld, Vector2 clickWorld, Vector2Int playerCell, Vector2Int requestedCell, out Vector2Int targetCell, out Vector2 facing, out int rangeTiles)
    {
        targetCell = requestedCell;
        facing = Vector2.down;
        rangeTiles = GetToolRangeTiles(item);

        Vector2Int delta = requestedCell - playerCell;

        switch (item.toolType)
        {
            case ToolType.Hoe:
                if (delta == Vector2Int.zero)
                {
                    delta = DetermineFacingDelta(clickWorld - playerWorld);
                }

                if (!IsHoeOffset(delta))
                {
                    targetCell = Vector2Int.zero;
                    return false;
                }

                targetCell = playerCell + delta;
                facing = DetermineFacing(delta);
                rangeTiles = 1;
                return true;
            default:
                if (!IsWithinRange(delta, rangeTiles))
                {
                    targetCell = Vector2Int.zero;
                    return false;
                }

                facing = DetermineFacing(delta);
                return true;
        }
    }

    bool IsHoeOffset(Vector2Int delta)
    {
        if (delta == Vector2Int.zero) return false;
        return Mathf.Abs(delta.x) <= 1 && Mathf.Abs(delta.y) <= 1;
    }

    Vector2Int DetermineFacingDelta(Vector2 worldDelta)
    {
        if (worldDelta.sqrMagnitude <= 0.0001f) return Vector2Int.zero;

        if (Mathf.Abs(worldDelta.y) >= Mathf.Abs(worldDelta.x))
        {
            return worldDelta.y >= 0f ? Vector2Int.up : Vector2Int.down;
        }

        return worldDelta.x >= 0f ? Vector2Int.right : Vector2Int.left;
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

    bool TryConsumeToolCost(ToolType toolType)
    {
        if (!stamina) return true;

        float cost = 0f;
        switch (toolType)
        {
            case ToolType.Hoe:
                cost = stamina.hoeCost;
                break;
        }

        if (cost <= 0f) return true;

        var result = stamina.SpendExhaustible(cost);
        if (result == PlayerStamina.SpendResult.Fainted)
        {
            sleep?.FaintNow();
            return false;
        }

        return true;
    }

    float ActionTimeMult() => (stamina && stamina.IsExhausted) ? exhaustedActionTimeMult : 1f;
    float AnimSpeedMult() => (stamina && stamina.IsExhausted) ? exhaustedAnimSpeedMult : 1f;

    void StartToolUse(ItemSO item, Vector2 facing, int rangeTiles)
    {
        activeTool = item;
        activeToolType = item.toolType;
        activeFacing = facing;
        activeToolRangeTiles = rangeTiles;
        toolLocked = true;
        toolFailSafeTimer = toolFailSafeSeconds * ActionTimeMult();
        cooldownTimer = Mathf.Max(minToolCooldown, item ? item.cooldown : minToolCooldown) * ActionTimeMult();

        LockMove(true);
        FaceDirection(activeFacing);
        TriggerToolAnimation(activeToolType);
        if (animator) animator.speed = AnimSpeedMult();
    }

    void FaceDirection(Vector2 facing)
    {
        if (controller) controller.ForceFace(facing);
        if (animator)
        {
            animator.SetFloat(HorizontalHash, facing.x);
            animator.SetFloat(VerticalHash, facing.y);
            animator.SetFloat(SpeedHash, 0f);
        }
        if (sprite) sprite.flipX = facing.x < 0f;
    }

    void ApplyFacing()
    {
        FaceDirection(activeFacing);
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
        activeToolRangeTiles = Mathf.Max(1, baseRangeTiles);
        LockMove(false);
        controller?.ApplyPendingMove();
        if (animator) animator.speed = 1f;
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
        activeToolRangeTiles = Mathf.Max(1, baseRangeTiles);
        LockMove(false);
        controller?.ApplyPendingMove();
        if (animator) animator.speed = 1f;
    }
}
