using UnityEngine;
// UI quản lý toàn bộ giao diện trang bị
public class EquipmentUI : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerEquipment equip;
    [SerializeField] EquipmentSlotUI[] slots;

    public static EquipmentUI Instance { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!inv) inv = FindObjectOfType<PlayerInventory>();
        if (!equip && inv) equip = inv.GetComponent<PlayerEquipment>();

        if (equip != null) equip.EquipmentChanged += OnEquipmentChanged;
    }

    void OnDestroy()
    {
        if (equip != null) equip.EquipmentChanged -= OnEquipmentChanged;
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        RefreshAll();
    }

    void OnEquipmentChanged(EquipSlotType slot)
    {
        RefreshSlot(slot);
    }

    void RefreshAll()
    {
        if (equip == null || slots == null) return;
        foreach (var s in slots)
        {
            if (!s) continue;
            s.Render(equip.Get(s.SlotType));
        }
    }

    void RefreshSlot(EquipSlotType slot)
    {
        if (equip == null || slots == null) return;
        foreach (var s in slots)
        {
            if (s && s.SlotType == slot)
            {
                s.Render(equip.Get(slot));
                break;
            }
        }
    }

    bool CanEquip(ItemSO item, EquipSlotType slot)
    {
        if (!item) return false;
        if (item.category != ItemCategory.Equipment) return false;
        if (item.equipSlot != slot) return false;   // mũ chỉ vào HatSlot
        return true;
    }

    // === gọi từ Bag khi kéo thả ===
    public void EquipFromBag(int bagIndex, EquipSlotType slot)
    {
        if (!inv || !equip) return;
        if ((uint)bagIndex >= (uint)inv.bag.Length) return;

        var stack = inv.bag[bagIndex];
        var item = stack.item;
        if (!CanEquip(item, slot)) return;

        // clear ô bag
        inv.SetBag(bagIndex, null, 0);

        // nếu slot đang có đồ → trả đồ cũ lại bag/hotbar (tạm: cho vào bag)
        var old = equip.Get(slot);
        if (old != null)
        {
            inv.AddItem(old, 1);   // dùng AddAuto để nhét lại
        }

        equip.Set(slot, item);
        RefreshSlot(slot);
    }

    // === gọi từ Hotbar khi kéo thả ===
    public void EquipFromHotbar(int hotbarIndex, EquipSlotType slot)
    {
        if (!inv || !equip) return;
        if ((uint)hotbarIndex >= (uint)inv.hotbar.Length) return;

        var stack = inv.hotbar[hotbarIndex];
        var item = stack.item;
        if (!CanEquip(item, slot)) return;

        inv.SetHotbar(hotbarIndex, null, 0);

        var old = equip.Get(slot);
        if (old != null)
        {
            inv.AddItem(old, 1);
        }

        equip.Set(slot, item);
        RefreshSlot(slot);
    }
    public void Unequip(EquipSlotType slot)
    {
        if (!inv || !equip) return;

        var item = equip.Get(slot);
        if (item == null) return;

        // thử nhét lại vào túi/hotbar
        int remaining = inv.AddItem(item, 1);  // 0 = ok
        if (remaining > 0) return;             // hết chỗ → thôi không tháo

        equip.Set(slot, null);      // xoá khỏi slot
        RefreshSlot(slot);          // vẽ lại UI
    }
}
