// TimeRestoreAtBoot.cs  (gắn vào Persistent)
using UnityEngine;

public class TimeRestoreAtBoot : MonoBehaviour
{
    [SerializeField] TimeManager timeMgr;
    void Awake(){
        if (!timeMgr) timeMgr = FindObjectOfType<TimeManager>(true);
        SaveStore.LoadFromDisk(); // idempotent
        if (timeMgr){
            SaveStore.GetTime(out var d, out var h, out var m);
            timeMgr.day = d; timeMgr.hour = h; timeMgr.minute = m;
        }
    }
}
