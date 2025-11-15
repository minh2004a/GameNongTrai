


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
    GameObject currentStagePrefab;
    [SerializeField] PickupItem2D pickupPrefab;
    TimeManager time;
    SeasonManager season;
    string plantId;
    bool removeFromSave;
    bool wasWateredToday;
    int lastWateredDay;
    static readonly HashSet<SeedSO> warnedMissingId = new();
    bool seasonRemovalPending;
    bool isStump;

    public bool IsMature => IsDataValid() && stage >= data.stagePrefabs.Length - 1;
    public bool CanHarvestByHand => IsDataValid() && data.harvestMethod == HarvestMethod.Hand && IsMature;
    bool RequiresWatering => data && data.requiresWatering;

    int CurrentDay
    {
        get
        {
            if (time && time.isActiveAndEnabled) return time.day;
            var tm = FindFirstObjectByType<TimeManager>();
            return tm ? tm.day : SaveStore.PeekSavedDay();
        }
    }

    public void Init(SeedSO seed)
    {
        data = seed;
        stage = 0;
        daysInStage = 0;
        plantId = SaveStore.CreatePlantId();
        wasWateredToday = false;
        int today = Mathf.Max(0, CurrentDay);
        lastWateredDay = RequiresWatering ? Mathf.Max(0, today - 1) : today;
        isStump = false;
        removeFromSave = false;
        currentStagePrefab = null;

        if (!IsDataValid())
        {
            PersistState();
            return;
        }

        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        else targetDaysForStage = 0;

        SpawnStage();
        if (EnsureSeasonSurvival())
        {
            PersistState();
        }
    }

    public void Restore(SeedSO seed, SaveStore.PlantState state)
    {
        data = seed;
        plantId = string.IsNullOrEmpty(state.id) ? SaveStore.CreatePlantId() : state.id;
        stage = 0;
        daysInStage = 0;
        targetDaysForStage = 0;
        wasWateredToday = state.wateredToday;
        lastWateredDay = Mathf.Max(0, state.lastWateredDay);
        isStump = state.isStump;
        removeFromSave = false;
        currentStagePrefab = null;

        if (!IsDataValid())
        {
            PersistState();
            return;
        }

        if (isStump)
        {
            PersistState();
            return;
        }

        int maxStage = data.stagePrefabs.Length - 1;
        stage = Mathf.Clamp(state.stage, 0, Mathf.Max(0, maxStage));
        daysInStage = Mathf.Max(0, state.daysInStage);
        targetDaysForStage = Mathf.Max(0, state.targetDaysForStage);
        if (data.growthMode == GrowthMode.RandomRange && targetDaysForStage <= 0) PickTargetDays();

        ApplyOfflineGrowth(state.lastUpdatedDay);
        SpawnStage();
        if (EnsureSeasonSurvival())
        {
            PersistState();
        }
    }

    public void Water()
    {
        if (!IsDataValid()) return;
        if (isStump) return;
        int today = CurrentDay;
        if (wasWateredToday && lastWateredDay == today) return;
        wasWateredToday = true;
        lastWateredDay = today;
        PersistState();
    }

    public bool TryHarvestByHand(PlayerInventory inv)
    {
        if (isStump) return false;
        if (!CanHarvestByHand) return false;

        var item = data.harvestItem;
        int count = item ? Mathf.Max(0, data.harvestItemCount) : 0;

        if (item && count > 0)
        {
            InventoryAddResult delivery;
            if (inv)
            {
                delivery = inv.AddItemDetailed(item, count);
            }
            else
            {
                delivery = new InventoryAddResult
                {
                    requested = count,
                    remaining = count,
                    addedToBag = 0,
                    addedToHotbar = 0
                };
            }

            if (delivery.remaining > 0)
            {
                SpawnPickup(item, delivery.remaining);
            }
        }

        HandlePostHarvest();
        return true;
    }

    bool IsDataValid()
    {
        return data && data.stagePrefabs != null && data.stagePrefabs.Length > 0;
    }

    void OnEnable()
    {
        time = FindFirstObjectByType<TimeManager>();
        if (time) time.OnNewDay += TickDay;
        season = FindFirstObjectByType<SeasonManager>();
        if (season) season.OnSeasonChanged += HandleSeasonChanged;
    }

    void OnDisable()
    {
        if (time) time.OnNewDay -= TickDay;
        if (season) season.OnSeasonChanged -= HandleSeasonChanged;
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

        if (isStump)
        {
            PersistState();
            return;
        }

        if (IsMature)
        {
            wasWateredToday = false;
            PersistState();
            return;
        }

        if (RequiresWatering && !wasWateredToday)
        {
            wasWateredToday = false;
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
                    wasWateredToday = false;
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
                    wasWateredToday = false;
                    AdvanceStage();
                    advanced = true;
                }
                break;
            }
            case GrowthMode.RandomRange:
            {
                if (targetDaysForStage <= 0) PickTargetDays();
                if (daysInStage >= targetDaysForStage)
                {
                    wasWateredToday = false;
                    AdvanceStage();
                    advanced = true;
                }
                break;
            }
        }

        if (!advanced)
        {
            wasWateredToday = false;
            PersistState();
        }
        else
        {
            wasWateredToday = false;
        }
    }

    void AdvanceStage()
    {
        AdvanceStageInternal(true);
        PersistState();
    }

    void AdvanceStageInternal(bool spawnVisual)
    {
        if (!IsDataValid()) return;
        int maxStage = data.stagePrefabs.Length - 1;
        int nextStage = Mathf.Min(stage + 1, maxStage);
        stage = nextStage;
        daysInStage = 0;
        isStump = false;
        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        currentStagePrefab = null;
        if (spawnVisual) SpawnStage();
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
        if (isStump)
        {
            if (visual)
            {
                Destroy(visual);
                visual = null;
            }
            currentStagePrefab = null;
            return;
        }

        var prefab = data.GetStagePrefabForSeason(stage, ResolveCurrentSeason());
        if (prefab == currentStagePrefab && visual)
        {
            return;
        }

        if (visual)
        {
            Destroy(visual);
            visual = null;
        }

        currentStagePrefab = prefab;
        if (prefab)
        {
            visual = Instantiate(prefab, transform);
        }
    }

    void HandlePostHarvest()
    {
        if (data.destroyOnHarvest)
        {
            RemoveFromSave();
            Destroy(gameObject);
            return;
        }

        stage = 0;
        daysInStage = 0;
        isStump = false;
        if (data.growthMode == GrowthMode.RandomRange) PickTargetDays();
        wasWateredToday = false;
        int today = Mathf.Max(0, CurrentDay);
        lastWateredDay = RequiresWatering ? Mathf.Max(0, today - 1) : today;
        SpawnStage();
        PersistState();
    }

    void SpawnPickup(ItemSO item, int count)
    {
        if (!item || count <= 0) return;
        if (!pickupPrefab)
        {
            Debug.LogWarning("PlantGrowth: Thiếu pickupPrefab, không thể spawn vật phẩm thu hoạch.");
            return;
        }
        var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.Set(item, count);
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
            targetDaysForStage = targetDaysForStage,
            lastUpdatedDay = CurrentDay,
            wateredToday = wasWateredToday,
            lastWateredDay = this.lastWateredDay,
            isStump = isStump
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

    bool EnsureSeasonSurvival()
    {
        if (!data || !data.HasSeasonRestrictions) return true;
        var sm = GetSeasonManager();
        if (!sm) return true;
        if (data.IsSeasonAllowed(sm.CurrentSeason)) return true;
        RemoveForSeason();
        return false;
    }

    void HandleSeasonChanged(SeasonManager.Season newSeason)
    {
        if (!data)
        {
            return;
        }

        if (data.HasSeasonRestrictions && !data.IsSeasonAllowed(newSeason))
        {
            RemoveForSeason();
            return;
        }

        RefreshSeasonVisual();
    }

    void RemoveForSeason()
    {
        if (seasonRemovalPending) return;
        seasonRemovalPending = true;
        RemoveFromSave();
        Destroy(gameObject);
    }

    void ApplyOfflineGrowth(int lastRecordedDay)
    {
        if (!IsDataValid()) return;
        if (isStump) return;

        int now = CurrentDay;
        int last = lastRecordedDay > 0 ? lastRecordedDay : now;
        if (now <= last) return;

        if (data.growthMode == GrowthMode.RandomRange && targetDaysForStage <= 0) PickTargetDays();

        for (int day = last; day < now; day++)
        {
            if (IsMature)
            {
                daysInStage = 0;
                break;
            }

            bool watered = !RequiresWatering || lastWateredDay >= day;
            if (!watered) continue;

            daysInStage++;

            switch (data.growthMode)
            {
                case GrowthMode.FixedDays:
                {
                    int need = (data.stageDays != null && stage < data.stageDays.Length)
                        ? data.stageDays[stage]
                        : 1;
                    if (daysInStage >= need)
                    {
                        AdvanceStageInternal(false);
                    }
                    break;
                }
                case GrowthMode.RandomChance:
                {
                    float p = (data.stageAdvanceChance != null && stage < data.stageAdvanceChance.Length)
                        ? data.stageAdvanceChance[stage]
                        : 0f;
                    if (UnityEngine.Random.value <= p)
                    {
                        AdvanceStageInternal(false);
                    }
                    break;
                }
                case GrowthMode.RandomRange:
                {
                    if (targetDaysForStage <= 0) PickTargetDays();
                    if (daysInStage >= targetDaysForStage)
                    {
                        AdvanceStageInternal(false);
                    }
                    break;
                }
            }
        }

        wasWateredToday = false;
    }

    public void ReplaceWithStump(GameObject stumpPrefab)
    {
        if (removeFromSave) removeFromSave = false;
        if (string.IsNullOrEmpty(plantId)) plantId = SaveStore.CreatePlantId();

        if (!IsDataValid())
        {
            RemoveFromSave();
            Destroy(gameObject);
            return;
        }

        if (!stumpPrefab)
        {
            RemoveFromSave();
            Destroy(gameObject);
            return;
        }

        isStump = true;
        stage = Mathf.Clamp(data.stagePrefabs.Length - 1, 0, data.stagePrefabs.Length - 1);
        daysInStage = 0;
        currentStagePrefab = null;
        var parent = transform.parent;
        var stump = Instantiate(stumpPrefab, transform.position, transform.rotation, parent);
        var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
        tag.treeId = plantId;

        PersistState();
        Destroy(gameObject);
    }

    public static GameObject ResolveStumpPrefab(SeedSO seed)
    {
        if (!seed) return null;
        var sm = Object.FindFirstObjectByType<SeasonManager>();
        var season = sm ? sm.CurrentSeason : SeasonManager.Season.Spring;

        foreach (var prefab in seed.EnumerateAllStagePrefabs())
        {
            if (!prefab) continue;
            var target = prefab.GetComponentInChildren<TreeChopTarget>(true);
            if (!target) continue;
            var seasonalStump = target.GetSeasonalStumpPrefab(season);
            if (seasonalStump) return seasonalStump;
            if (target.stumpPrefab) return target.stumpPrefab;
        }
        return null;
    }

    void RefreshSeasonVisual()
    {
        if (!IsDataValid()) return;
        if (isStump) return;
        currentStagePrefab = null;
        SpawnStage();
        PersistState();
    }

    SeasonManager GetSeasonManager()
    {
        if (season && season.isActiveAndEnabled) return season;
        season = FindFirstObjectByType<SeasonManager>();
        return season;
    }

    SeasonManager.Season ResolveCurrentSeason()
    {
        var sm = GetSeasonManager();
        return sm ? sm.CurrentSeason : SeasonManager.Season.Spring;
    }
}
