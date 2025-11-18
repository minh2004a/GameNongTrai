using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] Transform slotsParent;
    [SerializeField] EquipmentSlotUI slotPrefab;

    EquipmentSlotUI[] slots;

    static readonly Dictionary<EquipSlotType, string> DefaultLabels = new()
    {
        { EquipSlotType.Hat, "Mũ" },
        { EquipSlotType.Armor, "Giáp" },
        { EquipSlotType.Pants, "Quần" },
        { EquipSlotType.Gloves, "Găng" },
        { EquipSlotType.Boots, "Giày" },
        { EquipSlotType.Ring, "Nhẫn" },
        { EquipSlotType.Backpack, "Ba lô" },
        { EquipSlotType.Potion, "Lọ thuốc" }
    };

    void Awake()
    {
        if (!inv) inv = FindObjectOfType<PlayerInventory>();
        BuildSlots();
    }

    void OnEnable()
    {
        if (!inv) return;
        inv.EquipmentChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (!inv) return;
        inv.EquipmentChanged -= Refresh;
    }

    void BuildSlots()
    {
        if (!inv || slotsParent == null || slotPrefab == null) return;

        foreach (Transform c in slotsParent)
            Destroy(c.gameObject);

        var order = inv.EquipmentSlotOrder;
        if (order == null) return;
        slots = new EquipmentSlotUI[order.Count];

        for (int i = 0; i < order.Count; i++)
        {
            var slot = Instantiate(slotPrefab, slotsParent);
            var label = DefaultLabels.TryGetValue(order[i], out var text) ? text : order[i].ToString();
            slot.Init(i, order[i], label, this);
            slots[i] = slot;
        }
    }

    void Refresh()
    {
        if (inv == null || slots == null) return;

        int n = Mathf.Min(inv.EquipmentSlotOrder.Count, slots.Length);
        for (int i = 0; i < n; i++)
        {
            var stack = inv.GetEquipmentSlot(i);
            slots[i].Render(stack);
        }
    }

    public void RequestEquipFromBag(int bagIndex, int equipIndex)
    {
        if (!inv) return;
        inv.MoveOrSwapBagEquipment(bagIndex, equipIndex);
    }

    public void RequestEquipFromHotbar(int hotbarIndex, int equipIndex)
    {
        if (!inv) return;
        inv.MoveOrSwapHotbarEquipment(hotbarIndex, equipIndex);
    }

    public void RequestMoveEquipmentToBag(int equipIndex, int bagIndex)
    {
        if (!inv) return;
        inv.MoveOrSwapBagEquipment(bagIndex, equipIndex);
    }

    public void RequestMoveEquipmentToHotbar(int equipIndex, int hotbarIndex)
    {
        if (!inv) return;
        inv.MoveOrSwapHotbarEquipment(hotbarIndex, equipIndex);
    }

    public void RequestSwapEquipment(int from, int to)
    {
        if (!inv) return;
        inv.MoveOrSwapEquipmentSlot(from, to);
    }
}
