// HotbarUI.cs
using System.Linq;
using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] HotbarSlotUI[] slots;

    void Awake(){
        if (!inv) inv = FindObjectOfType<PlayerInventory>();
        if (slots == null || slots.Length == 0 || slots.Any(s => s == null))
            slots = GetComponentsInChildren<HotbarSlotUI>(true)
                    .OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
    }
    void OnEnable(){
        if (!inv) return;
        inv.SelectedChanged += OnChanged;
        inv.HotbarChanged   += OnChanged;
        if (inv.selected < 0 || inv.selected >= inv.hotbar.Length) inv.SelectSlot(0);
        Refresh();
    }
    void OnDisable(){
        if (!inv) return;
        inv.SelectedChanged -= OnChanged;
        inv.HotbarChanged   -= OnChanged;
    }
    void OnChanged(int _){ Refresh(); }
    void OnChanged(){ Refresh(); }

    public void OnClickSlot(int i){ inv?.SelectSlot(i); }

    public void Refresh(){
        if (!inv || slots == null) return;
        int n = Mathf.Min(slots.Length, inv.hotbar.Length);
        for (int i = 0; i < n; i++){
            var st = inv.hotbar[i];
            slots[i]?.Render(st, i == inv.selected, i, this);
        }
        for (int i = n; i < slots.Length; i++) slots[i]?.Render(default, false, i, this);
    }
}
