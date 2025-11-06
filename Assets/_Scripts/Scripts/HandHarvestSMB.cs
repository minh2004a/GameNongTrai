using UnityEngine;

public class HandHarvestSMB : StateMachineBehaviour
{
    PlayerController controller;
    PlayerPlanting planting;
    bool releaseLock;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        controller = animator.GetComponentInParent<PlayerController>();
        planting = animator.GetComponentInParent<PlayerPlanting>();

        if (controller)
        {
            releaseLock = !controller.MoveLocked;
            controller.SetMoveLock(true);
        }
        else
        {
            releaseLock = false;
        }

        planting?.BeginHandHarvestAnimation();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (controller && releaseLock)
        {
            controller.SetMoveLock(false);
        }

        planting?.EndHandHarvestAnimation();
        controller = null;
        planting = null;
        releaseLock = false;
    }
}