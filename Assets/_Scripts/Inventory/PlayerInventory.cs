
// PlayerInventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;
// Quản lý kho đồ của người chơi, bao gồm hotbar và túi đồ
[Serializable] public struct ItemStack { public ItemSO item; public int count; }

public class PlayerInventory : MonoBehaviour
{
    public ItemStack[] hotbar = new ItemStack[8];
    public int selected;

    [SerializeField] EquipSlotType[] equipmentSlotOrder = new[]
    {
        EquipSlotType.Hat,
        EquipSlotType.Armor,
        EquipSlotType.Pants,
        EquipSlotType.Gloves,
        EquipSlotType.Boots,
        EquipSlotType.Ring,
        EquipSlotType.Backpack,
        EquipSlotType.Potion
    };
    public ItemStack[] equipment = new ItemStack[8];

    // tối đa 20 ô
    public ItemStack[] bag = new ItemStack[20];

    // số ô đang mở (set trong Inspector)
    [SerializeField, Min(0)] int unlockedBagSlots = 8;

    // property cho code khác xài
    public int UnlockedBagSlots => Mathf.Clamp(unlockedBagSlots, 0, bag.Length);

    public event System.Action BagChanged;
    public event Action<int> SelectedChanged;   // báo UI khi đổi ô
    public event Action HotbarChanged;          // báo UI khi nội dung ô đổi
    public event Action<ItemSO, InventoryAddResult> ItemAdded;
    public event Action EquipmentChanged;

    public ItemSO CurrentItem =>
        (selected >= 0 && selected < hotbar.Length) ? hotbar[selected].item : null;

    void Awake()
    {
        EnsureEquipmentSize();
    }

    void EnsureEquipmentSize()
    {
        int n = equipmentSlotOrder?.Length ?? 0;
        if (n <= 0) n = 0;
        if (equipment == null || equipment.Length != n)
            equipment = new ItemStack[n];
    }

    public IReadOnlyList<EquipSlotType> EquipmentSlotOrder => equipmentSlotOrder;
    public ItemStack GetEquipmentSlot(int i)
    {
        EnsureEquipmentSize();
        if ((uint)i >= (uint)equipment.Length) return default;
        return equipment[i];
    }
    public bool CanEquip(ItemSO item, int slotIndex)
    {
        if ((uint)slotIndex >= (uint)equipmentSlotOrder.Length) return false;
        if (!item) return true;
        return item.category == ItemCategory.Equipment && item.equipSlot == equipmentSlotOrder[slotIndex];
    }
    public void SetEquipment(int index, ItemSO item, int count = 1)
    {
        EnsureEquipmentSize();
        if ((uint)index >= (uint)equipment.Length) return;
        if (item && !CanEquip(item, index)) return;
        if (item && count <= 0)
        {
            equipment[index] = default;
            EquipmentChanged?.Invoke();
            return;
        }
        equipment[index] = new ItemStack { item = item, count = count };
        EquipmentChanged?.Invoke();
    }
    void SetEquipment(int index, ItemStack stack)
    {
        EnsureEquipmentSize();
        if ((uint)index >= (uint)equipment.Length) return;
        if (stack.item && !CanEquip(stack.item, index)) return;
        if (stack.count <= 0)
            stack = default;
        equipment[index] = stack;
        EquipmentChanged?.Invoke();
    }

    public void SelectSlot(int i)
    {
        if ((uint)i >= (uint)hotbar.Length || i == selected) return;
        selected = i;
        SelectedChanged?.Invoke(i);
    }
    public InventoryAddResult AddItemDetailed(ItemSO item, int count)
    {
        var result = InventoryUtil.AddAutoDetailed(this, item, count);
        if (item && (result.addedToHotbar > 0 || result.addedToBag > 0 || result.remaining > 0))
        {
            ItemAdded?.Invoke(item, result);
        }
        return result;
    }

