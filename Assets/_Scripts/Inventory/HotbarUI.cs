
// HotbarUI.cs
using System.Linq;
using UnityEngine;
// Quản lý giao diện thanh công cụ (hotbar) của người chơi
public class HotbarUI : MonoBehaviour
{ 
    [SerializeField] PlayerInventory inv;
    [SerializeField] HotbarSlotUI[] slots;
    PlayerUseConsumable consumableUser;

    void Awake()
    {
        if (!inv) inv = FindObjectOfType<PlayerInventory>();
        if (inv) consumableUser = inv.GetComponent<PlayerUseConsumable>();
        if (slots == null || slots.Length == 0 || slots.Any(s => s == null))
            slots = GetComponentsInChildren<HotbarSlotUI>(true)
                    .OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
    }
    void OnEnable()
    {
        if (!inv) return;
        inv.SelectedChanged += OnChanged;
        inv.HotbarChanged += OnChanged;
        if (inv.selected < 0 || inv.selected >= inv.hotbar.Length) inv.SelectSlot(0);
        Refresh();
    }
    void OnDisable()
    {
        if (!inv) return;
        inv.SelectedChanged -= OnChanged;
        inv.HotbarChanged -= OnChanged;
    }
    void OnChanged(int _) { Refresh(); }
    void OnChanged() { Refresh(); }

    public void OnClickSlot(int i) { inv?.SelectSlot(i); }

    public void OnRightClickSlot(int i)
    {
        if (!inv) return;
        inv.SelectSlot(i);
        if (!consumableUser && inv) consumableUser = inv.GetComponent<PlayerUseConsumable>();
        consumableUser?.TryUseSelected(ignoreUiGuard: true);
    }

    public void Refresh()
    {
        if (!inv || slots == null) return;
        int n = Mathf.Min(slots.Length, inv.hotbar.Length);
        for (int i = 0; i < n; i++)
        {
            var st = inv.hotbar[i];
            slots[i]?.Render(st, i == inv.selected, i, this);
        }
        for (int i = n; i < slots.Length; i++) slots[i]?.Render(default, false, i, this);
    }
    public void RequestSwap(int a, int b)
    {
        if (!inv) return;
        inv.SwapHotbarSlot(a, b);
        Refresh();
    }
    public void RequestMoveOrMerge(int a, int b)
    {
        if (!inv) return;
        inv.MoveOrMergeHotbarSlot(a, b);
        Refresh();
    }
    public void RequestMoveHotbarToBag(int hotbarIndex, int bagIndex)
    {
        if (!inv) return;
        inv.MoveOrSwapHotbarBag(hotbarIndex, bagIndex);
        Refresh(); // cập nhật hotbar

        // Bag UI sẽ tự Refresh nếu đang lắng nghe BagChanged
        // (InventoryBookUI.OnEnable đã sub event BagChanged rồi)
    }
}
