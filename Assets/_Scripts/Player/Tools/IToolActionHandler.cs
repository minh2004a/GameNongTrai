using UnityEngine;

public interface IToolActionHandler
{
    ToolType ToolType { get; }
    bool CanBeginUse(ToolUser user, ItemSO item);
    void OnBeginUse(ToolUser user, ItemSO item);
    void OnPerformHit(ToolUser user, ItemSO item, ToolUser.ToolHitContext context);
    void OnEndUse(ToolUser user, ItemSO item);
}
