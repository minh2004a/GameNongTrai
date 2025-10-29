using System.Collections;
using UnityEngine;

public class ExhaustionWatcher : MonoBehaviour
{
    public PlayerHealth hp;
    public PlayerStamina sta;
    public TimeManager tm;
    public Transform bedSpawn;   // đặt điểm giường trong Inspector
    public BedSleepZone bedZone;   // kéo thả SleepZone vào đây
    bool busy;

    void Update()
    {
        if (busy) return;
        bool hpZero = hp && hp.hp <= 0;
        bool staFaint = sta && sta.IsFainted;      // chỉ ngất mới kéo về giường
        if (hpZero || staFaint) StartCoroutine(HandleExhaustion(hpZero, staFaint));
    }


    IEnumerator HandleExhaustion(bool hpZero, bool staFaint)
    {
        busy = true; yield return null;
        if (bedZone) bedZone.SuppressPromptOnce();
        if (bedSpawn) transform.position = bedSpawn.position;

        if (sta && staFaint) sta.SetPercent(0.5f); // hồi 50% năng lượng khi ngất
        if (hp && hpZero) hp.SetPercent(0.5f);  // nếu bạn vẫn muốn xử lý chết = 50% máu

        if (tm) tm.SleepToNextMorning();
        busy = false;
    }
}
