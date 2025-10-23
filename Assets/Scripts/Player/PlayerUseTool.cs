using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUseTool : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerController controller;
    [SerializeField] Animator anim;
    [SerializeField] Transform hitOrigin;
    [SerializeField] LayerMask targetMask;   // layer cây/đá
    [SerializeField] float hitRadius = 0.35f;
    [SerializeField] float toolFailSafe = 10f;
    float unlockAt = -1f;
    Vector2 lockedFace;   // hướng đã chốt
    ItemSO pendingItem;
    float nextUseTime;
    bool actionLocked;
    void Awake(){
        if (!inv) inv = GetComponent<PlayerInventory>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    void Update(){
        if (Mouse.current.leftButton.wasPressedThisFrame) TryUse();
        if (actionLocked && Time.time >= unlockAt) Anim_EndAction();
    }

    void TryUse(){
    var it = inv.CurrentItem;
    if (!it || it.category != ItemCategory.Tool) return;
    if (actionLocked || Time.time < nextUseTime) return;

    nextUseTime  = Time.time + it.cooldown;
    pendingItem  = it;
    actionLocked = true;
    unlockAt     = Time.time + toolFailSafe;

    controller?.SetMoveLock(true);

    // CHỐT 4 hướng từ Animator
    float hx = anim.GetFloat("Horizontal"), hy = anim.GetFloat("Vertical");
    lockedFace = Mathf.Abs(hx) >= Mathf.Abs(hy) ? new Vector2(Mathf.Sign(hx),0)
                                                : new Vector2(0,Mathf.Sign(hy));

    anim.ResetTrigger("UseTool");
    anim.SetTrigger("UseTool");
}

    // Animation Event ở frame chém
    public void ToolStrike(){
        if (!pendingItem) return;
        Vector2 dir    = lockedFace; // KHÔNG đọc theo chuột
        Vector2 center = (Vector2)hitOrigin.position + dir * pendingItem.range;
        var hits = Physics2D.OverlapCircleAll(center, hitRadius, targetMask);
        foreach (var c in hits)
            c.GetComponentInParent<IToolTarget>()?.Hit(pendingItem.toolType, pendingItem.power, dir);
    }

    // Animation Event cuối clip
    public void Anim_EndAction(){
        actionLocked = false;
        pendingItem  = null;
        controller?.ApplyPendingMove();
        controller?.SetMoveLock(false);
    }
    void OnDrawGizmosSelected(){
        if (!hitOrigin) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hitOrigin.position, 0.05f);
    }
}
