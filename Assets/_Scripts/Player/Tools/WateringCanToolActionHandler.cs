using System.Collections.Generic;
using UnityEngine;

public sealed class WateringCanToolActionHandler : IToolActionHandler
{
    public ToolType ToolType => ToolType.WateringCan;

    public bool CanBeginUse(ToolUser user, ItemSO item) => true;

    public void OnBeginUse(ToolUser user, ItemSO item)
    {
        var anim = user.ToolAnimator;
        if (!anim) return;
        anim.ResetTrigger("UseAxe");
        anim.ResetTrigger("UseHoe");
        anim.SetTrigger("UseTool");
    }

    public void OnPerformHit(ToolUser user, ItemSO item, ToolUser.ToolHitContext context)
    {
        var soil = user.EnsureSoilManager();
        var watered = new HashSet<PlantGrowth>();
        HashSet<Vector2Int> hydratedCells = soil ? new HashSet<Vector2Int>() : null;

        foreach (var hit in context.Hits)
        {
            if (!hit) continue;
            var plant = hit.GetComponentInParent<PlantGrowth>();
            if (!plant) continue;
            if (!watered.Add(plant)) continue;
            plant.Water();
            if (hydratedCells != null)
            {
                hydratedCells.Add(soil.WorldToCell(plant.transform.position));
            }
        }

        if (hydratedCells != null)
        {
            hydratedCells.Add(soil.WorldToCell((Vector2)context.Center));
            foreach (var cell in hydratedCells)
            {
                soil.TryWaterCell(cell);
            }
        }
    }

    public void OnEndUse(ToolUser user, ItemSO item) { }
}
