using UnityEngine;

using System;

[RequireComponent(typeof(SpriteRenderer))]
public class SeasonalSpriteToggle : MonoBehaviour
{
    [Serializable]
    struct SeasonSprite
    {
        public SeasonManager.Season season;
        public Sprite sprite;
    }

    [SerializeField] SeasonManager.Season[] enabledSeasons = { SeasonManager.Season.Winter };
    [SerializeField] bool disableRendererWhenOutOfSeason = true;
    [SerializeField] SeasonSprite[] seasonalSprites;
    [SerializeField] Sprite fallbackSprite;

    SpriteRenderer spriteRenderer;
    SeasonManager seasonManager;
    Sprite initialSprite;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CacheInitialSprite();
    }

    void OnEnable()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        CacheInitialSprite();
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

    void CacheInitialSprite()
    {
        if (!spriteRenderer)
            return;

        if (!initialSprite)
            initialSprite = spriteRenderer.sprite;

        if (!fallbackSprite)
            fallbackSprite = spriteRenderer.sprite;
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

        if (disableRendererWhenOutOfSeason)
        {
            bool shouldEnable = ShouldEnable(season);
            if (spriteRenderer.enabled != shouldEnable)
                spriteRenderer.enabled = shouldEnable;
        }

        var spriteForSeason = GetSpriteForSeason(season);
        if (!spriteForSeason)
            spriteForSeason = fallbackSprite ? fallbackSprite : initialSprite;

        if (spriteForSeason && spriteRenderer.sprite != spriteForSeason)
            spriteRenderer.sprite = spriteForSeason;
    }

    bool ShouldEnable(SeasonManager.Season season)
    {
        if (enabledSeasons == null || enabledSeasons.Length == 0)
            return true;

        foreach (var s in enabledSeasons)
            if (s == season)
                return true;

        return false;
    }

    Sprite GetSpriteForSeason(SeasonManager.Season season)
    {
        if (seasonalSprites == null)
            return null;

        for (int i = 0; i < seasonalSprites.Length; i++)
        {
            if (seasonalSprites[i].season == season && seasonalSprites[i].sprite)
                return seasonalSprites[i].sprite;
        }

        return null;
    }
}
