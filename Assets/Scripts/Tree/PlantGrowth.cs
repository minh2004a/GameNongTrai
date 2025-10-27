// PlantGrowth.cs
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public SeedSO data;
    public int stage;           // 0..last
    int daysInStage;
    GameObject visual;          // instance của stage hiện tại
    TimeManager time;

    public bool IsMature => stage >= data.stagePrefabs.Length - 1;

    public void Init(SeedSO seed)
    {
        data = seed;
        stage = 0; daysInStage = 0;
        SpawnStage();
    }

    void OnEnable(){
        time = FindFirstObjectByType<TimeManager>();
        if (time) time.OnNewDay += TickDay;
    }
    void OnDisable(){
        if (time) time.OnNewDay -= TickDay;
    }

    void TickDay(){
        if (!data || IsMature) return;
        daysInStage++;
        int need = data.stageDays != null && stage < data.stageDays.Length ? data.stageDays[stage] : 1;
        if (daysInStage >= need){
            stage = Mathf.Min(stage + 1, data.stagePrefabs.Length - 1);
            daysInStage = 0;
            SpawnStage();
        }
    }

    void SpawnStage(){
        if (visual) Destroy(visual);
        var prefab = data.stagePrefabs[Mathf.Clamp(stage, 0, data.stagePrefabs.Length - 1)];
        if (prefab) visual = Instantiate(prefab, transform);
    }
}
