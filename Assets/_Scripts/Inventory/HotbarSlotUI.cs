

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
// Giao diện cho một ô trên thanh công cụ (hotbar) của người chơi
[RequireComponent(typeof(Button))]
public class HotbarSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] GameObject selectedFrame;

    int idx; HotbarUI owner;

    // state
    bool dragging;
    bool suppressClick;
    Image ghost;
    Canvas rootCanvas;

    public int Index => idx;

    void Awake(){
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas) rootCanvas = rootCanvas.rootCanvas;
    }

   void Update(){
    if (dragging && ghost) ghost.rectTransform.position = Input.mousePosition;
}


    public void Render(ItemStack st, bool selected, int index, HotbarUI ui){
        idx = index; owner = ui;

        if (icon){
            icon.sprite = st.item ? st.item.icon : null;
            icon.enabled = st.item;
        }
        if (countText) countText.text = (st.item && st.count > 1) ? st.count.ToString() : "";

        if (selectedFrame){
            selectedFrame.SetActive(selected);
            selectedFrame.transform.SetAsFirstSibling(); // không che icon
        }

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(()=> { if (!suppressClick) owner.OnClickSlot(idx); });
    }

    public void OnPointerDown(PointerEventData e)
    {
        // Chọn ngay trên Down để không lỡ bắn bằng item cũ
        owner?.OnClickSlot(idx);

        // Chặn hành động dùng item trong frame click UI
        UIInputGuard.MarkClick();

        suppressClick = true;
        if (e.button == PointerEventData.InputButton.Left && icon && icon.enabled && icon.sprite)
        {
            StartDragGhost();
            dragging = true;
            // Không cần onClick trên Up nữa
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (dragging)
        {
            dragging = false;
            if (ghost) Destroy(ghost.gameObject);

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);

            HotbarSlotUI targetHotbar = null;
            InventorySlotUI targetBag = null;
            EquipmentSlotUI targetEquip = null;

            foreach (var r in results)
            {
                if (!targetEquip)
                    targetEquip = r.gameObject.GetComponentInParent<EquipmentSlotUI>();
                if (!targetHotbar)
                    targetHotbar = r.gameObject.GetComponentInParent<HotbarSlotUI>();
                if (!targetBag)
                    targetBag = r.gameObject.GetComponentInParent<InventorySlotUI>();
            }

            if (owner)
            {
                if (targetEquip)
                {
                    EquipmentUI.Instance?.EquipFromHotbar(idx, targetEquip.SlotType);
                }
                else if (targetHotbar)
                {
                    owner.RequestMoveOrMerge(idx, targetHotbar.Index);
                }
                else if (targetBag)
                {
                    owner.RequestMoveHotbarToBag(idx, targetBag.Index);
                }
            }
            return;
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            owner?.OnRightClickSlot(idx);
            return;
        }

        if (!suppressClick)
        {
            if (owner) owner.OnClickSlot(idx);
        }

    }
    void OnDisable()
    {
        if (ghost) Destroy(ghost.gameObject);
        dragging = false; suppressClick = false;
    }

    void StartDragGhost()
    {
        ghost = new GameObject("DragGhost", typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.transform.SetAsLastSibling();
        ghost.sprite = icon.sprite;
        ghost.preserveAspect = true;
        ghost.raycastTarget = false;
        var cg = ghost.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.85f;

        // kích thước giống icon gốc
        ghost.rectTransform.sizeDelta = (icon ? icon.rectTransform.rect.size : new Vector2(48, 48));
        ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        ghost.rectTransform.position = Input.mousePosition;
    }


    void DestroyGhost(){
        if (ghost){
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    void FinishDragAndSwap(){
        // Raycast UI để tìm HotbarSlotUI target
        var ed = EventSystem.current;
        if (!ed){ DestroyGhost(); return; }

        var ped = new PointerEventData(ed);
        ped.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        ed.RaycastAll(ped, results);

        HotbarSlotUI targetHotbar = null;
        InventorySlotUI targetBag = null;

        foreach (var r in results)
        {
            // ưu tiên hotbar nếu muốn
            targetHotbar = r.gameObject.GetComponentInParent<HotbarSlotUI>();
            if (targetHotbar != null) break;

            targetBag = r.gameObject.GetComponentInParent<InventorySlotUI>();
            if (targetBag != null) break;
        }

        if (owner != null)
        {
            if (targetHotbar != null)
            {
                // kéo trong hotbar như cũ
                owner.RequestMoveOrMerge(idx, targetHotbar.Index);
            }
            else if (targetBag != null)
            {
                // kéo từ hotbar sang bag
                owner.RequestMoveHotbarToBag(idx, targetBag.Index);
            }
        }
    }
}
