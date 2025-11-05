using UnityEngine;

// Controls the sprite shown for tilled soil based on neighbouring tiles.
[DisallowMultipleComponent]
public class TilledSoilVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer wetOverlayRenderer;
    [SerializeField] Sprite[] connectionSprites = new Sprite[16];
    [SerializeField] Sprite[] wetConnectionSprites = new Sprite[16];

    void Reset()
    {
        CacheRenderers();
    }

    public void ApplyState(int tilledMask, bool isWet, int wetMask)
    {
        CacheRenderers();

        var drySprite = SelectDrySprite(tilledMask);
        if (drySprite && spriteRenderer && spriteRenderer.sprite != drySprite)
        {
            spriteRenderer.sprite = drySprite;
        }

        ApplyWetOverlay(isWet, wetMask, tilledMask);
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
        var sprites = wet ? wetConnectionSprites : connectionSprites;
        if (sprites == null || sprites.Length == 0) return null;
        mask = Mathf.Clamp(mask, 0, Mathf.Min(15, sprites.Length - 1));
        var sprite = sprites[mask];
        if (!sprite && sprites.Length > 0)
        {
            sprite = sprites[0];
        }
        return sprite;
    }
}
