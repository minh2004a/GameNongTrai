using UnityEngine;

public class FreezeFacingSMB : StateMachineBehaviour
{
    Vector2 lockDir;

    public override void OnStateEnter(Animator a, AnimatorStateInfo s, int l){
        float hx = a.GetFloat("Horizontal"), hy = a.GetFloat("Vertical");
        lockDir = Mathf.Abs(hx) >= Mathf.Abs(hy) ? new Vector2(Mathf.Sign(hx), 0)
                                                 : new Vector2(0, Mathf.Sign(hy));
    }

    public override void OnStateUpdate(Animator a, AnimatorStateInfo s, int l){
        a.SetFloat("Horizontal", lockDir.x);
        a.SetFloat("Vertical",   lockDir.y);   // ép hướng MỖI FRAME của state
    }
}
 