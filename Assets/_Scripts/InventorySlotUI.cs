using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;

    [Header("Long press drag")]
    [Tooltip("Giữ bao lâu để bật chế độ kéo")]
    public float holdSeconds = 2f;

    CanvasGroup cg;
    int index;
    InventoryBookUI owner;
    bool locked;
    bool pointerDown;
    float pressTime;
    bool dragging;
    bool suppressClick;
    Image ghost;
    Canvas rootCanvas;
    public int Index => index;
    public InventoryBookUI Owner => owner;
    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas) rootCanvas = rootCanvas.rootCanvas;
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

    void Update()
    {
        if (dragging && ghost) ghost.rectTransform.position = Input.mousePosition;
        if (!pointerDown) return;

        if (!dragging && Time.time - pressTime >= holdSeconds)
        {
            if (icon && icon.enabled && icon.sprite)
            {
                StartDragGhost();
                dragging = true;
                suppressClick = true;
            }
            else
            {
                pointerDown = false;
            }
        }
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (locked) return;
        pointerDown = true;
        pressTime = Time.time;

        owner?.OnSlotClicked(index);
        UIInputGuard.MarkClick();
        suppressClick = true;
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (locked) return;
        pointerDown = false;

        if (dragging)
        {
            dragging = false;
            DestroyGhost();

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            InventorySlotUI targetBag = null;
            HotbarSlotUI targetHotbar = null;
            foreach (var r in results)
            {
                targetBag = r.gameObject.GetComponentInParent<InventorySlotUI>();
                if (targetBag != null) break;
                targetHotbar = r.gameObject.GetComponentInParent<HotbarSlotUI>();
                if (targetHotbar != null) break;
            }

            if (owner != null)
            {
                if (targetBag)
                    owner.RequestMoveOrMergeBag(index, targetBag.Index);
                else if (targetHotbar)
                    owner.RequestMoveBagToHotbar(index, targetHotbar.Index);
            }
            return;
        }

        if (!suppressClick)
        {
            if (!locked) owner?.OnSlotClicked(index);
        }
    }
    void OnDisable()
    {
        DestroyGhost();
        dragging = false;
        pointerDown = false;
        suppressClick = false;
    }
    void StartDragGhost()
    {
        ghost = new GameObject("BagDragGhost", typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.transform.SetAsLastSibling();
        ghost.sprite = icon.sprite;
        ghost.preserveAspect = true;
        ghost.raycastTarget = false;
        var cgGhost = ghost.gameObject.AddComponent<CanvasGroup>();
        cgGhost.blocksRaycasts = false;
        cgGhost.alpha = 0.85f;

        ghost.rectTransform.sizeDelta = (icon ? icon.rectTransform.rect.size : new Vector2(48, 48));
        ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        ghost.rectTransform.position = Input.mousePosition;
    }
    void DestroyGhost()
    {
        if (ghost)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    public void OnClick()
    {
        if (locked) return;        // chặn click ô khoá
        owner?.OnSlotClicked(index);
    }
}
