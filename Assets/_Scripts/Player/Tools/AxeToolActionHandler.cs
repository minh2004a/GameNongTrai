using UnityEngine;

public sealed class AxeToolActionHandler : IToolActionHandler
{
    public ToolType ToolType => ToolType.Axe;

    public bool CanBeginUse(ToolUser user, ItemSO item) => true;

    public void OnBeginUse(ToolUser user, ItemSO item)
    {
        var anim = user.ToolAnimator;
        if (!anim) return;
        anim.ResetTrigger("UseHoe");
        anim.SetTrigger("UseAxe");
    }

    public void OnPerformHit(ToolUser user, ItemSO item, ToolUser.ToolHitContext context)
    {
        user.ApplyDamageToTargets(item, context.Hits);
    }

    public void OnEndUse(ToolUser user, ItemSO item) { }
}
