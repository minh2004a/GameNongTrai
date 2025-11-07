
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// Quản lý việc sử dụng công cụ của người chơi, bao gồm hướng và va chạm
public class ToolUser : MonoBehaviour
{
    [SerializeField] PlayerStamina stamina;
    [SerializeField] PlayerInventory inv;
    [SerializeField] Animator anim;
    [SerializeField] Transform hitOrigin;
    [SerializeField] LayerMask hitMask;
    [SerializeField] PlayerController pc;
    [SerializeField] SleepManager sleep;
    [SerializeField] float originDist = 0.55f;
    [SerializeField] float exhaustedActionTimeMult = 1.6f;
    [SerializeField, Range(0.1f,1f)] float exhaustedAnimSpeedMult = 0.7f;
    [SerializeField] SoilManager soilManager;
    bool toolLocked;
    Vector2 toolFacing = Vector2.down;
    Vector2 appliedAnimFacing = Vector2.down;
    [SerializeField] float toolFailSafe = 3f;
    float toolTimer;
    ItemSO usingItem;
    float nextUseTime;

    Vector2 aimDir = Vector2.right;
    [SerializeField] Camera worldCamera;
    static readonly int UseAxeTrigger = Animator.StringToHash("UseAxe");
    static readonly int UseHoeTrigger = Animator.StringToHash("UseHoe");
    static readonly int UseToolTrigger = Animator.StringToHash("UseTool");
    static readonly Collider2D[] HitBuffer = new Collider2D[32];
    float ActionTimeMult() => (stamina && stamina.IsExhausted) ? exhaustedActionTimeMult : 1f;
    float AnimSpeedMult()   => (stamina && stamina.IsExhausted) ? exhaustedAnimSpeedMult : 1f;
    void Awake()
    {
        if (!pc) pc = GetComponent<PlayerController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
        if (!worldCamera) worldCamera = Camera.main;
    }

    void Reset()
    {
        anim = GetComponentInChildren<Animator>();
        pc = GetComponent<PlayerController>();
        soilManager = FindFirstObjectByType<SoilManager>();
        worldCamera = Camera.main;
    }

    void Update()
    {
        if (toolLocked)
        {
            // ép Animator giữ hướng, đứng yên
            if (appliedAnimFacing != toolFacing)
            {
                TopDownAnimatorUtility.ApplyFacing(anim, toolFacing);
                appliedAnimFacing = toolFacing;
            }
            TopDownAnimatorUtility.ApplySpeed(anim, 0f);
            toolTimer -= Time.deltaTime;
            if (toolTimer <= 0f) Tool_End(); // failsafe nếu quên Animation Event
        }
        else
        {
            var d = new Vector2(anim.GetFloat(TopDownAnimatorUtility.HorizontalHash),
                                anim.GetFloat(TopDownAnimatorUtility.VerticalHash));
            if (d.sqrMagnitude > 0.001f)
                aimDir = d.normalized;
        }

        var dir = toolLocked ? toolFacing : aimDir;                 // dùng hướng đã khóa khi chặt
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();
        else
            dir = toolFacing;
        float fwd = usingItem ? (usingItem.hitboxForward >= 0f ? usingItem.hitboxForward : originDist)
                      : originDist;
        if (hitOrigin) hitOrigin.localPosition = (Vector3)(dir * fwd);
    }
    void LateUpdate()
    {
        if (!toolLocked) return;
        // fallback: nếu script khác đổi sau Animator, vẫn ép lại ở LateUpdate
        if (appliedAnimFacing != toolFacing)
        {
            TopDownAnimatorUtility.ApplyFacing(anim, toolFacing);
            appliedAnimFacing = toolFacing;
        }
        TopDownAnimatorUtility.ApplySpeed(anim, 0f);
    }

    public Vector2 ToolFacing => toolFacing; // để SMB đọc
    public void OnUse(InputValue v)
    {
        if (!v.isPressed) return;
        if (UIInputGuard.BlockInputNow()) return;   // <— THÊM DÒNG NÀY
        if (!worldCamera) worldCamera = Camera.main;
        Vector2 mouseW = worldCamera ? worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                                     : Mouse.current.position.ReadValue();
        Vector2 toMouse = mouseW - (Vector2)transform.position;
        if (toMouse.sqrMagnitude > 0.0001f)
            aimDir = toMouse.normalized;
        TryUseCurrent();                    // click xa → giữ hướng cũ như trước
    }

