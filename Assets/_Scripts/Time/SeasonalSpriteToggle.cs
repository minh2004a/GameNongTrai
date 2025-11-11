using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SeasonalSpriteToggle : MonoBehaviour
{
    [SerializeField] SeasonManager.Season[] enabledSeasons = { SeasonManager.Season.Winter };

    SpriteRenderer spriteRenderer;
    SeasonManager seasonManager;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        AttachSeasonManager();
        ApplyCurrentSeason();
    }

    void Start()
    {
        if (!seasonManager)
        {
            AttachSeasonManager();
            ApplyCurrentSeason();
        }
    }

    void OnDisable()
    {
        if (seasonManager)
        {
            seasonManager.OnSeasonChanged -= HandleSeasonChanged;
            seasonManager = null;
        }
    }

    void AttachSeasonManager()
    {
        if (seasonManager)
            return;

        seasonManager = FindFirstObjectByType<SeasonManager>();
        if (seasonManager)
            seasonManager.OnSeasonChanged += HandleSeasonChanged;
    }

    void HandleSeasonChanged(SeasonManager.Season season)
    {
        Apply(season);
    }

    void ApplyCurrentSeason()
    {
        var current = seasonManager ? seasonManager.CurrentSeason : SeasonManager.Season.Spring;
        Apply(current);
    }

    void Apply(SeasonManager.Season season)
    {
        if (!spriteRenderer)
            return;

        bool shouldEnable = ShouldEnable(season);
        if (spriteRenderer.enabled != shouldEnable)
            spriteRenderer.enabled = shouldEnable;
    }

    bool ShouldEnable(SeasonManager.Season season)
    {
        if (enabledSeasons == null || enabledSeasons.Length == 0)
            return false;

        foreach (var s in enabledSeasons)
            if (s == season)
                return true;

        return false;
    }
}
