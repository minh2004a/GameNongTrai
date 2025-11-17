using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;

    CanvasGroup cg;
    int index;
    InventoryBookUI owner;
    bool locked;
    public int Index => index;
    public InventoryBookUI Owner => owner;
    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void Init(int index, InventoryBookUI owner)
    {
        this.index = index;
        this.owner = owner;
    }

    // locked = true => ô bị khoá, mờ, không dùng
    public void Render(ItemStack stack, bool locked)
    {
        this.locked = locked;

        if (cg)
        {
            cg.alpha = locked ? 0.4f : 1f; // mờ mờ khi khoá
            cg.blocksRaycasts = !locked;   // không nhận click khi khoá
        }

        if (locked)
        {
            // ô khoá: không hiện icon, không số
            if (icon)
            {
                icon.enabled = false;
                icon.sprite = null;
            }
            if (countText) countText.text = "";
            return;
        }

        // ô mở bình thường
        if (stack.item != null)
        {
            if (icon)
            {
                icon.enabled = true;
                icon.sprite = stack.item.icon;
            }

            if (countText)
            {
                countText.text = (stack.count > 1)
                    ? stack.count.ToString()
                    : "";
            }
        }
        else
        {
            if (icon)
            {
                icon.enabled = false;
                icon.sprite = null;
            }
            if (countText) countText.text = "";
        }
    }

    public void OnClick()
    {
        if (locked) return;        // chặn click ô khoá
        owner?.OnSlotClicked(index);
    }
}
