using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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
        if (!pointerDown || dragging) return;

        if (pointerDown && Time.time - pressTime >= holdSeconds){
            // chỉ cho kéo khi có item
            if (icon && icon.enabled && icon.sprite){
                StartDragGhost();
                dragging = true;
                suppressClick = true;
            }else{
                // không có item thì không vào kéo
                pointerDown = false;
            }
        }

        if (dragging && ghost){
            ghost.rectTransform.position = Input.mousePosition;
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

    public void OnPointerDown(PointerEventData eventData){
        pointerDown = true;
        pressTime = Time.time;
        suppressClick = false; // cho phép click nếu không vào kéo
    }

    public void OnPointerUp(PointerEventData eventData){
        if (dragging){
            // kết thúc kéo: tìm slot dưới con trỏ
            FinishDragAndSwap();
        }
        pointerDown = false;
        dragging = false;
        DestroyGhost();
        // click sẽ bị chặn nếu đã kéo (suppressClick=true). Nếu không kéo, onClick vẫn chạy bình thường.
    }

    void StartDragGhost(){
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (!rootCanvas) return;

        var go = new GameObject("HotbarDragGhost", typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(rootCanvas.transform, false);
        ghost = go.GetComponent<Image>();
        ghost.raycastTarget = false;
        ghost.sprite = icon ? icon.sprite : null;
        ghost.SetNativeSize();
        go.GetComponent<CanvasGroup>().alpha = 0.8f;
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
