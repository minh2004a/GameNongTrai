
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

    CanvasGroup cg;
    int index;
    InventoryBookUI owner;
    bool locked;
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
        else rootCanvas = FindObjectOfType<Canvas>();
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
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (locked) return;
        owner?.OnSlotClicked(index);
        UIInputGuard.MarkClick();
        suppressClick = true;
        if (icon && icon.enabled && icon.sprite)
        {
            StartDragGhost();
            dragging = ghost != null;
        }
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (locked) return;

        if (dragging)
        {
            dragging = false;
            DestroyGhost();

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            InventorySlotUI targetBag = null;
            HotbarSlotUI targetHotbar = null;
            EquipmentSlotUI targetEquip = null;
            foreach (var r in results)
            {
                targetBag = r.gameObject.GetComponentInParent<InventorySlotUI>();
                if (targetBag != null) break;
                targetEquip = r.gameObject.GetComponentInParent<EquipmentSlotUI>();
                if (targetEquip != null) break;
                targetHotbar = r.gameObject.GetComponentInParent<HotbarSlotUI>();
                if (targetHotbar != null) break;
            }

            if (owner != null)
            {
                if (targetBag)
                    owner.RequestMoveOrMergeBag(index, targetBag.Index);
                else if (targetEquip)
                    owner.RequestMoveBagToEquipment(index, targetEquip.Index);
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
        suppressClick = false;
    }
    void StartDragGhost()
    {
        if (rootCanvas == null)
        {
            Debug.LogWarning("Bag drag ghost skipped because no Canvas was found in parents.", this);
            return;
        }

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
