// PlantGrowth.cs
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public SeedSO data;
    public int stage;           // 0..last
    int daysInStage;
    int targetDaysForStage;     // dùng khi RandomRange
    GameObject visual;
    TimeManager time;

    public bool IsMature => stage >= data.stagePrefabs.Length - 1;

    public void Init(SeedSO seed)
    {
        data = seed;
        stage = 0; daysInStage = 0;
        if (data && data.growthMode == GrowthMode.RandomRange) PickTargetDays();
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

        switch (data.growthMode)
        {
            case GrowthMode.FixedDays:
            {
                int need = (data.stageDays != null && stage < data.stageDays.Length)
                           ? data.stageDays[stage] : 1;
                if (daysInStage >= need) AdvanceStage();
                break;
            }
            case GrowthMode.RandomChance:
            {
                float p = (data.stageAdvanceChance != null && stage < data.stageAdvanceChance.Length)
                          ? data.stageAdvanceChance[stage] : 0f;
                // mỗi ngày có p xác suất lên stage
                if (UnityEngine.Random.value <= p) AdvanceStage(); // Random.value ∈ [0..1]
                break;
            }
            case GrowthMode.RandomRange:
            {
                if (daysInStage >= targetDaysForStage) AdvanceStage();
                break;
            }
        }
    }

    void AdvanceStage(){
        stage = Mathf.Min(stage + 1, data.stagePrefabs.Length - 1);
        daysInStage = 0;
        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        SpawnStage();
    }

    void PickTargetDays(){
        // Lấy [min,max] cho stage hiện tại; nếu thiếu dữ liệu thì dùng 1 ngày
        if (data.stageDayRange != null && stage < data.stageDayRange.Length){
            var r = data.stageDayRange[stage];
            int min = Mathf.Max(1, r.x);
            int max = Mathf.Max(min, r.y);
            // Range int: max là exclusvie → +1 để bao gồm max
            targetDaysForStage = UnityEngine.Random.Range(min, max + 1);
        } else {
            targetDaysForStage = 1;
        }
    }

    void SpawnStage(){
        if (visual) Destroy(visual);
        var prefab = data.stagePrefabs[Mathf.Clamp(stage, 0, data.stagePrefabs.Length - 1)];
        if (prefab) visual = Instantiate(prefab, transform);
    }
}
