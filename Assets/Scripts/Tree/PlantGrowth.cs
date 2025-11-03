// PlantGrowth.cs
using System.Collections.Generic;
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public SeedSO data;
    public int stage;           // 0..last
    int daysInStage;
    int targetDaysForStage;     // dùng khi RandomRange
    GameObject visual;
    TimeManager time;
    string plantId;
    bool removeFromSave;
    static readonly HashSet<SeedSO> warnedMissingId = new();

    public bool IsMature => IsDataValid() && stage >= data.stagePrefabs.Length - 1;

    public void Init(SeedSO seed)
    {
        data = seed;
        stage = 0;
        daysInStage = 0;
        plantId = SaveStore.CreatePlantId();

        if (!IsDataValid())
        {
            PersistState();
            return;
        }

        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        else targetDaysForStage = 0;

        SpawnStage();
        PersistState();
    }

    public void Restore(SeedSO seed, SaveStore.PlantState state)
    {
        data = seed;
        plantId = string.IsNullOrEmpty(state.id) ? SaveStore.CreatePlantId() : state.id;
        stage = 0;
        daysInStage = 0;
        targetDaysForStage = 0;

        if (!IsDataValid())
        {
            PersistState();
            return;
        }

        int maxStage = data.stagePrefabs.Length - 1;
        stage = Mathf.Clamp(state.stage, 0, Mathf.Max(0, maxStage));
        daysInStage = Mathf.Max(0, state.daysInStage);
        targetDaysForStage = Mathf.Max(0, state.targetDaysForStage);
        if (data.growthMode == GrowthMode.RandomRange && targetDaysForStage <= 0) PickTargetDays();

        SpawnStage();
        PersistState();
    }

    bool IsDataValid()
    {
        return data && data.stagePrefabs != null && data.stagePrefabs.Length > 0;
    }

    void OnEnable()
    {
        time = FindFirstObjectByType<TimeManager>();
        if (time) time.OnNewDay += TickDay;
    }

    void OnDisable()
    {
        if (time) time.OnNewDay -= TickDay;
        if (removeFromSave) PersistRemoval();
        else PersistState();
    }

    void TickDay()
    {
        if (!IsDataValid())
        {
            PersistState();
            return;
        }

        if (IsMature)
        {
            PersistState();
            return;
        }

        daysInStage++;
        bool advanced = false;

        switch (data.growthMode)
        {
            case GrowthMode.FixedDays:
            {
                int need = (data.stageDays != null && stage < data.stageDays.Length)
                           ? data.stageDays[stage] : 1;
                if (daysInStage >= need)
                {
                    AdvanceStage();
                    advanced = true;
                }
                break;
            }
            case GrowthMode.RandomChance:
            {
                float p = (data.stageAdvanceChance != null && stage < data.stageAdvanceChance.Length)
                          ? data.stageAdvanceChance[stage] : 0f;
                if (UnityEngine.Random.value <= p)
                {
                    AdvanceStage();
                    advanced = true;
                }
                break;
            }
            case GrowthMode.RandomRange:
            {
                if (daysInStage >= targetDaysForStage)
                {
                    AdvanceStage();
                    advanced = true;
                }
                break;
            }
        }

        if (!advanced) PersistState();
    }

    void AdvanceStage()
    {
        if (!IsDataValid()) return;
        stage = Mathf.Min(stage + 1, data.stagePrefabs.Length - 1);
        daysInStage = 0;
        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        SpawnStage();
        PersistState();
    }

    void PickTargetDays()
    {
        if (data.stageDayRange != null && stage < data.stageDayRange.Length)
        {
            var r = data.stageDayRange[stage];
            int min = Mathf.Max(1, r.x);
            int max = Mathf.Max(min, r.y);
            targetDaysForStage = UnityEngine.Random.Range(min, max + 1);
        }
        else
        {
            targetDaysForStage = 1;
        }
    }

    void SpawnStage()
    {
        if (!IsDataValid()) return;
        if (visual) Destroy(visual);
        var prefab = data.stagePrefabs[Mathf.Clamp(stage, 0, data.stagePrefabs.Length - 1)];
        if (prefab) visual = Instantiate(prefab, transform);
    }

    string SceneName => gameObject.scene.IsValid() ? gameObject.scene.name : null;

    SaveStore.PlantState CaptureState()
    {
        return new SaveStore.PlantState
        {
            id = plantId,
            seedId = data ? data.seedId : null,
            x = transform.position.x,
            y = transform.position.y,
            stage = stage,
            daysInStage = daysInStage,
            targetDaysForStage = targetDaysForStage
        };
    }

    void PersistState()
    {
        if (!IsDataValid()) return;
        if (string.IsNullOrEmpty(data.seedId))
        {
            if (warnedMissingId.Add(data))
                Debug.LogWarning("PlantGrowth: Seed thiếu seedId, không thể lưu trạng thái.");
            return;
        }
        if (string.IsNullOrEmpty(plantId)) plantId = SaveStore.CreatePlantId();
        var scene = SceneName;
        if (string.IsNullOrEmpty(scene)) return;
        SaveStore.SetPlantStatePending(scene, CaptureState());
    }

    void PersistRemoval()
    {
        if (string.IsNullOrEmpty(plantId)) return;
        var scene = SceneName;
        if (string.IsNullOrEmpty(scene)) return;
        SaveStore.RemovePlantPending(scene, plantId);
    }

    public void RemoveFromSave()
    {
        removeFromSave = true;
        PersistRemoval();
    }
}
