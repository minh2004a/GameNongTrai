// ToolUnlockSMB.cs — gắn lên state Axe
using UnityEngine;
public class ToolUnlockSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator a, AnimatorStateInfo s, int l)
    {
        a.GetComponentInParent<PlayerUseTool>()?.Anim_EndAction(); // mở khóa khi state thoát
    }
}
