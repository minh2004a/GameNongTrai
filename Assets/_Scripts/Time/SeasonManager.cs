using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculates the active <see cref="Season"/> based on the in-game day and raises events when it changes.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    [Serializable]
    public class SeasonDefinition
    {
        public Season season = Season.Spring;
        [Min(1)] public int lengthInDays = 30;
    }

    [SerializeField] TimeManager timeManager;
    [SerializeField] int startingSeasonIndex = 0;
    [SerializeField] int dayOffset = 0;
    [SerializeField] List<SeasonDefinition> seasons = new()
    {
        new SeasonDefinition { season = Season.Spring, lengthInDays = 30 },
        new SeasonDefinition { season = Season.Summer, lengthInDays = 30 },
        new SeasonDefinition { season = Season.Fall, lengthInDays = 30 },
        new SeasonDefinition { season = Season.Winter, lengthInDays = 30 },
    };

    public event Action<Season, Season> OnSeasonChanged;

    public Season CurrentSeason { get; private set; } = Season.Spring;
    public int DayInSeason { get; private set; } = 1;

    int currentSeasonIndex = -1;

    void Reset()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
    }

    void Awake()
    {
        if (!timeManager)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }
    }

    void OnEnable()
    {
        AttachTimeManager(timeManager ?? FindFirstObjectByType<TimeManager>());
        EvaluateSeason(true);
    }

    void OnDisable()
    {
        if (timeManager)
        {
            timeManager.OnNewDay -= HandleNewDay;
        }
    }

    void AttachTimeManager(TimeManager tm)
    {
        if (!tm) return;
        if (timeManager)
        {
            timeManager.OnNewDay -= HandleNewDay;
        }
        timeManager = tm;
        if (isActiveAndEnabled)
        {
            timeManager.OnNewDay += HandleNewDay;
        }
    }

    void HandleNewDay()
    {
        EvaluateSeason(false);
    }

    void EvaluateSeason(bool force)
    {
        if (seasons == null || seasons.Count == 0)
        {
            seasons = new List<SeasonDefinition>
            {
                new SeasonDefinition { season = Season.Spring, lengthInDays = 30 },
                new SeasonDefinition { season = Season.Summer, lengthInDays = 30 },
                new SeasonDefinition { season = Season.Fall, lengthInDays = 30 },
                new SeasonDefinition { season = Season.Winter, lengthInDays = 30 },
            };
        }

        int savedDay = timeManager ? timeManager.day : Mathf.Max(1, SaveStore.PeekSavedDay());
        int targetSeason = ResolveSeasonIndex(savedDay);
        if (!force && targetSeason == currentSeasonIndex)
        {
            UpdateSeasonDay(savedDay);
            return;
        }

        Season previousSeason = currentSeasonIndex >= 0 && currentSeasonIndex < seasons.Count
            ? seasons[currentSeasonIndex].season
            : seasons[Mathf.Clamp(startingSeasonIndex, 0, seasons.Count - 1)].season;

        currentSeasonIndex = targetSeason;
        CurrentSeason = seasons[Mathf.Clamp(currentSeasonIndex, 0, seasons.Count - 1)].season;
        UpdateSeasonDay(savedDay);
        OnSeasonChanged?.Invoke(previousSeason, CurrentSeason);
    }

    int ResolveSeasonIndex(int absoluteDay)
    {
        int count = seasons.Count;
        if (count == 0) return 0;

        int cycleLength = 0;
        foreach (var def in seasons)
        {
            cycleLength += Mathf.Max(1, def.lengthInDays);
        }
        if (cycleLength <= 0) cycleLength = count;

        int startingIndex = Mathf.Clamp(startingSeasonIndex, 0, count - 1);
        int offset = Mathf.Max(0, dayOffset);
        int dayIndex = Mathf.Max(0, absoluteDay - 1 + offset);
        int normalized = cycleLength > 0 ? dayIndex % cycleLength : dayIndex;

        int cumulative = 0;
        for (int step = 0; step < count; step++)
        {
            int i = (startingIndex + step) % count;
            int length = Mathf.Max(1, seasons[i].lengthInDays);
            cumulative += length;
            if (normalized < cumulative)
            {
                return i;
            }
        }

        return startingIndex;
    }

    void UpdateSeasonDay(int absoluteDay)
    {
        int count = seasons.Count;
        int startIndex = Mathf.Clamp(startingSeasonIndex, 0, Mathf.Max(0, count - 1));
        int dayCounter = Mathf.Max(0, absoluteDay - 1 + Mathf.Max(0, dayOffset));

        int cycleLength = 0;
        foreach (var def in seasons)
        {
            cycleLength += Mathf.Max(1, def.lengthInDays);
        }
        if (cycleLength <= 0) cycleLength = Mathf.Max(1, count);
        dayCounter %= cycleLength;

        int cumulative = 0;
        for (int step = 0; step < count; step++)
        {
            int i = (startIndex + step) % count;
            int length = Mathf.Max(1, seasons[i].lengthInDays);
            if (dayCounter < cumulative + length)
            {
                DayInSeason = (dayCounter - cumulative) + 1;
                return;
            }
            cumulative += length;
        }

        DayInSeason = 1;
    }
}
