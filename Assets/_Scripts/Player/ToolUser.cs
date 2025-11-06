
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
    readonly Dictionary<ToolType, IToolActionHandler> toolHandlers = new();
    IToolActionHandler currentHandler;

    Vector2 aimDir = Vector2.right;
    float ActionTimeMult() => (stamina && stamina.IsExhausted) ? exhaustedActionTimeMult : 1f;
    float AnimSpeedMult()   => (stamina && stamina.IsExhausted) ? exhaustedAnimSpeedMult : 1f;
    void Awake()
    {
        if (!pc) pc = GetComponent<PlayerController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
        InitializeToolHandlers();
    }

    void Reset(){ anim = GetComponentInChildren<Animator>(); pc = GetComponent<PlayerController>(); soilManager = FindFirstObjectByType<SoilManager>(); InitializeToolHandlers(); }

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
        TryUseCurrent();                    // click xa → giữ hướng cũ như trước
    }

    public void TryUseCurrent(){
        var it = inv?.CurrentItem;
        if (!it || it.category != ItemCategory.Tool) return;
        if (!toolHandlers.TryGetValue(it.toolType, out var handler)) return;
        if (!handler.CanBeginUse(this, it)) return;
        if (Time.time < nextUseTime) return;
        if (!stamina) return;
        var r = stamina.SpendExhaustible(stamina.toolCost);
        if (r == PlayerStamina.SpendResult.Fainted){ sleep.FaintNow(); return; }
        // nếu Exhausted thì vẫn tiếp tục chặt, anim/cooldown đã chậm theo ActionTimeMult()
        usingItem = it;
        currentHandler = handler;
        nextUseTime = Time.time + Mathf.Max(0.05f, it.cooldown) * ActionTimeMult();
        toolLocked = true;
        toolTimer = toolFailSafe * ActionTimeMult();
        if (anim) anim.speed = AnimSpeedMult();
        toolFacing = Facing4FromAnim();           // chốt hướng
        anim.SetFloat("Horizontal", toolFacing.x);
        anim.SetFloat("Vertical",   toolFacing.y);
        anim.SetFloat("Speed",      0f);
        pc?.SetMoveLock(true);                    // nếu PlayerController có hàm này]
        currentHandler?.OnBeginUse(this, usingItem);
    }

    public void Tool_DoHit()
    {
        if (!usingItem) return;
        if (currentHandler == null) return;

        var context = BuildHitContext(usingItem);
        currentHandler.OnPerformHit(this, usingItem, context);
    }

    public void Tool_End()
    {
        if (anim) anim.speed = 1f;
        var finishedItem = usingItem;
        usingItem = null;
        var handler = currentHandler;
        currentHandler = null;
        if (!toolLocked) return;
        toolLocked = false;
        pc?.SetMoveLock(false);
        handler?.OnEndUse(this, finishedItem);
    }
    Vector2 Facing4FromAnim()
    {
        float x = anim.GetFloat("Horizontal"), y = anim.GetFloat("Vertical");
        if (Mathf.Abs(x) >= Mathf.Abs(y)) return x >= 0 ? Vector2.right : Vector2.left;
        return y >= 0 ? Vector2.up : Vector2.down;
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

    void InitializeToolHandlers()
    {
        currentHandler = null;
        toolHandlers.Clear();
        RegisterToolHandler(new AxeToolActionHandler());
        RegisterToolHandler(new HoeToolActionHandler());
        RegisterToolHandler(new WateringCanToolActionHandler());
    }

    void RegisterToolHandler(IToolActionHandler handler)
    {
        if (handler == null) return;
        toolHandlers[handler.ToolType] = handler;
    }

    public ToolHitContext BuildHitContext(ItemSO item)
    {
        Vector2 dir = toolLocked ? toolFacing : aimDir;
        float fwd = (item.hitboxForward >= 0f) ? item.hitboxForward : originDist;
        Vector3 center = (Vector2)transform.position + dir * fwd;
        center += new Vector3(0f, item.hitboxYOffset, 0f);
        float radius = Mathf.Max(0.01f, item.range) * Mathf.Max(0.01f, item.hitboxScale);
        var hits = Physics2D.OverlapCircleAll(center, radius, hitMask);
        return new ToolHitContext(dir, center, radius, hits);
    }

    public void ApplyDamageToTargets(ItemSO item, Collider2D[] hits)
    {
        if (hits == null) return;
        foreach (var c in hits)
        {
            if (!c) continue;
            var target = c.GetComponent<IToolTarget>();
            if (target == null) continue;
            Vector2 pushDir = (c.transform.position - transform.position).normalized;
            target.Hit(item.toolType, item.Dame, pushDir);
        }
    }

    public SoilManager EnsureSoilManager()
    {
        if (!soilManager) soilManager = FindFirstObjectByType<SoilManager>();
        return soilManager;
    }

    public Animator ToolAnimator => anim;

    public readonly struct ToolHitContext
    {
        public readonly Vector2 Direction;
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly Collider2D[] Hits;

        public ToolHitContext(Vector2 direction, Vector3 center, float radius, Collider2D[] hits)
        {
            Direction = direction;
            Center = center;
            Radius = radius;
            Hits = hits;
        }
    }
}
