// RestoreFromSaveAtBoot.cs
using UnityEngine;

public class RestoreFromSaveAtBoot : MonoBehaviour
{
    [SerializeField] TimeManager timeMgr;
    [SerializeField] PlayerHealth hp;
    [SerializeField] PlayerStamina stamina;
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerEquipment equipment;
    [SerializeField] ItemDB itemDB;

    void Start(){
        // thời gian + vitals bạn đã áp như trước
        SaveStore.GetTime(out var d,out var h,out var m);
        if (timeMgr){ timeMgr.day=d; timeMgr.hour=h; timeMgr.minute=m; }
        SaveStore.GetVitals01(out var hp01, out var sta01);
        if (hp) hp.SetPercent(hp01);
        if (stamina) stamina.SetPercent(sta01);

        if (!equipment && inv) equipment = inv.GetComponent<PlayerEquipment>();
        // inventory
        if (inv && itemDB) SaveStore.ApplyInventory(inv, itemDB);
        if (equipment && itemDB) SaveStore.ApplyEquipment(equipment, itemDB);
    }
}