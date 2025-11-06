using UnityEngine;
public class VFXDestroyOnExit : StateMachineBehaviour
{
    public override void OnStateExit(Animator a, AnimatorStateInfo s, int layerIndex)
    {
        Object.Destroy(a.gameObject);
    }
}
