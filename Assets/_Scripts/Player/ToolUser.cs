
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
    [SerializeField] float toolFailSafe = 3f;
    float toolTimer;
    ItemSO usingItem;
    float nextUseTime;

    Vector2 aimDir = Vector2.right;
    float ActionTimeMult() => (stamina && stamina.IsExhausted) ? exhaustedActionTimeMult : 1f;
    float AnimSpeedMult()   => (stamina && stamina.IsExhausted) ? exhaustedAnimSpeedMult : 1f;
    void Awake()
    {
        if (!pc) pc = GetComponent<PlayerController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
    }

    void Reset(){ anim = GetComponentInChildren<Animator>(); pc = GetComponent<PlayerController>(); soilManager = FindFirstObjectByType<SoilManager>(); }

    void Update()
    {
        if (toolLocked)
        {
            // ép Animator giữ hướng, đứng yên
            anim.SetFloat("Horizontal", toolFacing.x);
            anim.SetFloat("Vertical", toolFacing.y);
            anim.SetFloat("Speed", 0f);
            toolTimer -= Time.deltaTime;
            if (toolTimer <= 0f) Tool_End(); // failsafe nếu quên Animation Event
        }
        else
        {
            var d = new Vector2(anim.GetFloat("Horizontal"), anim.GetFloat("Vertical"));
            if (d.sqrMagnitude > 0.001f) aimDir = d.normalized;
        }

        var dir = toolLocked ? toolFacing : aimDir;                 // dùng hướng đã khóa khi chặt
        float fwd = usingItem ? (usingItem.hitboxForward >= 0f ? usingItem.hitboxForward : originDist)
                      : originDist;
        if (hitOrigin) hitOrigin.localPosition = (Vector3)(dir * fwd);
    }
    void LateUpdate()
    {
        if (!toolLocked) return;
        // fallback: nếu script khác đổi sau Animator, vẫn ép lại ở LateUpdate
        anim.SetFloat("Horizontal", toolFacing.x);
        anim.SetFloat("Vertical", toolFacing.y);
        anim.SetFloat("Speed", 0f);
    }

    public Vector2 ToolFacing => toolFacing; // để SMB đọc
    public void OnUse(InputValue v)
    {
        if (!v.isPressed) return;
        if (UIInputGuard.BlockInputNow()) return;   // <— THÊM DÒNG NÀY
        Vector2 mouseW = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 toMouse = mouseW - (Vector2)transform.position;
        if (toMouse.sqrMagnitude > 1e-4f)
        {
            Vector2 face = Facing4FromVector(toMouse);
            aimDir = face;
            pc?.ForceFace(face);
        }
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
        anim.SetFloat("Horizontal", toolFacing.x);
        anim.SetFloat("Vertical",   toolFacing.y);
        anim.SetFloat("Speed",      0f);
        pc?.SetMoveLock(true);                    // nếu PlayerController có hàm này]
        if (anim)
        {
            switch (usingItem.toolType)
            {
                case ToolType.Axe:
                    anim.ResetTrigger("UseHoe");
                    anim.SetTrigger("UseAxe");
                    break;
                case ToolType.Hoe:
                    anim.ResetTrigger("UseAxe");
                    anim.SetTrigger("UseHoe");
                    break;
                case ToolType.WateringCan:
                    anim.ResetTrigger("UseAxe");
                    anim.ResetTrigger("UseHoe");
                    anim.SetTrigger("UseTool");
                    break;
                default:
                    anim.SetTrigger("UseTool");
                    break;
            }
        }
    }

    public void Tool_DoHit()
    {
        if (!usingItem) return;

        Vector2 dir = toolLocked ? toolFacing : aimDir;

        float fwd = (usingItem.hitboxForward >= 0f) ? usingItem.hitboxForward : originDist;
        Vector3 center = (Vector2)transform.position + dir * fwd;
        center += new Vector3(0f, usingItem.hitboxYOffset, 0f);

        float r = Mathf.Max(0.01f, usingItem.range) * Mathf.Max(0.01f, usingItem.hitboxScale);

        var cols = Physics2D.OverlapCircleAll(center, r, hitMask);
        if (usingItem.toolType == ToolType.WateringCan)
        {
            if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
            var watered = new HashSet<PlantGrowth>();
            HashSet<Vector2Int> hydratedCells = soilManager ? new HashSet<Vector2Int>() : null;
            foreach (var c in cols)
            {
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

        foreach (var c in cols)
        {
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
        float x = anim.GetFloat("Horizontal"), y = anim.GetFloat("Vertical");
        if (Mathf.Abs(x) >= Mathf.Abs(y)) return x >= 0 ? Vector2.right : Vector2.left;
        return y >= 0 ? Vector2.up : Vector2.down;
    }
    Vector2 Facing4FromVector(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 1e-4f) return Facing4FromAnim();
        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? (dir.x >= 0 ? Vector2.right : Vector2.left)
            : (dir.y >= 0 ? Vector2.up : Vector2.down);
    }
    public void ApplyToolFacingLockFrame()
    {
        if (!anim) return;
        anim.SetFloat("Horizontal", toolFacing.x);
        anim.SetFloat("Vertical", toolFacing.y);
        anim.SetFloat("Speed", 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        var it = inv ? inv.CurrentItem : null;
        if (!it) return;

        Vector2 dir = toolLocked ? toolFacing : aimDir;
        float fwd = (it.hitboxForward >= 0f) ? it.hitboxForward : originDist;

        Vector3 center = (Vector2)transform.position + dir * fwd;
        center += new Vector3(0f, it.hitboxYOffset, 0f);
        float r = Mathf.Max(0.01f, it.range) * Mathf.Max(0.01f, it.hitboxScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, r);
    }
}
