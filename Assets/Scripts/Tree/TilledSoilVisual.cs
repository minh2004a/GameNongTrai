using UnityEngine;

// Controls the sprite shown for tilled soil based on neighbouring tiles.
[DisallowMultipleComponent]
public class TilledSoilVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite[] connectionSprites = new Sprite[16];

    void Reset()
    {
        CacheRenderer();
    }

    public void ApplyMask(int mask)
    {
        CacheRenderer();

        if (!spriteRenderer || connectionSprites == null || connectionSprites.Length == 0)
        {
            return;
        }

        mask = Mathf.Clamp(mask, 0, 15);
        Sprite sprite = null;
        if (mask < connectionSprites.Length)
        {
            sprite = connectionSprites[mask];
        }

        if (!sprite)
        {
            sprite = connectionSprites[0];
        }

        if (spriteRenderer.sprite != sprite)
        {
            spriteRenderer.sprite = sprite;
        }
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
}
