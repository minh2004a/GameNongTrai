using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI labelText;

    CanvasGroup cg;
    Canvas rootCanvas;
    int index;
    EquipSlotType slotType;
    EquipmentUI owner;
    bool dragging;
    bool suppressClick;
    Image ghost;
    string slotLabel;

    public int Index => index;
    public EquipSlotType SlotType => slotType;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas) rootCanvas = rootCanvas.rootCanvas;
        else rootCanvas = FindObjectOfType<Canvas>();
    }

    public void Init(int index, EquipSlotType slotType, string label, EquipmentUI owner)
    {
        this.index = index;
        this.slotType = slotType;
        this.owner = owner;
        slotLabel = label;
        Render(default);
    }

    public void Render(ItemStack stack)
    {
        if (stack.item != null)
        {
            if (icon)
            {
                icon.enabled = true;
                icon.sprite = stack.item.icon;
            }
            if (countText)
                countText.text = stack.count > 1 ? stack.count.ToString() : "";
            if (labelText && labelText != countText)
                labelText.text = "";
        }
        else
        {
            if (icon)
            {
                icon.enabled = false;
                icon.sprite = null;
            }
            if (countText)
                countText.text = slotLabel;
            if (labelText && labelText != countText)
                labelText.text = slotLabel;
        }
    }

    void Update()
    {
        if (dragging && ghost)
            ghost.rectTransform.position = Input.mousePosition;
    }

    public void OnPointerDown(PointerEventData e)
    {
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
        if (dragging)
        {
            dragging = false;
            DestroyGhost();

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            EquipmentSlotUI targetEquip = null;
            InventorySlotUI targetBag = null;
            HotbarSlotUI targetHotbar = null;
            foreach (var r in results)
            {
                targetEquip = r.gameObject.GetComponentInParent<EquipmentSlotUI>();
                if (targetEquip != null) break;
                targetBag = r.gameObject.GetComponentInParent<InventorySlotUI>();
                if (targetBag != null) break;
                targetHotbar = r.gameObject.GetComponentInParent<HotbarSlotUI>();
                if (targetHotbar != null) break;
            }

            if (owner != null)
            {
                if (targetEquip)
                    owner.RequestSwapEquipment(index, targetEquip.Index);
                else if (targetBag)
                    owner.RequestMoveEquipmentToBag(index, targetBag.Index);
                else if (targetHotbar)
                    owner.RequestMoveEquipmentToHotbar(index, targetHotbar.Index);
            }
            return;
        }

        if (!suppressClick)
        {
            owner?.RequestSwapEquipment(index, index);
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
            Debug.LogWarning("Equip drag ghost skipped because no Canvas was found in parents.", this);
            return;
        }

        ghost = new GameObject("EquipDragGhost", typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
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
        UIInputGuard.MarkClick();
    }
}
