// PlayerInventory.cs
using System;
using UnityEngine;

[Serializable] public struct ItemStack { public ItemSO item; public int count; }

public class PlayerInventory : MonoBehaviour
{
    public ItemStack[] hotbar = new ItemStack[8];
    public int selected;
    public ItemStack[] bag = new ItemStack[24];
    public event System.Action BagChanged;

    public event Action<int> SelectedChanged;   // báo UI khi đổi ô
    public event Action HotbarChanged;          // báo UI khi nội dung ô đổi

    public ItemSO CurrentItem =>
        (selected >= 0 && selected < hotbar.Length) ? hotbar[selected].item : null;

    public void SelectSlot(int i)
    {
        if ((uint)i >= (uint)hotbar.Length || i == selected) return;
        selected = i;
        SelectedChanged?.Invoke(i);
    }
    public int AddItem(ItemSO item, int count)
    {
        return InventoryUtil.AddAuto(this, item, count); // 0 = ok, >0 = còn dư
    }
    public void CycleSlot(int d)
    {
        int n = hotbar.Length; if (n == 0) return;
        SelectSlot((selected + ((d % n + n) % n)) % n);
    }
    public void SetBag(int i, ItemSO item, int count)
    {
        bag[i] = new ItemStack { item = item, count = count };
        BagChanged?.Invoke();
    }

    public void SetHotbar(int i, ItemSO item, int count = 1)
    {
        if ((uint)i >= (uint)hotbar.Length) return;
        hotbar[i] = new ItemStack { item = item, count = count };
        HotbarChanged?.Invoke();
        if (i == selected) SelectedChanged?.Invoke(selected);
    }
    public void SwapHotbarSlot(int a, int b)
    {
        if ((uint)a >= (uint)hotbar.Length || (uint)b >= (uint)hotbar.Length || a == b) return;
        var tmp = hotbar[a];
        hotbar[a] = hotbar[b];
        hotbar[b] = tmp;
        HotbarChanged?.Invoke();
        if (a == selected || b == selected) SelectedChanged?.Invoke(selected);
    }

    public void MoveOrMergeHotbarSlot(int from, int to)
    {
        if ((uint)from >= (uint)hotbar.Length || (uint)to >= (uint)hotbar.Length || from == to) return;

        var a = hotbar[from]; // source
        var b = hotbar[to];   // target
        if (a.item == null) return;

        // Gộp nếu cùng item và cho phép stack
        if (b.item != null && a.item == b.item && a.item.stackable)
        {
            int cap = Mathf.Max(1, a.item.maxStack);
            int space = cap - b.count;
            if (space > 0)
            {
                int move = Mathf.Min(a.count, space);
                b.count += move;
                a.count -= move;
                hotbar[to] = b;
                hotbar[from] = (a.count > 0) ? a : default;
            }
            HotbarChanged?.Invoke();
            if (to == selected || from == selected) SelectedChanged?.Invoke(selected);
            return;
        }

        // Trống -> di chuyển
        if (b.item == null)
        {
            hotbar[to] = a;
            hotbar[from] = default;
        }
        else
        {
            // Khác loại hoặc không stack được -> hoán đổi
            hotbar[to] = a;
            hotbar[from] = b;
        }

        HotbarChanged?.Invoke();
        if (to == selected || from == selected) SelectedChanged?.Invoke(selected);
    }
    public bool ConsumeSelected(int n = 1)
    {
        if ((uint)selected >= (uint)hotbar.Length || n <= 0) return false;
        var s = hotbar[selected];
        if (s.item == null || s.count < n) return false;
        s.count -= n;
        hotbar[selected] = (s.count > 0) ? s : default;
        HotbarChanged?.Invoke();
        SelectedChanged?.Invoke(selected);
        return true;
    }
}
