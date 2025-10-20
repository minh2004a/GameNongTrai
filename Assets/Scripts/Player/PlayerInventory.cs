// PlayerInventory.cs
using System;
using UnityEngine;

[Serializable] public struct ItemStack { public ItemSO item; public int count; }

public class PlayerInventory : MonoBehaviour
{
    public ItemStack[] hotbar = new ItemStack[8];
    public int selected;

    public event Action<int> SelectedChanged;   // báo UI khi đổi ô
    public event Action HotbarChanged;          // báo UI khi nội dung ô đổi

    public ItemSO CurrentItem =>
        (selected >= 0 && selected < hotbar.Length) ? hotbar[selected].item : null;

    public void SelectSlot(int i){
        if ((uint)i >= (uint)hotbar.Length || i == selected) return;
        selected = i;
        SelectedChanged?.Invoke(i);
    }
    public void CycleSlot(int d){
        int n = hotbar.Length; if (n == 0) return;
        SelectSlot((selected + ((d % n + n) % n)) % n);
    }
    public void SetHotbar(int i, ItemSO item, int count = 1){
        if ((uint)i >= (uint)hotbar.Length) return;
        hotbar[i] = new ItemStack{ item=item, count=count };
        HotbarChanged?.Invoke();
        if (i == selected) SelectedChanged?.Invoke(selected);
    }
}
