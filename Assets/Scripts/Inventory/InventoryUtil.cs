// InventoryUtil.cs
using UnityEngine;

public static class InventoryUtil
{
    public static bool TryAddToHotbar(PlayerInventory inv, ItemSO item, int count)
    {
        // cộng dồn nếu đã có cùng item
        for (int i = 0; i < inv.hotbar.Length; i++)
        {
            var s = inv.hotbar[i];
            if (s.item == item) { inv.SetHotbar(i, item, s.count + count); return true; }
        }
        // bỏ vào ô trống
        for (int i = 0; i < inv.hotbar.Length; i++)
        {
            if (inv.hotbar[i].item == null) { inv.SetHotbar(i, item, count); return true; }
        }
        return false; // đầy
    }
    // InventoryUtil.cs (thêm)
    public static bool TryAddToBag(PlayerInventory inv, ItemSO item, int count)
    {
        if (!item || count <= 0) return false;

        // cộng dồn
        for (int i = 0; i < inv.bag.Length && count > 0; i++)
        {
            var s = inv.bag[i];
            if (s.item == item) { inv.SetBag(i, item, s.count + count); return true; }
        }
        // ô trống
        for (int i = 0; i < inv.bag.Length && count > 0; i++)
        {
            if (inv.bag[i].item == null) { inv.SetBag(i, item, count); return true; }
        }
        return false;
    }

    public static bool TryAddAuto(PlayerInventory inv, ItemSO item, int count)
    {
        if (TryAddToHotbar(inv, item, count)) return true;
        return TryAddToBag(inv, item, count);
    }

}
