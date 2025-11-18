using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] EquipSlotType slotType = EquipSlotType.Hat;
    [SerializeField] Image defaultIcon;   // icon khi chưa gắn
    [SerializeField] Image itemIcon;      // icon khi đã gắn

    public EquipSlotType SlotType => slotType;

    void Reset()
    {
        // auto gán sơ sơ, nhưng tốt nhất em tự kéo tay trong Inspector cho đúng
        if (!defaultIcon)
        {
            var imgs = GetComponentsInChildren<Image>();
            if (imgs.Length > 1) defaultIcon = imgs[1];
        }
    }

    public void Render(ItemSO item)
    {
        if (item != null)
        {
            // có item: hiện icon item, tắt icon mặc định
            if (defaultIcon) defaultIcon.enabled = false;

            if (itemIcon)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = item.icon;
            }
        }
        else
        {
            // không có item: chỉ hiện icon mặc định
            if (itemIcon) itemIcon.enabled = false;
            if (defaultIcon) defaultIcon.enabled = true;
        }
    }

    // nếu em có làm chuột phải để tháo đồ
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            EquipmentUI.Instance?.Unequip(slotType);
        }
    }
}
