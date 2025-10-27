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

    [Header("Long press drag")]
    [Tooltip("Giữ bao lâu để bật chế độ kéo")]
    public float holdSeconds = 2f;

    int idx; HotbarUI owner;

    // state
    bool pointerDown;
    float pressTime;
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
    if (!pointerDown) return;

    if (!dragging && Time.time - pressTime >= holdSeconds){
        if (icon && icon.enabled && icon.sprite){
            StartDragGhost();
            dragging = true;
            suppressClick = true;
        } else {
            pointerDown = false;
        }
    }
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
        pointerDown = true;
        pressTime = Time.time;
        suppressClick = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        pointerDown = false;

        if (dragging)
        {
            dragging = false;
            if (ghost) Destroy(ghost.gameObject);

            // Raycast tìm ô đích dưới con trỏ rồi hoán đổi/gộp
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            HotbarSlotUI target = null;
            foreach (var r in results)
            {
                target = r.gameObject.GetComponentInParent<HotbarSlotUI>();
                if (target) break;
            }
            if (target && owner) owner.RequestMoveOrMerge(idx, target.Index);
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
        dragging = false; pointerDown = false; suppressClick = false;
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

        HotbarSlotUI target = null;
        foreach (var r in results){
            target = r.gameObject.GetComponentInParent<HotbarSlotUI>();
            if (target != null) break;
        }

        if (target != null && owner != null){
            owner.RequestMoveOrMerge(idx, target.Index);
        }
    }
}
