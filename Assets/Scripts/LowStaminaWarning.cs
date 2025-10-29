using UnityEngine;
using TMPro;
using System.Collections;

public class LowStaminaWarning : MonoBehaviour
{
    [SerializeField] PlayerStamina stamina;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text label;

    [Header("Config")]
    [SerializeField, Range(0f,1f)] float thresholdLow = 0.10f;   // bật khi ≤10%
    [SerializeField, Range(0f,1f)] float thresholdHigh = 0.15f;  // reset khi ≥15%
    [SerializeField] float showSeconds = 2f;

    bool visible;
    bool armed = true;       // CHẶN hiển thị lặp lại khi vẫn ≤10%
    Coroutine co;

    void Awake(){
        if (panel) panel.SetActive(false);
    }
    void OnEnable(){ if (stamina) stamina.OnStamina01.AddListener(OnStamina01); }
    void OnDisable(){ if (stamina) stamina.OnStamina01.RemoveListener(OnStamina01); }

    void OnStamina01(float r)
    {
        // Chỉ cảnh báo một lần cho đến khi vượt lại thresholdHigh
        if (armed && r <= thresholdLow) { Show(); armed = false; }
        if (r >= thresholdHigh)         { if (visible) Hide(); armed = true; }
    }

    void Show()
    {
        if (panel) panel.SetActive(true);
        visible = true;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(AutoHideRealtime()); // dùng thời gian thực
    }

    IEnumerator AutoHideRealtime()
    {
        // KHẮC PHỤC khi bạn có thay đổi Time.timeScale
        float t = 0f;
        while (t < showSeconds) { t += Time.unscaledDeltaTime; yield return null; }
        Hide();
    }

    void Hide()
    {
        if (panel) panel.SetActive(false);
        visible = false;
        if (co != null){ StopCoroutine(co); co = null; }
    }
}
