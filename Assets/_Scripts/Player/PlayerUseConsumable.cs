using UnityEngine;

/// <summary>
/// Cho phép người chơi sử dụng vật phẩm tiêu hao (hoa quả, thức ăn, ...)
/// bằng chuột phải hoặc từ UI.
/// </summary>
[RequireComponent(typeof(PlayerInventory))]
public class PlayerUseConsumable : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;
    [SerializeField] PlayerHealth health;
    [SerializeField] PlayerStamina stamina;
    [SerializeField] ConsumableConfirmUI confirmUI;

    ItemSO pendingItem;
    int pendingSlot = -1;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!health) health = GetComponent<PlayerHealth>();
        if (!stamina) stamina = GetComponent<PlayerStamina>();
        if (!confirmUI) confirmUI = FindObjectOfType<ConsumableConfirmUI>(true);
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        TryUseSelected();
    }

    /// <summary>
    /// Thử sử dụng vật phẩm đang được chọn trong hotbar.
    /// </summary>
    /// <param name="ignoreUiGuard">Bỏ qua việc chặn input khi click lên UI.</param>
    /// <returns>true nếu đã sử dụng và trừ vật phẩm.</returns>
    public bool TryUseSelected(bool ignoreUiGuard = false)
    {
        if (!inventory) return false;
        if (!ignoreUiGuard && UIInputGuard.BlockInputNow()) return false;

        int slot = inventory.selected;
        if ((uint)slot >= (uint)inventory.hotbar.Length) return false;

        var stack = inventory.hotbar[slot];
        var item = stack.item;
        if (!item || item.category != ItemCategory.Consumable) return false;

        if (confirmUI)
        {
            pendingSlot = slot;
            pendingItem = item;
            confirmUI.Show(item, OnConfirmUsePending, OnCancelUsePending);
            return false;
        }

        if (!ApplyEffects(item)) return false;

        return inventory.ConsumeSelected();
    }

    void OnConfirmUsePending()
    {
        if (!inventory)
        {
            ClearPending();
            return;
        }

        int slot = pendingSlot;
        var item = pendingItem;
        ClearPending();

        if ((uint)slot >= (uint)inventory.hotbar.Length) return;

        var stack = inventory.hotbar[slot];
        if (stack.item != item) return;

        if (inventory.selected != slot) inventory.SelectSlot(slot);

        if (!ApplyEffects(item)) return;

        inventory.ConsumeSelected();
    }

    void OnCancelUsePending() => ClearPending();

    void ClearPending()
    {
        pendingItem = null;
        pendingSlot = -1;
    }

    bool ApplyEffects(ItemSO item)
    {
        bool applied = false;

        if (item.healthRestore > 0 && health)
        {
            int before = health.hp;
            health.Heal(item.healthRestore);
            if (health.hp > before) applied = true;
        }

        if (item.staminaRestore > 0f && stamina)
        {
            float restored = stamina.Restore(item.staminaRestore);
            if (restored > 0f) applied = true;
        }

        return applied;
    }
}
