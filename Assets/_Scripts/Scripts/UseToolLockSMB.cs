using UnityEngine;

public class UseToolLockSMB : StateMachineBehaviour
{
    ToolUser tool;
    SpriteRenderer[] srs;
    bool lockFlipX;

    public override void OnStateEnter(Animator a, AnimatorStateInfo s, int l)
    {
        tool = a.GetComponentInParent<ToolUser>();
        if (!tool) return;

        // chốt flip theo hướng đã khóa
        var f = tool.ToolFacing; // Vector2 do bạn chốt lúc bắt đầu chặt
        lockFlipX = f.x < 0f;

        // lấy mọi SpriteRenderer để khỏi sót child
        srs = a.GetComponentsInChildren<SpriteRenderer>(true);
        // khóa di chuyển nếu cần
        tool.ApplyToolFacingLockFrame();
    }

    public override void OnStateUpdate(Animator a, AnimatorStateInfo s, int l)
    {
        if (!tool) return;
        // ép Animator giữ hướng
        tool.ApplyToolFacingLockFrame();

        // ép flipX trái/phải
        if (srs != null)
            for (int i = 0; i < srs.Length; i++) if (srs[i]) srs[i].flipX = lockFlipX;
    }

    public override void OnStateExit(Animator a, AnimatorStateInfo s, int l)
    {
        tool = null;
        srs = null;
    }
}
