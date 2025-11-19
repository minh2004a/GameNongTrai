// SaveOnSleep.cs
using UnityEngine;

public class SaveOnSleep : MonoBehaviour
{
    [SerializeField] SleepManager sleep;
    [SerializeField] TimeManager timeMgr;
    [SerializeField] PlayerHealth hp;
    [SerializeField] PlayerStamina stamina;
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerEquipment equipment;
    [SerializeField] ItemDB itemDB;

    void Awake(){
        if (!sleep)   sleep   = FindObjectOfType<SleepManager>(true);
        if (!timeMgr) timeMgr = FindObjectOfType<TimeManager>(true);
        if (!hp)      hp      = FindObjectOfType<PlayerHealth>(true);
        if (!stamina) stamina = FindObjectOfType<PlayerStamina>(true);
        if (!inv)     inv     = FindObjectOfType<PlayerInventory>(true);
        if (!equipment && inv) equipment = inv.GetComponent<PlayerEquipment>();
    }
    void OnEnable(){ if (sleep) sleep.OnSaveRequested += Handle; }
    void OnDisable(){ if (sleep) sleep.OnSaveRequested -= Handle; }

    void Handle(){
        // thời gian + máu + thể lực: bạn đã thêm trước đó
        SaveStore.SetTime(timeMgr.day, timeMgr.hour, timeMgr.minute);
        float hp01 = hp ? (float)hp.hp / Mathf.Max(1, hp.maxHP) : 1f;
        float sta01 = stamina ? stamina.Ratio : 1f;
        SaveStore.SetVitals01(hp01, sta01);

        // inventory
        SaveStore.CaptureInventory(inv, itemDB);
        SaveStore.CaptureEquipment(equipment, itemDB);

        SaveStore.SetLastScene("House");
        SaveStore.CommitPendingAndSave();
    }
}
