// AttackUnlockSMB.cs — gắn lên state Attack trong Animator
using UnityEngine;
public class AttackUnlockSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var pc = animator.GetComponentInParent<PlayerCombat>();
        if (pc) pc.AttackEnd();
    }
}