    public int AddItem(ItemSO item, int count)
    {
        return AddItemDetailed(item, count).remaining; // 0 = ok, >0 = còn dư
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
    void MergeOrSwap(ref ItemStack source, ref ItemStack target)
    {
        if (source.item == null) return;

        // Gộp nếu cùng item và cho phép stack
        if (target.item != null && source.item == target.item && source.item.stackable)
        {
            int cap = Mathf.Max(1, source.item.maxStack);
            int space = cap - target.count;
            if (space > 0)
            {
                int move = Mathf.Min(source.count, space);
                target.count += move;
                source.count -= move;
                if (source.count <= 0) source = default;
            }
            return;
        }

        // Trống -> di chuyển
        if (target.item == null)
        {
            target = source;
            source = default;
            return;
        }

        // Khác loại hoặc không stack được -> hoán đổi
        (source, target) = (target, source);
    }
    public void MoveOrMergeBagSlot(int from, int to)
    {
        if ((uint)from >= (uint)bag.Length || (uint)to >= (uint)bag.Length || from == to) return;
        if (from >= UnlockedBagSlots || to >= UnlockedBagSlots) return;

        ref ItemStack a = ref bag[from];
        ref ItemStack b = ref bag[to];
        if (a.item == null) return;

        MergeOrSwap(ref a, ref b);
        BagChanged?.Invoke();
    }
    public void MoveOrSwapHotbarBag(int hotbarIndex, int bagIndex)
    {
        // an toàn index
        if ((uint)hotbarIndex >= (uint)hotbar.Length) return;
        if ((uint)bagIndex >= (uint)bag.Length) return;
        if (bagIndex >= UnlockedBagSlots) return;

        ref ItemStack h = ref hotbar[hotbarIndex];
        ref ItemStack b = ref bag[bagIndex];

        if (h.item == null) return;

        MergeOrSwap(ref h, ref b);

        HotbarChanged?.Invoke();
        BagChanged?.Invoke();

        if (hotbarIndex == selected)
            SelectedChanged?.Invoke(selected);
    }
    public void MoveOrMergeBagToHotbar(int bagIndex, int hotbarIndex)
    {
        if ((uint)hotbarIndex >= (uint)hotbar.Length) return;
        if ((uint)bagIndex >= (uint)bag.Length) return;
        if (bagIndex >= UnlockedBagSlots) return;

        ref ItemStack b = ref bag[bagIndex];
        ref ItemStack h = ref hotbar[hotbarIndex];
        if (b.item == null) return;

        MergeOrSwap(ref b, ref h);

        HotbarChanged?.Invoke();
        BagChanged?.Invoke();

        if (hotbarIndex == selected)
            SelectedChanged?.Invoke(selected);
    }
    public void MoveOrSwapBagEquipment(int bagIndex, int equipIndex)
    {
        EnsureEquipmentSize();
        if ((uint)bagIndex >= (uint)bag.Length) return;
        if ((uint)equipIndex >= (uint)equipment.Length) return;
        if (bagIndex >= UnlockedBagSlots) return;

        ref ItemStack bagSlot = ref bag[bagIndex];
        ref ItemStack equipSlot = ref equipment[equipIndex];

        if (bagSlot.item && !CanEquip(bagSlot.item, equipIndex)) return;

        (bagSlot, equipSlot) = (equipSlot, bagSlot);

        BagChanged?.Invoke();
        EquipmentChanged?.Invoke();
    }
    public void MoveOrSwapHotbarEquipment(int hotbarIndex, int equipIndex)
    {
        EnsureEquipmentSize();
        if ((uint)hotbarIndex >= (uint)hotbar.Length) return;
        if ((uint)equipIndex >= (uint)equipment.Length) return;

        ref ItemStack hot = ref hotbar[hotbarIndex];
        ref ItemStack equipSlot = ref equipment[equipIndex];

        if (hot.item && !CanEquip(hot.item, equipIndex)) return;

        (hot, equipSlot) = (equipSlot, hot);

        HotbarChanged?.Invoke();
        EquipmentChanged?.Invoke();

        if (hotbarIndex == selected)
            SelectedChanged?.Invoke(selected);
    }
    public void MoveOrSwapEquipmentSlot(int from, int to)
    {
        EnsureEquipmentSize();
        if ((uint)from >= (uint)equipment.Length || (uint)to >= (uint)equipment.Length || from == to) return;

        ref ItemStack source = ref equipment[from];
        ref ItemStack target = ref equipment[to];
        if (source.item == null) return;

        if (!CanEquip(source.item, to)) return;
        if (target.item && !CanEquip(target.item, from)) return;

        (source, target) = (target, source);

        EquipmentChanged?.Invoke();
    }
    public void UnlockBagSlots(int extra)
    {
        int before = unlockedBagSlots;
        unlockedBagSlots = Mathf.Clamp(unlockedBagSlots + extra, 0, bag.Length);

        if (unlockedBagSlots != before)
        {
            BagChanged?.Invoke();
        }
    }
    public void SetUnlockedBagSlots(int unlocked)
    {
        int clamped = Mathf.Clamp(unlocked, 0, bag.Length);
        if (clamped == unlockedBagSlots) return;
        unlockedBagSlots = clamped;
        BagChanged?.Invoke();
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
