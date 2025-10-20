// HotbarSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class HotbarSlotUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] GameObject selectedFrame;

    int idx; HotbarUI owner;

    public void Render(ItemStack st, bool selected, int index, HotbarUI ui){
        idx = index; owner = ui;
        if (icon){ icon.sprite = st.item ? st.item.icon : null; icon.enabled = st.item; }
        if (countText) countText.text = (st.item && st.count > 1) ? st.count.ToString() : "";
        if (selectedFrame){
            selectedFrame.SetActive(selected);
            selectedFrame.transform.SetAsFirstSibling(); // không che icon
        }
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(()=> owner.OnClickSlot(idx));
    }
}
