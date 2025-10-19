using UnityEngine;
public class BowUnlockSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator a, AnimatorStateInfo s, int l)
    {
        a.GetComponentInParent<PlayerCombat>()?.BowEnd();
    }
}