    public void TryUseCurrent(){
        var it = inv?.CurrentItem;
        if (!it || it.category != ItemCategory.Tool) return;
        switch (it.toolType)
        {
            case ToolType.Axe:
            case ToolType.Hoe:
            case ToolType.WateringCan:
                break;
            default:
                return;
        }
        if (Time.time < nextUseTime) return;
        if (!stamina) return;
        var r = stamina.SpendExhaustible(stamina.toolCost);
        if (r == PlayerStamina.SpendResult.Fainted){ sleep.FaintNow(); return; }
        // nếu Exhausted thì vẫn tiếp tục chặt, anim/cooldown đã chậm theo ActionTimeMult()
        usingItem = it;
        nextUseTime = Time.time + Mathf.Max(0.05f, it.cooldown) * ActionTimeMult();
        toolLocked = true;
        toolTimer = toolFailSafe * ActionTimeMult();
        if (anim) anim.speed = AnimSpeedMult();
        toolFacing = Facing4FromAnim();           // chốt hướng
        TopDownAnimatorUtility.ApplyFacing(anim, toolFacing);
        appliedAnimFacing = toolFacing;
        TopDownAnimatorUtility.ApplySpeed(anim, 0f);
        pc?.SetMoveLock(true);                    // nếu PlayerController có hàm này
        if (anim)
        {
            switch (usingItem.toolType)
            {
                case ToolType.Axe:
                    anim.ResetTrigger(UseHoeTrigger);
                    anim.SetTrigger(UseAxeTrigger);
                    break;
                case ToolType.Hoe:
                    anim.ResetTrigger(UseAxeTrigger);
                    anim.SetTrigger(UseHoeTrigger);
                    break;
                case ToolType.WateringCan:
                    anim.ResetTrigger(UseAxeTrigger);
                    anim.ResetTrigger(UseHoeTrigger);
                    anim.SetTrigger(UseToolTrigger);
                    break;
                default:
                    anim.SetTrigger(UseToolTrigger);
                    break;
            }
        }
    }

    public void Tool_DoHit()
    {
        if (!usingItem) return;

        Vector2 dir = toolLocked ? toolFacing : aimDir;
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();
        else
            dir = toolFacing;

        float fwd = (usingItem.hitboxForward >= 0f) ? usingItem.hitboxForward : originDist;
        Vector3 center = (Vector2)transform.position + dir * fwd;
        center += new Vector3(0f, usingItem.hitboxYOffset, 0f);

        float r = Mathf.Max(0.01f, usingItem.range) * Mathf.Max(0.01f, usingItem.hitboxScale);

        int hitCount = Physics2D.OverlapCircleNonAlloc(center, r, HitBuffer, hitMask);
        Collider2D[] overflow = null;
        if (hitCount >= HitBuffer.Length)
        {
            overflow = Physics2D.OverlapCircleAll(center, r, hitMask);
        }

        if (usingItem.toolType == ToolType.WateringCan)
        {
            if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
            var watered = new HashSet<PlantGrowth>();
            HashSet<Vector2Int> hydratedCells = soilManager ? new HashSet<Vector2Int>() : null;
            int total = overflow != null ? overflow.Length : hitCount;
            for (int i = 0; i < total; i++)
            {
                var c = overflow != null ? overflow[i] : HitBuffer[i];
                if (!c) continue;
                var plant = c.GetComponentInParent<PlantGrowth>();
                if (!plant) continue;
                if (watered.Add(plant))
                {
                    plant.Water();
                    if (hydratedCells != null)
                    {
                        hydratedCells.Add(soilManager.WorldToCell(plant.transform.position));
                    }
                }
            }

            if (hydratedCells != null)
            {
                hydratedCells.Add(soilManager.WorldToCell((Vector2)center));
                foreach (var cell in hydratedCells)
                {
                    soilManager.TryWaterCell(cell);
                }
            }
            return;
        }

        int targetCount = overflow != null ? overflow.Length : hitCount;
        for (int i = 0; i < targetCount; i++)
        {
            var c = overflow != null ? overflow[i] : HitBuffer[i];
            if (!c) continue;
            var t = c.GetComponent<IToolTarget>();
            if (t != null)
            {
                Vector2 pushDir = (c.transform.position - transform.position).normalized;
                t.Hit(usingItem.toolType, usingItem.Dame, pushDir);
            }
        }

        if (usingItem.toolType == ToolType.Hoe)
        {
            if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
            if (soilManager)
            {
                var cell = soilManager.WorldToCell((Vector2)center);
                soilManager.TryTillCell(cell);
            }
        }
    }

    public void Tool_End()
    {   
        if (anim) anim.speed = 1f;
        usingItem = null;
        if (!toolLocked) return;
        toolLocked = false;
        pc?.SetMoveLock(false);
    }
    Vector2 Facing4FromAnim()
    {
        float x = anim.GetFloat(TopDownAnimatorUtility.HorizontalHash);
        float y = anim.GetFloat(TopDownAnimatorUtility.VerticalHash);
        var snapped = TopDownAnimatorUtility.SnapToCardinal(new Vector2(x, y));
        return snapped.sqrMagnitude > 0.0001f ? snapped : toolFacing;
    }
    public void ApplyToolFacingLockFrame()
    {
        if (!anim) return;
        TopDownAnimatorUtility.ApplyFacing(anim, toolFacing);
        TopDownAnimatorUtility.ApplySpeed(anim, 0f);
        appliedAnimFacing = toolFacing;
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        var it = inv ? inv.CurrentItem : null;
        if (!it) return;

        Vector2 dir = toolLocked ? toolFacing : aimDir;
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();
        else
            dir = toolFacing;
        float fwd = (it.hitboxForward >= 0f) ? it.hitboxForward : originDist;

        Vector3 center = (Vector2)transform.position + dir * fwd;
        center += new Vector3(0f, it.hitboxYOffset, 0f);
        float r = Mathf.Max(0.01f, it.range) * Mathf.Max(0.01f, it.hitboxScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, r);
    }
}
