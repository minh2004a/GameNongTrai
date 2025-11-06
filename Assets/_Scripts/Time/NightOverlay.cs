// NightOverlay.cs (có giờ:phút cho bình minh/hoàng hôn)
using UnityEngine;
using UnityEngine.UI;

public class NightOverlay : MonoBehaviour
{
    public TimeManager time;
    public Image overlay;                 // Image full-screen, màu đen
    public float maxAlpha = 0.85f;

    [Header("Bình minh bắt đầu/kết thúc (giờ, phút)")]
    public int dawnStartHour = 5; public int   dawnStartMinute = 0;  // 05:00
    public int dawnEndHour   = 6; public int   dawnEndMinute   = 0;  // 06:00

    [Header("Hoàng hôn bắt đầu/kết thúc (giờ, phút)")]
    public int duskStartHour = 18; public int duskStartMinute = 0;  // 18:00
    public int duskEndHour   = 19; public int  duskEndMinute   = 0;  // 19:00

    [Header("Độ cong chuyển tiếp")]
    public AnimationCurve dawnCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve duskCurve = AnimationCurve.EaseInOut(0,0,1,1);

    int ToMin(int h, int m) => h * 60 + m;

    float Daylight01(int h, int m)
    {
        int t = ToMin(h, m);
        int dawnA = ToMin(dawnStartHour, dawnStartMinute);
        int dawnB = ToMin(dawnEndHour,   dawnEndMinute);
        int duskA = ToMin(duskStartHour, duskStartMinute);
        int duskB = ToMin(duskEndHour,   duskEndMinute);

        if (t < dawnA) return 0f;                                                            // đêm
        if (t < dawnB) return dawnCurve.Evaluate(Mathf.InverseLerp(dawnA, dawnB, t));       // 0→1
        if (t < duskA) return 1f;                                                            // ngày
        if (t < duskB) return 1f - duskCurve.Evaluate(Mathf.InverseLerp(duskA, duskB, t));  // 1→0
        return 0f;                                                                           // đêm
    }

    void Awake(){ if (overlay) overlay.raycastTarget = false; }

    void Update()
    {
        if (!time || !overlay) return;
        float d = Daylight01(time.hour, time.minute);   // 0..1
        float a = Mathf.Lerp(maxAlpha, 0f, d);
        var c = overlay.color; c.a = a; overlay.color = c;
    }
}
