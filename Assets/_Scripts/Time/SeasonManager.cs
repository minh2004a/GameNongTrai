using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tracks the current season based on the in-game day and swaps tilemaps
/// (such as ground and grass) to the matching seasonal variants.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    [Serializable]
    public struct SeasonScheduleEntry
    {
        public Season season;
        [Min(1)] public int startDay;
    }

    [Serializable]
    public struct SeasonTilemapVariant
    {
        public Season season;
        public Tilemap template;
    }

    [Serializable]
    public class SeasonTilemapTarget
    {
        public Tilemap target;
        public SeasonTilemapVariant[] variants;

        Dictionary<Season, Tilemap> cachedLookup;

        public void ApplySeason(Season season)
        {
            if (!target)
            {
                return;
            }

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

            if (cachedLookup.TryGetValue(season, out var cached) && cached)
            {
                return cached;
            }

            if (variants != null)
            {
                foreach (var variant in variants)
                {
                    if (variant.season == season && variant.template)
                    {
                        cachedLookup[season] = variant.template;
                        return variant.template;
                    }
                }
            }

            return null;
        }
    }

    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    [SerializeField] Season defaultSeason = Season.Spring;

    [SerializeField] SeasonScheduleEntry[] seasonSchedule =
    {
        new SeasonScheduleEntry { season = Season.Spring, startDay = 1 },
        new SeasonScheduleEntry { season = Season.Summer, startDay = 31 },
        new SeasonScheduleEntry { season = Season.Fall, startDay = 61 },
        new SeasonScheduleEntry { season = Season.Winter, startDay = 91 },
    };

    [SerializeField] SeasonTilemapTarget[] tilemapTargets;

    [SerializeField] bool applyImmediatelyOnStart = true;

    public event Action<Season> OnSeasonChanged;

    public Season CurrentSeason { get; private set; }

    TimeManager timeManager;
    bool hasAppliedSeason;

    void Awake()
    {
        AttachTimeManager(FindFirstObjectByType<TimeManager>());
    }

    void OnEnable()
    {
        AttachTimeManager(timeManager ?? FindFirstObjectByType<TimeManager>());
    }

    void Start()
    {
        if (applyImmediatelyOnStart)
        {
            RefreshSeason(force: true);
        }
    }

    void OnDisable()
    {
        if (timeManager)
        {
            timeManager.OnNewDay -= HandleNewDay;
        }
    }

    void HandleNewDay()
    {
        RefreshSeason(force: false);
    }

    public void RefreshSeason(bool force)
    {
        int day = GetCurrentDay();
        var determinedSeason = DetermineSeasonForDay(day);
        if (!force && hasAppliedSeason && determinedSeason == CurrentSeason)
        {
            return;
        }

        ApplySeason(determinedSeason);
    }

    public void ForceSeason(Season season)
    {
        ApplySeason(season);
    }

    void ApplySeason(Season season)
    {
        CurrentSeason = season;
        hasAppliedSeason = true;

        if (tilemapTargets != null)
        {
            foreach (var target in tilemapTargets)
            {
                target?.ApplySeason(season);
            }
        }

        OnSeasonChanged?.Invoke(season);
    }

    Season DetermineSeasonForDay(int day)
    {
        if (seasonSchedule == null || seasonSchedule.Length == 0)
        {
            return defaultSeason;
        }

        Season chosenSeason = defaultSeason;
        int bestStart = int.MinValue;
        bool found = false;

        foreach (var entry in seasonSchedule)
        {
            if (entry.startDay <= 0) continue;
            if (entry.startDay <= day && (!found || entry.startDay > bestStart))
            {
                chosenSeason = entry.season;
                bestStart = entry.startDay;
                found = true;
            }
        }

        if (found)
        {
            return chosenSeason;
        }

        // If no entry starts before the current day, fall back to the earliest configured season.
        int earliestStart = int.MaxValue;
        foreach (var entry in seasonSchedule)
        {
            if (entry.startDay > 0 && entry.startDay < earliestStart)
            {
                earliestStart = entry.startDay;
                chosenSeason = entry.season;
            }
        }

        return chosenSeason;
    }

    int GetCurrentDay()
    {
        if (timeManager && timeManager.isActiveAndEnabled)
        {
            return Mathf.Max(1, timeManager.day);
        }

        var fallback = FindFirstObjectByType<TimeManager>();
        if (fallback)
        {
            AttachTimeManager(fallback);
            return Mathf.Max(1, fallback.day);
        }

        return 1;
    }

    void AttachTimeManager(TimeManager tm)
    {
        if (tm == timeManager)
        {
            return;
        }

        if (timeManager)
        {
            timeManager.OnNewDay -= HandleNewDay;
        }

        timeManager = tm;
        if (timeManager && isActiveAndEnabled)
        {
            timeManager.OnNewDay += HandleNewDay;
        }
    }
}

static class SeasonTilemapUtility
{
    public static void CopyTilemap(Tilemap source, Tilemap target)
    {
        if (!source || !target)
        {
            return;
        }

        // Copy general settings.
        target.animationFrameRate = source.animationFrameRate;
        target.color = source.color;
        target.orientation = source.orientation;
        target.orientationMatrix = source.orientationMatrix;
        target.tileAnchor = source.tileAnchor;

        target.ClearAllTiles();

        var bounds = source.cellBounds;
        foreach (var position in bounds.allPositionsWithin)
        {
            var tile = source.GetTile(position);
            if (!tile)
            {
                continue;
            }

            target.SetTile(position, tile);

            var flags = source.GetTileFlags(position);
            target.SetTileFlags(position, TileFlags.None);
            target.SetTransformMatrix(position, source.GetTransformMatrix(position));
            target.SetColor(position, source.GetColor(position));
            target.SetTileFlags(position, flags);
        }

        target.CompressBounds();
        target.RefreshAllTiles();
    }
}
