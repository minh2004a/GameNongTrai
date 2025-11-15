
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
    static readonly int UseWateringHash = Animator.StringToHash("UseWatering");
    static readonly int UseAxeHash = Animator.StringToHash("UseAxe");

    readonly List<Vector2Int> pendingCells = new();
    readonly HashSet<PlantGrowth> wateredPlantsBuffer = new();
    readonly HashSet<Component> axeHitBuffer = new();
    bool checkedWateringTrigger;
    bool hasWateringTrigger;

    Camera cachedCamera;
    ItemSO activeTool;
    ToolType activeToolType = ToolType.None;
    Vector2 activeFacing = Vector2.down;
    Vector2 activeToolHitPoint;
    bool activeToolHasHitPoint;
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
        checkedWateringTrigger = false;
        hasWateringTrigger = false;
        activeToolHitPoint = Vector2.zero;
        activeToolHasHitPoint = false;
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
        checkedWateringTrigger = false;
        hasWateringTrigger = false;
        activeToolHitPoint = Vector2.zero;
        activeToolHasHitPoint = false;
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
    void LateUpdate()
    {
        // Đang vung tool thì cứ khóa hướng mỗi frame
        if (toolLocked)
        {
            UpdateFacingWhileLocked();
            ApplyFacing(); // ApplyFacing() đã gọi FaceDirection(activeFacing)
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

        Vector2 playerWorld = transform.position;
        Vector2Int playerCell = soil ? soil.WorldToCell(playerWorld) : Vector2Int.zero;
        Vector2Int requestedCell = soil ? soil.WorldToCell(clickWorld) : Vector2Int.zero;

        if (!TryResolveToolTarget(item, soil, playerWorld, clickWorld, playerCell, requestedCell, out var targetCell, out var facing, out var rangeTiles, out var hitPoint, out var hasHitPoint))
        {
            return;
        }

        pendingCells.Clear();
        if (ToolUsesCellTargets(item.toolType))
        {
            BuildTargetCells(item.toolType, targetCell, facing, pendingCells);
            if (pendingCells.Count == 0) return;
        }

        if (!TryConsumeToolCost(item.toolType))
        {
            pendingCells.Clear();
            return;
        }

        StartToolUse(item, facing, rangeTiles, hitPoint, hasHitPoint);
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

    bool TryResolveToolTarget(ItemSO item, SoilManager soil, Vector2 playerWorld, Vector2 clickWorld, Vector2Int playerCell, Vector2Int requestedCell, out Vector2Int targetCell, out Vector2 facing, out int rangeTiles, out Vector2 hitPoint, out bool hasHitPoint)
    {
        targetCell = requestedCell;
        facing = Vector2.down;
        rangeTiles = GetToolRangeTiles(item);
        hitPoint = clickWorld;
        hasHitPoint = false;

        Vector2Int delta = requestedCell - playerCell;
        Vector2 worldDelta = clickWorld - playerWorld;
        bool hasMouseDirection = worldDelta.sqrMagnitude > 0.0001f;
        Vector2 facingFromMouse = DetermineFacingFromWorld(worldDelta);
        Vector2Int facingDelta = DetermineFacingDelta(worldDelta);

        switch (item.toolType)
        {
            case ToolType.Hoe:
                if (!soil) return false;
                if (delta == Vector2Int.zero && hasMouseDirection)
                {
                    delta = facingDelta;
                }

                if (!IsHoeOffset(delta))
                {
                    targetCell = Vector2Int.zero;
                    return false;
                }

                targetCell = playerCell + delta;
                facing = facingFromMouse;
                rangeTiles = 1;
                hitPoint = soil.CellToWorld(targetCell);
                hasHitPoint = true;
                return true;
            case ToolType.WateringCan:
                if (!soil) return false;
                if (delta == Vector2Int.zero && hasMouseDirection)
                {
                    delta = facingDelta;
                }

                if (delta == Vector2Int.zero)
                {
                    targetCell = playerCell;
                    facing = facingFromMouse;
                    hitPoint = soil.CellToWorld(targetCell);
                    hasHitPoint = true;
                    return true;
                }

                if (!IsWithinRange(delta, rangeTiles))
                {
                    targetCell = Vector2Int.zero;
                    return false;
                }

                targetCell = playerCell + delta;
                facing = facingFromMouse;
                hitPoint = soil.CellToWorld(targetCell);
                hasHitPoint = true;
                return true;
            case ToolType.Axe:
            {
                facing = facingFromMouse;

                float tileSize = soil ? Mathf.Max(0.01f, soil.GridSize) : 1f;
                float maxDistance = Mathf.Max(tileSize, rangeTiles * tileSize);
                Vector2 clamped = Vector2.ClampMagnitude(worldDelta, maxDistance);
                if (clamped.sqrMagnitude <= 0.0001f)
                {
                    Vector2 fallbackFacing = facing.sqrMagnitude > 0.0001f ? facing : Vector2.down;
                    clamped = fallbackFacing.normalized * Mathf.Min(maxDistance, tileSize);
                }

                hitPoint = playerWorld + clamped;
                hasHitPoint = true;
                return true;
            }
            default:
                if (!IsWithinRange(delta, rangeTiles))
                {
                    targetCell = Vector2Int.zero;
                    return false;
                }

                facing = facingFromMouse;
                return true;
        }
    }

    bool IsHoeOffset(Vector2Int delta)
    {
        if (delta == Vector2Int.zero) return false;
        return Mathf.Abs(delta.x) <= 1 && Mathf.Abs(delta.y) <= 1;
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

    Vector2Int DetermineFacingDelta(Vector2 worldDelta)
    {
        if (worldDelta.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(worldDelta.y) >= Mathf.Abs(worldDelta.x))
            {
                return worldDelta.y >= 0f ? Vector2Int.up : Vector2Int.down;
            }

            return worldDelta.x >= 0f ? Vector2Int.right : Vector2Int.left;
        }

        Vector2 fallback = DetermineFallbackFacing();
        int fx = Mathf.Clamp(Mathf.RoundToInt(fallback.x), -1, 1);
        int fy = Mathf.Clamp(Mathf.RoundToInt(fallback.y), -1, 1);
        if (fx == 0 && fy == 0)
        {
            fy = -1;
        }

        return new Vector2Int(fx, fy);
    }

    Vector2 DetermineFacingFromWorld(Vector2 worldDelta)
    {
        Vector2Int delta = DetermineFacingDelta(worldDelta);
        return DetermineFacing(delta);
    }

    Vector2 DetermineFallbackFacing()
    {
        if (!controller) return Vector2.down;

        Vector2 pending = controller.PendingFacing4();
        if (pending.sqrMagnitude > 0.0001f) return pending;

        Vector2 facing = controller.Facing4;
        if (facing.sqrMagnitude > 0.0001f) return facing;

        return Vector2.down;
    }

    void BuildTargetCells(ToolType type, Vector2Int anchorCell, Vector2 facing, List<Vector2Int> results)
    {
        results.Clear();

        switch (type)
        {
            case ToolType.Hoe:
                results.Add(anchorCell);
                break;
            case ToolType.WateringCan:
                results.Add(anchorCell);
                break;
            default:
                break;
        }
    }

    bool ToolUsesCellTargets(ToolType type)
    {
        return type == ToolType.Hoe || type == ToolType.WateringCan;
    }

    bool TryConsumeToolCost(ToolType toolType)
    {
        if (!stamina) return true;

        float cost = 0f;
        switch (toolType)
        {
            case ToolType.Axe:
                cost = stamina.axeCost;
                break;
            case ToolType.Hoe:
                cost = stamina.hoeCost;
                break;
            case ToolType.WateringCan:
                cost = stamina.wateringCost;
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

    void StartToolUse(ItemSO item, Vector2 facing, int rangeTiles, Vector2 hitPoint, bool hasHitPoint)
    {
        activeTool = item;
        activeToolType = item.toolType;
        activeFacing = facing;
        activeToolRangeTiles = rangeTiles;
        activeToolHitPoint = hitPoint;
        activeToolHasHitPoint = hasHitPoint;
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

    void UpdateFacingWhileLocked()
    {
        if (!controller) return;

        Vector2 pending = controller.PendingFacing4();
        if (pending.sqrMagnitude > 0.0001f)
        {
            activeFacing = pending;
        }
    }

    void TriggerToolAnimation(ToolType type)
    {
        if (!animator) return;

        switch (type)
        {
            case ToolType.Axe:
                animator.ResetTrigger(UseAxeHash);
                animator.ResetTrigger(UseWateringHash);
                animator.ResetTrigger(UseHoeHash);
                animator.SetTrigger(UseAxeHash);
                break;
            case ToolType.Hoe:
                animator.ResetTrigger(UseHoeHash);
                animator.SetTrigger(UseHoeHash);
                break;
            case ToolType.WateringCan:
                animator.ResetTrigger(UseWateringHash);
                animator.ResetTrigger(UseHoeHash);
                if (AnimatorSupportsWateringTrigger())
                {
                    animator.SetTrigger(UseWateringHash);
                }
                else
                {
                    animator.SetTrigger(UseHoeHash);
                }
                break;
            default:
                animator.ResetTrigger(UseAxeHash);
                animator.ResetTrigger(UseHoeHash);
                break;
        }
    }

    bool AnimatorSupportsWateringTrigger()
    {
        if (!animator) return false;
        if (!checkedWateringTrigger)
        {
            hasWateringTrigger = false;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == UseWateringHash)
                {
                    hasWateringTrigger = true;
                    break;
                }
            }
            checkedWateringTrigger = true;
        }
        return hasWateringTrigger;
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
        activeToolHasHitPoint = false;
        activeToolHitPoint = Vector2.zero;
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
            case ToolType.Axe:
                PerformAxeHit();
                break;
            case ToolType.Hoe:
                PerformHoeHit();
                break;
            case ToolType.WateringCan:
                PerformWatering();
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

    void PerformAxeHit()
    {
        if (!activeTool) return;

        float radius = Mathf.Max(0.05f, activeTool.range) * Mathf.Max(0.05f, activeTool.hitboxScale);
        Vector2 center = activeToolHasHitPoint ? activeToolHitPoint : (Vector2)transform.position;
        if (!activeToolHasHitPoint)
        {
            Vector2 forward = activeFacing.sqrMagnitude > 0.0001f ? activeFacing.normalized : Vector2.down;
            float forwardDist = activeTool.hitboxForward >= 0f ? activeTool.hitboxForward : Mathf.Max(0.1f, radius);
            center += forward * forwardDist;
        }
        center += new Vector2(0f, activeTool.hitboxYOffset);

        var hits = Physics2D.OverlapCircleAll(center, radius);
        if (hits == null || hits.Length == 0) return;

        axeHitBuffer.Clear();
        int damage = Mathf.Max(1, activeTool.Dame);
        Vector2 pushDir = activeFacing.sqrMagnitude > 0.0001f ? activeFacing.normalized : Vector2.down;

        foreach (var hit in hits)
        {
           if (!hit || hit.isTrigger) continue;

            var tree = hit.GetComponentInParent<TreeChopTarget>();
            if (tree && axeHitBuffer.Add(tree))
            {
                tree.ApplyDamage(damage, pushDir);
                continue;
            }

            var behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (!behaviour) continue;
                if (!axeHitBuffer.Add(behaviour)) continue;

                if (behaviour is IChoppable choppable)
                {
                    choppable.Chop(damage, pushDir);
                    break;
                }

                if (behaviour is IDamageable damageable)
                {
                    damageable.TakeHit(damage);
                    break;
                }
            }
        }
    }

    void PerformWatering()
    {
        var soil = GetSoilManager();
        if (!soil) return;

        wateredPlantsBuffer.Clear();

        foreach (var cell in pendingCells)
        {
            soil.TryWaterCell(cell);
            WaterPlantsNearCell(soil, cell);
        }
    }

    void WaterPlantsNearCell(SoilManager soil, Vector2Int cell)
    {
        Vector2 center = soil.CellToWorld(cell);
        float radius = Mathf.Max(0.1f, soil.GridSize * 0.45f);
        bool found = false;

        var hits = Physics2D.OverlapCircleAll(center, radius);
        if (hits != null)
        {
            foreach (var hit in hits)
            {
                if (!hit) continue;
                var plant = hit.GetComponentInParent<PlantGrowth>();
                if (!plant) continue;
                if (wateredPlantsBuffer.Add(plant))
                {
                    plant.Water();
                }
                found = true;
            }
        }

        if (found) return;

        float sqrRadius = radius * radius;
#if UNITY_2023_1_OR_NEWER
        var allPlants = FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
#else
        var allPlants = FindObjectsOfType<PlantGrowth>();
#endif
        foreach (var plant in allPlants)
        {
            if (!plant) continue;
            if (((Vector2)plant.transform.position - center).sqrMagnitude > sqrRadius) continue;
            if (!wateredPlantsBuffer.Add(plant)) continue;
            plant.Water();
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
        activeToolHasHitPoint = false;
        activeToolHitPoint = Vector2.zero;
        LockMove(false);
        controller?.ApplyPendingMove();
        if (animator) animator.speed = 1f;
    }
}