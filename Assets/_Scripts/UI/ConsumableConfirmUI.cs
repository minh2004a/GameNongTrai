using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị bảng xác nhận trước khi dùng vật phẩm tiêu hao.
/// </summary>
public class ConsumableConfirmUI : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_Text itemNameText;
    [SerializeField] Image itemIcon;
    [SerializeField] Button acceptButton;
    [SerializeField] Button cancelButton;
    [SerializeField, TextArea] string messageFormat = "Bạn có muốn sử dụng {0}?";
    [SerializeField] string itemNameFormat = "{0}";

    Action confirmCallback;
    Action cancelCallback;
    ItemSO currentItem;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (acceptButton) acceptButton.onClick.AddListener(OnAcceptClicked);
        if (cancelButton) cancelButton.onClick.AddListener(OnCancelClicked);
        HideImmediate();
    }

    void OnDestroy()
    {
        if (acceptButton) acceptButton.onClick.RemoveListener(OnAcceptClicked);
        if (cancelButton) cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    public void Show(ItemSO item, Action onConfirm, Action onCancel)
    {
        currentItem = item;
        confirmCallback = onConfirm;
        cancelCallback = onCancel;
        UpdateVisuals(item);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        UIInputGuard.MarkClick();
    }

    public void Hide()
    {
        ClearCallbacks();
        HideWithoutClearingCallbacks();
    }

    void HideImmediate()
    {
        ClearCallbacks();
        HideWithoutClearingCallbacks();
    }

    void HideWithoutClearingCallbacks()
    {
        if (group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    void OnAcceptClicked()
    {
        UIInputGuard.MarkClick();
        HideWithoutClearingCallbacks();
        var cb = confirmCallback;
        ClearCallbacks();
        cb?.Invoke();
    }

    void OnCancelClicked()
    {
        UIInputGuard.MarkClick();
        HideWithoutClearingCallbacks();
        var cb = cancelCallback;
        ClearCallbacks();
        cb?.Invoke();
    }

    void ClearCallbacks()
    {
        confirmCallback = null;
        cancelCallback = null;
        currentItem = null;
    }

    void UpdateVisuals(ItemSO item)
    {
        string displayName = FormatItemName(item);
        if (messageText)
        {
            string format = string.IsNullOrWhiteSpace(messageFormat) ? "Bạn có muốn sử dụng {0}?" : messageFormat;
            messageText.text = string.Format(format, displayName);
        }
        if (itemNameText)
        {
            string format = string.IsNullOrWhiteSpace(itemNameFormat) ? "{0}" : itemNameFormat;
            itemNameText.text = string.Format(format, displayName);
        }
        if (itemIcon)
        {
            itemIcon.sprite = item ? item.icon : null;
            itemIcon.enabled = itemIcon.sprite != null;
        }
    }

    string FormatItemName(ItemSO item)
    {
        if (!item) return "vật phẩm";
        if (!string.IsNullOrWhiteSpace(item.id)) return item.id;
        if (!string.IsNullOrWhiteSpace(item.name)) return item.name;
        return "vật phẩm";
    }
}