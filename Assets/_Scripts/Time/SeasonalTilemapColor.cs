using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Adjusts the tint of a tilemap whenever the active <see cref="Season"/> changes.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class SeasonalTilemapColor : MonoBehaviour
{
    [SerializeField] Tilemap tilemap;
    [SerializeField] bool includeChildTilemaps = true;
    [SerializeField] Color springColor = Color.white;
    [SerializeField] Color summerColor = new Color(0.98f, 0.97f, 0.92f, 1f);
    [SerializeField] Color fallColor = new Color(0.96f, 0.9f, 0.85f, 1f);
    [SerializeField] Color winterColor = new Color(0.9f, 0.95f, 1f, 1f);

    SeasonManager seasonManager;

    void Reset()
    {
        tilemap = GetComponent<Tilemap>();
    }

    void Awake()
    {
        if (!tilemap)
        {
            tilemap = GetComponent<Tilemap>();
        }
    }

    void OnEnable()
    {
        AttachSeasonManager(seasonManager ?? FindFirstObjectByType<SeasonManager>());
        ApplySeason(GetCurrentSeason());
    }

    void OnDisable()
    {
        if (seasonManager)
        {
            seasonManager.OnSeasonChanged -= HandleSeasonChanged;
        }
    }

    void AttachSeasonManager(SeasonManager manager)
    {
        if (!manager) return;
        if (seasonManager)
        {
            seasonManager.OnSeasonChanged -= HandleSeasonChanged;
        }
        seasonManager = manager;
        if (isActiveAndEnabled)
        {
            seasonManager.OnSeasonChanged += HandleSeasonChanged;
        }
    }

    void HandleSeasonChanged(Season previous, Season current)
    {
        ApplySeason(current);
    }

    Season GetCurrentSeason()
    {
        if (seasonManager) return seasonManager.CurrentSeason;
        var manager = FindFirstObjectByType<SeasonManager>();
        if (manager)
        {
            AttachSeasonManager(manager);
            return manager.CurrentSeason;
        }
        return Season.Spring;
    }

    void ApplySeason(Season season)
    {
        ApplyColor(LookupColor(season));
    }

    Color LookupColor(Season season)
    {
        return season switch
        {
            Season.Spring => springColor,
            Season.Summer => summerColor,
            Season.Fall => fallColor,
            Season.Winter => winterColor,
            _ => springColor,
        };
    }

    void ApplyColor(Color color)
    {
        if (tilemap)
        {
            tilemap.color = color;
        }
        if (!includeChildTilemaps) return;
        foreach (var child in GetComponentsInChildren<Tilemap>())
        {
            if (child == tilemap) continue;
            child.color = color;
        }
    }
}
