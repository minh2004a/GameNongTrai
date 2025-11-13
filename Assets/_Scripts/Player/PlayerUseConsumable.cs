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

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!health) health = GetComponent<PlayerHealth>();
        if (!stamina) stamina = GetComponent<PlayerStamina>();
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

        var item = inventory.CurrentItem;
        if (!item || item.category != ItemCategory.Consumable) return false;

        bool applied = ApplyEffects(item);
        if (!applied) return false;

        return inventory.ConsumeSelected();
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
