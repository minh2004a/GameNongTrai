using UnityEngine;

// Controls the sprite shown for tilled soil based on neighbouring tiles.
[DisallowMultipleComponent]
public class TilledSoilVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer wetOverlayRenderer;
    [SerializeField] Sprite[] connectionSprites = new Sprite[16];
    [SerializeField] Sprite[] wetConnectionSprites = new Sprite[16];
    [SerializeField] SeasonalSpriteOverride[] seasonalOverrides = new SeasonalSpriteOverride[0];
    [SerializeField] bool applySeasonTint = true;
    [SerializeField] Color springTint = Color.white;
    [SerializeField] Color summerTint = new Color(0.98f, 0.96f, 0.9f, 1f);
    [SerializeField] Color fallTint = new Color(0.95f, 0.88f, 0.78f, 1f);
    [SerializeField] Color winterTint = new Color(0.9f, 0.95f, 1f, 1f);

    [System.Serializable]
    class SeasonalSpriteOverride
    {
        public Season season = Season.Spring;
        public Sprite[] connectionSprites = new Sprite[16];
        public Sprite[] wetConnectionSprites = new Sprite[16];
    }

    int cachedTilledMask = -1;
    int cachedWetMask = -1;
    bool cachedIsWet;
    Season activeSeason = Season.Spring;

    void Reset()
    {
        CacheRenderers();
    }

    public void ApplyState(int tilledMask, bool isWet, int wetMask)
    {
        cachedTilledMask = Mathf.Clamp(tilledMask, 0, 15);
        cachedWetMask = Mathf.Clamp(wetMask, 0, 15);
        cachedIsWet = isWet;
        UpdateFromCache();
    }

    public void ApplyMask(int mask)
    {
        ApplyState(mask, false, mask);
    }

    void CacheRenderers()
    {
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (!spriteRenderer)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        if (!wetOverlayRenderer)
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer != spriteRenderer)
                {
                    wetOverlayRenderer = renderer;
                    break;
                }
            }
        }
    }

    public void ApplySeason(Season season)
    {
        if (activeSeason == season) return;
        activeSeason = season;
        ApplyTint();
        UpdateFromCache();
    }

    void UpdateFromCache()
    {
        CacheRenderers();
        ApplyTint();
        if (cachedTilledMask < 0)
        {
            return;
        }

        var drySprite = SelectDrySprite(cachedTilledMask);
        if (drySprite && spriteRenderer && spriteRenderer.sprite != drySprite)
        {
            spriteRenderer.sprite = drySprite;
        }

        ApplyWetOverlay(cachedIsWet, cachedWetMask, cachedTilledMask);
    }

    Sprite SelectDrySprite(int tilledMask)
    {
        var drySprite = SelectSprite(tilledMask, false);
        if (!drySprite)
        {
            drySprite = SelectSprite(0, false);
        }
        return drySprite;
    }

    void ApplyWetOverlay(bool isWet, int wetMask, int fallbackMask)
    {
        if (!isWet)
        {
            if (wetOverlayRenderer)
            {
                wetOverlayRenderer.enabled = false;
            }
            return;
        }

        Sprite overlay = SelectSprite(wetMask, true);
        if (!overlay)
        {
            overlay = SelectSprite(fallbackMask, true);
        }
        if (!overlay)
        {
            overlay = SelectSprite(0, true);
        }

        if (wetOverlayRenderer)
        {
            wetOverlayRenderer.enabled = overlay != null;
            if (overlay && wetOverlayRenderer.sprite != overlay)
            {
                wetOverlayRenderer.sprite = overlay;
            }
        }
        else if (overlay && spriteRenderer)
        {
            // Fallback for prefabs without a dedicated overlay renderer.
            spriteRenderer.sprite = overlay;
        }
    }

    Sprite SelectSprite(int mask, bool wet)
    {
        var sprites = GetSpriteArray(wet);
        if (sprites == null || sprites.Length == 0) return null;
        mask = Mathf.Clamp(mask, 0, Mathf.Min(15, sprites.Length - 1));
        var sprite = sprites[mask];
        if (!sprite && sprites.Length > 0)
        {
            sprite = sprites[0];
        }
        return sprite;
    }

    void ApplyTint()
    {
        if (!applySeasonTint) return;
        var tint = LookupTint(activeSeason);
        if (spriteRenderer)
        {
            spriteRenderer.color = tint;
        }
    }

    Color LookupTint(Season season)
    {
        return season switch
        {
            Season.Spring => springTint,
            Season.Summer => summerTint,
            Season.Fall => fallTint,
            Season.Winter => winterTint,
            _ => springTint,
        };
    }

    Sprite[] GetSpriteArray(bool wet)
    {
        var overrideSet = GetSeasonOverride(activeSeason);
        if (overrideSet != null)
        {
            var seasonalSprites = wet ? overrideSet.wetConnectionSprites : overrideSet.connectionSprites;
            if (seasonalSprites != null && seasonalSprites.Length > 0)
            {
                return seasonalSprites;
            }
        }
        return wet ? wetConnectionSprites : connectionSprites;
    }

    SeasonalSpriteOverride GetSeasonOverride(Season season)
    {
        if (seasonalOverrides == null) return null;
        for (int i = 0; i < seasonalOverrides.Length; i++)
        {
            var entry = seasonalOverrides[i];
            if (entry != null && entry.season == season)
            {
                return entry;
            }
        }
        return null;
    }
}
