using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SeasonManager : MonoBehaviour
{
    [Serializable]
    public struct SeasonScheduleEntry { public Season season; [Min(1)] public int startDay; }
    [Serializable]
    public struct SeasonTilemapVariant { public Season season; public Tilemap template; }

    [Serializable]
    public class SeasonTilemapTarget
    {
        public Tilemap target;
        public SeasonTilemapVariant[] variants;
        Dictionary<Season, Tilemap> cachedLookup;

        public void ApplySeason(Season season)
        {
            if (!target) return;
            var source = GetTemplate(season);
            if (!source)
            {
                Debug.LogWarning($"SeasonTilemapTarget on '{target.name}' has no template for season {season}.", target);
                return;
            }
            SeasonTilemapUtility.CopyTilemap(source, target);
        }

        Tilemap GetTemplate(Season season)
        {
            cachedLookup ??= new Dictionary<Season, Tilemap>();
            if (cachedLookup.TryGetValue(season, out var cached) && cached) return cached;

            if (variants != null)
            {
                foreach (var v in variants)
                    if (v.season == season && v.template)
                    { cachedLookup[season] = v.template; return v.template; }
            }
            return null;
        }
    }

    public enum Season { Spring, Summer, Fall, Winter }

    [Header("Season cycle (simple mode)")]
    [SerializeField] bool useFixedCycle = true;        // BẬT để dùng vòng lặp 28 ngày/mùa
    [SerializeField, Min(1)] int daysPerSeason = 28;   // 28
    int YearLength => daysPerSeason * 4;

    [Header("Optional: legacy schedule (ignored if useFixedCycle = true)")]
    [SerializeField] Season defaultSeason = Season.Spring;
    [SerializeField] SeasonScheduleEntry[] seasonSchedule =
    {
        new SeasonScheduleEntry { season = Season.Spring, startDay = 1 },
        new SeasonScheduleEntry { season = Season.Summer, startDay = 31 },
        new SeasonScheduleEntry { season = Season.Fall,   startDay = 61 },
        new SeasonScheduleEntry { season = Season.Winter, startDay = 91 },
    };

    [Header("Tilemap targets (nếu bạn còn dùng copy-template)")]
    [SerializeField] SeasonTilemapTarget[] tilemapTargets;

    [SerializeField] bool applyImmediatelyOnStart = true;

    public event Action<Season> OnSeasonChanged;
    public Season CurrentSeason { get; private set; }
    public int DayInSeason { get; private set; }   // 1..28
    public int DayInYear   { get; private set; }   // 1..(daysPerSeason*4)

    TimeManager timeManager;
    bool hasAppliedSeason;

    void Awake()  { AttachTimeManager(FindFirstObjectByType<TimeManager>()); }
    void OnEnable(){ AttachTimeManager(timeManager ?? FindFirstObjectByType<TimeManager>()); }
    void Start()  { if (applyImmediatelyOnStart) RefreshSeason(force: true); }
    void OnDisable(){ if (timeManager) timeManager.OnNewDay -= HandleNewDay; }
    void HandleNewDay(){ RefreshSeason(force: false); }

    void Update()
    {
        // phím test nhanh
        if (Input.GetKeyDown(KeyCode.F1)) ForceSeason(Season.Spring);
        if (Input.GetKeyDown(KeyCode.F2)) ForceSeason(Season.Summer);
        if (Input.GetKeyDown(KeyCode.F3)) ForceSeason(Season.Fall);
        if (Input.GetKeyDown(KeyCode.F4)) ForceSeason(Season.Winter);
    }

    // ====== API chính bạn cần ======
    public void SetAbsoluteDay(int day) // ví dụ: 57
    {
        if (timeManager) timeManager.day = Mathf.Max(1, day);
        RefreshSeason(force: true);
    }

    public void SetSeasonAndDay(Season season, int dayInSeason) // ví dụ: Summer, 5
    {
        dayInSeason = Mathf.Clamp(dayInSeason, 1, daysPerSeason);
        int seasonIndex = (int)season;
        int absoluteDay = seasonIndex * daysPerSeason + dayInSeason; // 1..YearLength
        SetAbsoluteDay(absoluteDay);
    }
    // =================================

    public void RefreshSeason(bool force)
    {
        int dayAbs = GetCurrentDayWrapped();            // 1..YearLength (nếu dùng fixed cycle)
        DayInYear   = dayAbs;
        DayInSeason = 1 + ((dayAbs - 1) % daysPerSeason);

        var determinedSeason = useFixedCycle
            ? DetermineSeason_Fixed(dayAbs)
            : DetermineSeason_Legacy(GetCurrentDayRaw());

        if (!force && hasAppliedSeason && determinedSeason == CurrentSeason) return;
        ApplySeason(determinedSeason);
    }

    public void ForceSeason(Season season){ ApplySeason(season); }

    void ApplySeason(Season season)
    {
        CurrentSeason = season;
        hasAppliedSeason = true;

        if (tilemapTargets != null)
            foreach (var t in tilemapTargets) t?.ApplySeason(season);

        OnSeasonChanged?.Invoke(season);
        // (tuỳ chọn) Debug.Log($"Season={CurrentSeason}, DayInSeason={DayInSeason}, DayInYear={DayInYear}");
    }

    // ——— FIXED: mùa 28 ngày, quay vòng ———
    Season DetermineSeason_Fixed(int wrappedDay) // 1..YearLength
    {
        int seasonIndex = (wrappedDay - 1) / daysPerSeason; // 0..3
        return (Season)seasonIndex;
    }

    // ——— LEGACY: theo seasonSchedule ———
    Season DetermineSeason_Legacy(int rawDay)
    {
        if (seasonSchedule == null || seasonSchedule.Length == 0) return defaultSeason;

        Season chosen = defaultSeason;
        int bestStart = int.MinValue; bool found = false;

        foreach (var e in seasonSchedule)
        {
            if (e.startDay <= 0) continue;
            if (e.startDay <= rawDay && (!found || e.startDay > bestStart))
            { chosen = e.season; bestStart = e.startDay; found = true; }
        }
        if (found) return chosen;

        int earliest = int.MaxValue;
        foreach (var e in seasonSchedule)
            if (e.startDay > 0 && e.startDay < earliest)
            { earliest = e.startDay; chosen = e.season; }

        return chosen;
    }

    // ——— Day helpers ———
    int GetCurrentDayRaw()
    {
        if (timeManager && timeManager.isActiveAndEnabled) return Mathf.Max(1, timeManager.day);
        var fb = FindFirstObjectByType<TimeManager>();
        if (fb) { AttachTimeManager(fb); return Mathf.Max(1, fb.day); }
        return 1;
    }

    int GetCurrentDayWrapped()
    {
        int raw = GetCurrentDayRaw();
        if (!useFixedCycle) return raw;                           // không wrap khi dùng legacy
        return 1 + ((raw - 1) % Mathf.Max(1, YearLength));        // 1..YearLength
    }

    void AttachTimeManager(TimeManager tm)
    {
        if (tm == timeManager) return;
        if (timeManager) timeManager.OnNewDay -= HandleNewDay;
        timeManager = tm;
        if (timeManager && isActiveAndEnabled) timeManager.OnNewDay += HandleNewDay;
    }
}

static class SeasonTilemapUtility
{
    public static void CopyTilemap(Tilemap source, Tilemap target)
    {
        if (!source || !target) return;

        target.animationFrameRate = source.animationFrameRate;
        target.color = source.color;
        target.orientation = source.orientation;
        target.orientationMatrix = source.orientationMatrix;
        target.tileAnchor = source.tileAnchor;

        target.ClearAllTiles();
        var bounds = source.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = source.GetTile(pos);
            if (!tile) continue;

            target.SetTile(pos, tile);
            var flags = source.GetTileFlags(pos);
            target.SetTileFlags(pos, TileFlags.None);
            target.SetTransformMatrix(pos, source.GetTransformMatrix(pos));
            target.SetColor(pos, source.GetColor(pos));
            target.SetTileFlags(pos, flags);
        }
        target.CompressBounds();
        target.RefreshAllTiles();
    }
}
