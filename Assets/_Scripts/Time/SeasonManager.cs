using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls season progression based on the in-game calendar and swaps
/// configured tile variants on the grass and soil tilemaps to match the
/// active season.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3,
    }

    [SerializeField] private TimeManager timeManager;
    [Tooltip("Number of in-game days before advancing to the next season.")]
    [SerializeField] private int daysPerSeason = 28;
    [SerializeField] private SeasonalTilemap[] seasonalTilemaps;

    public event Action<Season> OnSeasonChanged;

    public Season CurrentSeason { get; private set; }

    void OnEnable()
    {
        if (!timeManager)
        {
            timeManager = FindObjectOfType<TimeManager>();
        }

        if (timeManager)
        {
            timeManager.OnNewDay += HandleNewDay;
        }

        UpdateSeason(force: true);
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
        UpdateSeason(force: false);
    }

    void UpdateSeason(bool force)
    {
        if (!timeManager)
        {
            return;
        }

        var newSeason = CalculateSeason(timeManager.day);
        if (!force && newSeason == CurrentSeason)
        {
            return;
        }

        CurrentSeason = newSeason;
        ApplySeasonToTilemaps(newSeason);
        OnSeasonChanged?.Invoke(newSeason);
    }

    Season CalculateSeason(int dayCount)
    {
        if (daysPerSeason <= 0)
        {
            daysPerSeason = 1;
        }

        var seasonIndex = ((dayCount - 1) / daysPerSeason) % 4;
        return (Season)Mathf.Clamp(seasonIndex, 0, 3);
    }

    void ApplySeasonToTilemaps(Season season)
    {
        if (seasonalTilemaps == null)
        {
            return;
        }

        foreach (var seasonalTilemap in seasonalTilemaps)
        {
            if (seasonalTilemap == null)
            {
                continue;
            }

            seasonalTilemap.Apply(season);
        }
    }

    [Serializable]
    class SeasonalTilemap
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private SeasonalTile[] seasonalTiles;

        public void Apply(Season season)
        {
            if (!tilemap || seasonalTiles == null)
            {
                return;
            }

            foreach (var seasonalTile in seasonalTiles)
            {
                if (seasonalTile == null)
                {
                    continue;
                }

                var target = seasonalTile.GetTile(season);
                if (!target)
                {
                    continue;
                }

                var candidates = seasonalTile.GetAllTiles();
                foreach (var candidate in candidates)
                {
                    if (!candidate || candidate == target)
                    {
                        continue;
                    }

                    tilemap.SwapTile(candidate, target);
                }
            }
        }
    }

    [Serializable]
    class SeasonalTile
    {
        [SerializeField] private TileBase spring;
        [SerializeField] private TileBase summer;
        [SerializeField] private TileBase autumn;
        [SerializeField] private TileBase winter;

        public TileBase GetTile(Season season)
        {
            switch (season)
            {
                case Season.Spring: return spring;
                case Season.Summer: return summer;
                case Season.Autumn: return autumn;
                case Season.Winter: return winter;
                default: return spring;
            }
        }

        public TileBase[] GetAllTiles()
        {
            return new[] { spring, summer, autumn, winter };
        }
    }
}
