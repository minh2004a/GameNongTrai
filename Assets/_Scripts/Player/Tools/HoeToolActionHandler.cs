using UnityEngine;

public sealed class HoeToolActionHandler : IToolActionHandler
{
    public ToolType ToolType => ToolType.Hoe;

    public bool CanBeginUse(ToolUser user, ItemSO item) => true;

    public void OnBeginUse(ToolUser user, ItemSO item)
    {
        var anim = user.ToolAnimator;
        if (!anim) return;
        anim.ResetTrigger("UseAxe");
        anim.SetTrigger("UseHoe");
    }

    public void OnPerformHit(ToolUser user, ItemSO item, ToolUser.ToolHitContext context)
    {
        user.ApplyDamageToTargets(item, context.Hits);
        var soil = user.EnsureSoilManager();
        if (!soil) return;
        var cell = soil.WorldToCell((Vector2)context.Center);
        soil.TryTillCell(cell);
    }

    public void OnEndUse(ToolUser user, ItemSO item) { }
}
