using UnityEngine;

// Controls the sprite shown for tilled soil based on neighbouring tiles.
[DisallowMultipleComponent]
public class TilledSoilVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite[] connectionSprites = new Sprite[16];
    [SerializeField] Sprite[] wetConnectionSprites = new Sprite[16];

    void Reset()
    {
        CacheRenderer();
    }

    public void ApplyState(int tilledMask, bool isWet, int wetMask)
    {
        CacheRenderer();

        Sprite sprite = SelectSprite(isWet ? wetMask : tilledMask, isWet);
        if (!sprite)
        {
            sprite = SelectSprite(tilledMask, false);
        }
        if (!sprite)
        {
            sprite = SelectSprite(0, false);
        }

        if (spriteRenderer && spriteRenderer.sprite != sprite)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public void ApplyMask(int mask)
    {
        ApplyState(mask, false, mask);
    }

    void CacheRenderer()
    {
        if (spriteRenderer) return;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
