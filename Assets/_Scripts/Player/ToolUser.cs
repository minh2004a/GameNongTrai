
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// Quản lý việc sử dụng công cụ của người chơi, bao gồm hướng và va chạm
public class ToolUser : MonoBehaviour
{
    [SerializeField] PlayerStamina stamina;
    [SerializeField] PlayerInventory inv;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
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
    Vector2 requestedFacing = Vector2.zero;
    float ActionTimeMult() => (stamina && stamina.IsExhausted) ? exhaustedActionTimeMult : 1f;
    float AnimSpeedMult()   => (stamina && stamina.IsExhausted) ? exhaustedAnimSpeedMult : 1f;
    void Awake()
    {
        if (!pc) pc = GetComponent<PlayerController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
    }

    void Reset()
    {
        anim = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        pc = GetComponent<PlayerController>();
        soilManager = FindFirstObjectByType<SoilManager>();
    }

    void Update()
    {
        if (toolLocked)
        {
            // ép Animator giữ hướng, đứng yên
            ApplyAnimatorFacing(toolFacing, true);
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
        ApplyAnimatorFacing(toolFacing, true);
    }

    public Vector2 ToolFacing => toolFacing; // để SMB đọc
    public void OnUse(InputValue v)
    {
        if (!v.isPressed) return;
        if (UIInputGuard.BlockInputNow()) return;
        Vector2 face = MouseFacing4();
        if (face.sqrMagnitude <= 1e-4f)
        {
            face = pc ? pc.Facing4 : Facing4FromAnim();
            if (face.sqrMagnitude <= 1e-4f) face = toolFacing;
        }
        aimDir = face;
        requestedFacing = face;
        ForceFace(face);
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
        var lockDir = DetermineToolFacing();
        toolFacing = Facing4FromVector(lockDir);           // chốt hướng
        aimDir = toolFacing;
        requestedFacing = Vector2.zero;
        toolLocked = true;
        toolTimer = toolFailSafe * ActionTimeMult();
        if (anim) anim.speed = AnimSpeedMult();
        ForceFace(toolFacing);
        ApplyAnimatorFacing(toolFacing, true);
        pc?.SetMoveLock(true);                    // nếu PlayerController có hàm này
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
        pc?.ApplyPendingMove();
        Vector2 face = pc ? pc.Facing4 : toolFacing;
        ForceFace(face);
        toolFacing = face;
        aimDir = face;
    }
    Vector2 Facing4FromAnim()
    {
        if (!anim) return toolFacing;
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
    Vector2 MouseFacing4()
    {
        if (!Camera.main || Mouse.current == null) return Vector2.zero;
        Vector3 mp = Mouse.current.position.ReadValue();
        Vector3 mw = Camera.main.ScreenToWorldPoint(mp);
        mw.z = 0f;
        Vector2 delta = (Vector2)mw - (Vector2)transform.position;
        if (delta.sqrMagnitude <= 1e-4f) return Vector2.zero;
        return Facing4FromVector(delta);
    }
    Vector2 DetermineToolFacing()
    {
        if (requestedFacing.sqrMagnitude > 1e-4f) return requestedFacing;
        var mouseFace = MouseFacing4();
        if (mouseFace.sqrMagnitude > 1e-4f) return mouseFace;
        if (pc)
        {
            var f = pc.Facing4;
            if (f.sqrMagnitude > 1e-4f) return f;
        }
        var animFace = Facing4FromAnim();
        if (animFace.sqrMagnitude > 1e-4f) return animFace;
        return toolFacing;
    }
    void ForceFace(Vector2 face)
    {
        if (face.sqrMagnitude <= 1e-4f) return;
        if (pc)
        {
            pc.ForceFace(face);
        }
        else
        {
            ApplyAnimatorFacing(face, false);
        }
    }
    void ApplyAnimatorFacing(Vector2 face, bool zeroSpeed)
    {
        if (anim)
        {
            anim.SetFloat("Horizontal", face.x);
            anim.SetFloat("Vertical", face.y);
            if (zeroSpeed) anim.SetFloat("Speed", 0f);
        }
        if (sprite)
        {
            sprite.flipX = face.x < 0f;
        }
    }
    public void ApplyToolFacingLockFrame()
    {
        ApplyAnimatorFacing(toolFacing, true);
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
