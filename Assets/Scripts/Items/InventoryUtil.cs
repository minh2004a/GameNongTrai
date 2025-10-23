// InventoryUtil.cs
using UnityEngine;

public static class InventoryUtil
{
    public static bool TryAddToHotbar(PlayerInventory inv, ItemSO item, int count)
    {
        // cộng dồn nếu đã có cùng item
        for (int i = 0; i < inv.hotbar.Length; i++){
            var s = inv.hotbar[i];
            if (s.item == item){ inv.SetHotbar(i, item, s.count + count); return true; }
        }
        // bỏ vào ô trống
        for (int i = 0; i < inv.hotbar.Length; i++){
            if (inv.hotbar[i].item == null){ inv.SetHotbar(i, item, count); return true; }
        }
        return false; // đầy
    }
}
