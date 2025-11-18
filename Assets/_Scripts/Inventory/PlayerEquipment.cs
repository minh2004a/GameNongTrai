using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    ItemSO[] equipped; // index = (int)EquipSlotType

    public event Action<EquipSlotType> EquipmentChanged;

    void Awake()
    {
        EnsureInit();
    }

    public ItemSO Get(EquipSlotType slot)
    {
        if (slot == EquipSlotType.None) return null;
        EnsureInit();                           // <- thêm dòng này

        int i = (int)slot;
        if (i < 0 || i >= equipped.Length) return null;
        return equipped[i];
    }

    public void Set(EquipSlotType slot, ItemSO item)
    {
        if (slot == EquipSlotType.None) return;
        EnsureInit();                           // <- thêm dòng này

        int i = (int)slot;
        if (i < 0 || i >= equipped.Length) return;

        equipped[i] = item;
        EquipmentChanged?.Invoke(slot);
    }
    void EnsureInit()
    {
        if (equipped == null)
        {
            int n = Enum.GetValues(typeof(EquipSlotType)).Length;
            equipped = new ItemSO[n];
        }
    }
}
