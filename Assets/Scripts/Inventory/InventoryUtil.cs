using UnityEngine;

public static class InventoryUtil
{
    // trả về: số còn dư chưa nhét được
    public static int AddToHotbar(PlayerInventory inv, ItemSO item, int count)
    {
        if (!item || count <= 0) return count;
        int cap = Mathf.Max(1, item.stackable ? item.maxStack : 1);

        // gộp vào stack cùng loại
        for (int i = 0; i < inv.hotbar.Length && count > 0; i++)
        {
            var s = inv.hotbar[i];
            if (s.item == item && s.count < cap)
            {
                int move = Mathf.Min(count, cap - s.count);
                inv.SetHotbar(i, item, s.count + move);
                count -= move;
            }
        }
        // đổ vào ô trống
        for (int i = 0; i < inv.hotbar.Length && count > 0; i++)
        {
            if (inv.hotbar[i].item == null)
            {
                int move = Mathf.Min(count, cap);
                inv.SetHotbar(i, item, move);
                count -= move;
            }
        }
        return count;
    }

    public static int AddToBag(PlayerInventory inv, ItemSO item, int count)
    {
        if (!item || count <= 0) return count;
        int cap = Mathf.Max(1, item.stackable ? item.maxStack : 1);

        // gộp vào stack cùng loại
        for (int i = 0; i < inv.bag.Length && count > 0; i++)
        {
            var s = inv.bag[i];
            if (s.item == item && s.count < cap)
            {
                int move = Mathf.Min(count, cap - s.count);
                inv.SetBag(i, item, s.count + move);
                count -= move;
            }
        }
        // đổ vào ô trống
        for (int i = 0; i < inv.bag.Length && count > 0; i++)
        {
            if (inv.bag[i].item == null)
            {
                int move = Mathf.Min(count, cap);
                inv.SetBag(i, item, move);
                count -= move;
            }
        }
        return count;
    }

    // auto: hotbar trước, dư → bag
    public static int AddAuto(PlayerInventory inv, ItemSO item, int count)
    {
        int left = AddToHotbar(inv, item, count);
        if (left > 0) left = AddToBag(inv, item, left);
        return left; // 0 = nhét hết
    }
    public static int RemainingCapacityFor(PlayerInventory inv, ItemSO item){
        if (!inv || !item) return 0;
        int cap = Mathf.Max(1, item.stackable ? item.maxStack : 1);
        int free = 0;

        for (int i = 0; i < inv.hotbar.Length; i++){
            var s = inv.hotbar[i];
            if (s.item == null) free += cap;
            else if (s.item == item && s.count < cap) free += (cap - s.count);
        }
        for (int i = 0; i < inv.bag.Length; i++){
            var s = inv.bag[i];
            if (s.item == null) free += cap;
            else if (s.item == item && s.count < cap) free += (cap - s.count);
        }
        return free;
    }
    public static bool HasAnySpaceFor(PlayerInventory inv, ItemSO item)
        => RemainingCapacityFor(inv, item) > 0;


    // wrapper bool cho code cũ
    public static bool TryAddToHotbar(PlayerInventory inv, ItemSO item, int count)
        => AddToHotbar(inv, item, count) == 0;
    public static bool TryAddToBag   (PlayerInventory inv, ItemSO item, int count)
        => AddToBag(inv, item, count) == 0;
    public static bool TryAddAuto    (PlayerInventory inv, ItemSO item, int count)
        => AddAuto(inv, item, count) == 0;
}
