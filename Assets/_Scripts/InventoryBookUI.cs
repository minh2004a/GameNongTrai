using UnityEngine;

public class InventoryBookUI : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] Transform slotsParent;      // = SlotsArea
    [SerializeField] InventorySlotUI slotPrefab;

    InventorySlotUI[] slots;

    void Awake()
    {
        if (!inv) inv = FindObjectOfType<PlayerInventory>();
        BuildSlots();
    }

    void OnEnable()
    {
        if (inv == null) return;
        inv.BagChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (inv == null) return;
        inv.BagChanged -= Refresh;
    }

    void BuildSlots()
    {
        if (inv == null || slotsParent == null || slotPrefab == null) return;

        // xoá con cũ
        foreach (Transform c in slotsParent)
            Destroy(c.gameObject);

        int n = inv.bag.Length; // luôn tạo đủ 20 ô
        slots = new InventorySlotUI[n];

        for (int i = 0; i < n; i++)
        {
            var slot = Instantiate(slotPrefab, slotsParent);
            slot.Init(i, this);
            slots[i] = slot;
        }
    }

    void Refresh()
    {
        if (inv == null || slots == null) return;

        int unlocked = inv.UnlockedBagSlots;
        int n = Mathf.Min(inv.bag.Length, slots.Length);

        for (int i = 0; i < n; i++)
        {
            bool locked = i >= unlocked;
            var stack = locked ? default : inv.bag[i];
            slots[i].Render(stack, locked);
        }
    }

    public void OnSlotClicked(int index)
    {
        Debug.Log($"Clicked bag slot {index}");
        // sau này show info bên phải, drag drop,... ở đây
    }
}
