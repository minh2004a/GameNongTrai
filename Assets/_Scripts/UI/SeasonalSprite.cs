using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SeasonalSprite : MonoBehaviour
{
    [Header("Sprite theo mùa")]
    public Sprite springSprite;
    public Sprite summerSprite;
    public Sprite fallSprite;
    public Sprite winterSprite;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ApplyCurrentSeason();
    }

    void OnEnable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged += HandleSeasonChanged;
    }

    void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= HandleSeasonChanged;
    }

    void HandleSeasonChanged(SeasonManager.Season season)
    {
        ApplyCurrentSeason();
    }

    public void ApplyCurrentSeason()
    {
        if (SeasonManager.Instance == null) return;

        var season = SeasonManager.Instance.CurrentSeason;
        Sprite s = null;

        switch (season)
        {
            case SeasonManager.Season.Spring: s = springSprite; break;
            case SeasonManager.Season.Summer: s = summerSprite; break;
            case SeasonManager.Season.Fall:   s = fallSprite;   break;
            case SeasonManager.Season.Winter: s = winterSprite; break;
        }

        if (s != null)
            sr.sprite = s;
    }
}
