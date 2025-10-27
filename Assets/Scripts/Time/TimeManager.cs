using UnityEngine;
using TMPro;
using System;
// Quản lý thời gian trong game, bao gồm ngày, giờ, và hiển thị UI
public class TimeManager : MonoBehaviour
{
    [SerializeField] RectTransform arrowRoot;
    // [SerializeField] float orbitRadius = 24f; // chỉnh theo kích thước icon
    [SerializeField] int startWeekday = 0;
    // 0=Thứ 2, 1=Thứ 3, … 6=Chủ nhật
    [SerializeField] RectTransform arrow;     // chính cái Image mũi tên

    public event Action OnNewDay;
    public event Action OnNewMinute; // nếu cần
    [Header("Game Time")]
    public int day = 1;
    public int hour = 6;
    public int minute = 0;

    [Tooltip("Số PHÚT game tăng mỗi GIÂY thật")]
    public float minutesPerRealSecond = 1f;

    float acc;

    [Header("UI")]
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text dayText;
    // void Start()
    // {
    //     if (arrow) arrow.anchoredPosition = new Vector2(orbitRadius, 0f);
    // }

    void Update()
    {
        acc += Time.deltaTime * minutesPerRealSecond;
        while (acc >= 1f)
        {
            minute++; acc -= 1f;
            OnNewMinute?.Invoke();
            if (minute >= 60) { minute = 0; hour++; }
            if (hour >= 24) { hour = 0; day++; OnNewDay?.Invoke(); } // phát sự kiện
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        if (timeText) timeText.text = FormatTime12(hour, minute);

        int w = (startWeekday + (day - 1)) % 7;   // tính thứ theo ngày hiện tại
        if (dayText) dayText.text = $"{VN_WEEK[w]}, {day}";

        UpdateArrow();
    }

    // Đảm bảo chỉ quay root
    void UpdateArrow()
    {
        if (!arrowRoot || !arrow) return;
        arrow.localEulerAngles = Vector3.zero;              // Arrow không tự xoay
        // arrow.anchoredPosition = new Vector2(orbitRadius, 0);// reset nếu layout vừa ép
        arrowRoot.localEulerAngles = new Vector3(0, 0, AngleFor(hour, minute));
    }

    static readonly string[] VN_WEEK = {
        "Thứ 2","Thứ 3","Thứ 4","Thứ 5","Thứ 6","Thứ 7","Chủ nhật"
    };

    float Triangle01(int h24, int m)
    {
        int minutes = h24 * 60 + m;
        int since6  = (minutes - 360 + 1440) % 1440;
        float x = since6 / 720f;         // 0..2 cho 24h
        return (x <= 1f) ? x : 2f - x;   // 0→1 (06→18), 1→0 (18→06)
    }
    float AngleFor(int h24, int m)
    {
        int minutes = h24 * 60 + m;
        int since6 = (minutes - 360 + 1440) % 1440; // 0 tại 06:00
        float frac = since6 / 1440f;                // 0..1 cho 24h
        return 180f - 360f * frac;                   // 6h=180°, 12h=90°, 18h=0°, 0h=270°
    }

    string FormatTime12(int h24, int m)
    {
        string ampm = h24 >= 12 ? "PM" : "AM";
        int h12 = h24 % 12; if (h12 == 0) h12 = 12;
        return $"{h12:00}:{m:00} {ampm}";
    }
}
