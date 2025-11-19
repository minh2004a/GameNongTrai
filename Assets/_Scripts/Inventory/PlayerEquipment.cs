

using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    ItemSO[] equipped; // index = (int)EquipSlotType
    [SerializeField] PlayerInventory inventory;
    [SerializeField] PlayerStamina stamina;

    public event Action<EquipSlotType> EquipmentChanged;

    void Awake()
    {
        EnsureInit();
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!stamina) stamina = GetComponent<PlayerStamina>();
        UpdateBagSlotBonus();
        UpdateStaminaBonus();
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
        if (slot == EquipSlotType.Backpack)
        {
            UpdateBagSlotBonus();
        }
        else if (slot == EquipSlotType.Boots)
        {
            UpdateStaminaBonus();
        }
    }
    void EnsureInit()
    {
        if (equipped == null)
        {
            int n = Enum.GetValues(typeof(EquipSlotType)).Length;
            equipped = new ItemSO[n];
        }
    }

    void UpdateBagSlotBonus()
    {
        if (!inventory) return;

        int bonus = 0;
        var backpack = Get(EquipSlotType.Backpack);
        if (backpack)
        {
            bonus = Mathf.Max(0, backpack.backpackSlotBonus);
        }

        inventory.SetBagSlotBonus(bonus);
    }

    void UpdateStaminaBonus()
    {
        if (!stamina) return;

        float maxBonus = 0f;
        float regenBonus = 0f;
        var boots = Get(EquipSlotType.Boots);
        if (boots)
        {
            maxBonus = Mathf.Max(0f, boots.staminaMaxBonus);
            regenBonus = Mathf.Max(0f, boots.staminaRegenBonus);
        }

        stamina.ApplyMaxBonus(maxBonus);
        stamina.ApplyRegenBonus(regenBonus);
    }
}
