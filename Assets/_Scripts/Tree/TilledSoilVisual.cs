using UnityEngine;

// Controls the sprite shown for tilled soil based on neighbouring tiles.
[DisallowMultipleComponent]
public class TilledSoilVisual : MonoBehaviour
{
    [System.Serializable]
    struct CornerVisual
    {
        public SpriteRenderer renderer;
        public Sprite sprite;
    }

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer wetOverlayRenderer;
    [SerializeField] Sprite[] connectionSprites = new Sprite[16];
    [SerializeField] Sprite[] wetConnectionSprites = new Sprite[16];
    [SerializeField] CornerVisual[] cornerSprites = new CornerVisual[4];
    [SerializeField] CornerVisual[] wetCornerSprites = new CornerVisual[4];

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
        ApplyCornerVisuals(cornerSprites, tilledMask);
        ApplyCornerVisuals(wetCornerSprites, wetMask, isWet, tilledMask);
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

    void ApplyCornerVisualsInternal(CornerVisual[] visuals, int mask, int fallbackMask)
    {
        int useMask = mask;
        if (useMask == 0 && fallbackMask != 0)
        {
            useMask = fallbackMask;
        }

        bool up = (useMask & (1 << 0)) != 0;
        bool right = (useMask & (1 << 1)) != 0;
        bool down = (useMask & (1 << 2)) != 0;
        bool left = (useMask & (1 << 3)) != 0;

        ApplyCorner(visuals, 0, !(up && left));
        ApplyCorner(visuals, 1, !(up && right));
        ApplyCorner(visuals, 2, !(down && right));
        ApplyCorner(visuals, 3, !(down && left));
    }

    void DisableCorners(CornerVisual[] visuals)
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; ++i)
        {
            if (visuals[i].renderer)
            {
                visuals[i].renderer.enabled = false;
            }
        }
    }

    void ApplyCorner(CornerVisual[] visuals, int index, bool show)
    {
        if (visuals == null || index < 0 || index >= visuals.Length) return;
        var visual = visuals[index];
        var renderer = visual.renderer;
        if (!renderer) return;

        if (!show || !visual.sprite)
        {
            renderer.enabled = false;
            return;
        }

        renderer.enabled = true;
        if (renderer.sprite != visual.sprite)
        {
            renderer.sprite = visual.sprite;
        }
    }

    void ApplyCornerVisuals(CornerVisual[] visuals, int mask, bool isActive, int fallbackMask)
    {
        if (!isActive)
        {
            DisableCorners(visuals);
            return;
        }
        ApplyCornerVisualsInternal(visuals, mask, fallbackMask);
    }

    void ApplyCornerVisuals(CornerVisual[] visuals, int mask)
    {
        if (visuals == null) return;
        ApplyCornerVisualsInternal(visuals, mask, 0);
    }
}