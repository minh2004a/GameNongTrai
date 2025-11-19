using System.Collections;
using UnityEngine;

public class StartingInventoryApplier : MonoBehaviour
{
    public StartingLoadout loadout;
    public ItemDB itemDB;  // để tra bằng itemKey
    public PlayerEquipment equipment;

    IEnumerator Start()
    {
        if (!SaveStore.JustStartedNewGame) yield break;

        var inv = FindObjectOfType<PlayerInventory>(true);
        while (!inv) { yield return null; inv = FindObjectOfType<PlayerInventory>(true); }
        if (!equipment && inv) equipment = inv.GetComponent<PlayerEquipment>();

        foreach (var e in loadout.items)
        {
            // Lấy ItemSO từ entry (ưu tiên kéo-thả; nếu trống thì tra theo key)
            ItemSO it = e.item;
            if (!it && itemDB && !string.IsNullOrEmpty(e.itemKey))
                it = itemDB.Find(e.itemKey);

            if (!it || e.amount <= 0) continue;

            // Nếu có chỉ định slot hotbar -> đặt thẳng vào hotbar
            if (e.hotbarSlot >= 0)
            {
                int slot = Mathf.Clamp(e.hotbarSlot, 0, inv.hotbar.Length - 1);
                // Nếu item không stack được thì chỉ 1
                int count = it.stackable ? Mathf.Clamp(e.amount, 1, Mathf.Max(1, it.maxStack)) : 1;
                inv.SetHotbar(slot, it, count);

                // "equip" = chọn slot đó làm slot đang cầm
                if (e.equip) inv.SelectSlot(slot);
            }
            else
            {
                // Không chỉ định slot -> thả vào kho (tự add/stack)
                inv.AddItem(it, e.amount);

                // Nếu muốn "equip" mà không có slot chỉ định, thử chọn
                if (e.equip)
                {
                    for (int i = 0; i < inv.hotbar.Length; i++)
                        if (inv.hotbar[i].item == it) { inv.SelectSlot(i); break; }
                }
            }
        }

        // Ghi inventory vào save (đúng chuẩn)
        SaveStore.CaptureInventory(inv, itemDB);
        SaveStore.CaptureEquipment(equipment, itemDB);
        SaveStore.JustStartedNewGame = false;
    }
}
