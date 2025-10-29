using UnityEngine;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("Sprite + kích thước")]
    public Sprite sprite;
    [Range(0.25f, 10f)] public float size = 1f;
    public Vector2 hotspotPixels = Vector2.zero;

    [Header("Canvas (Screen Space - Overlay)")]
    public Canvas canvas;

    [Header("Chống nhấp biên")]
    [Range(0f, 16f)] public float borderPadding = 1f;

    RectTransform rt;
    Image img;
    Vector2 spritePx;
    bool lastInside = false;
    
    void Reset() { canvas = FindObjectOfType<Canvas>(); }

    void Awake()
    {
        if (!canvas) canvas = FindObjectOfType<Canvas>();

        var go = new GameObject("UICursor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        img = go.GetComponent<Image>();
        rt  = go.GetComponent<RectTransform>();
        img.raycastTarget = false;

        ApplySprite();
        Toggle(false); // mặc định ngoài game
    }

    void ApplySprite()
    {
        img.sprite = sprite;
        img.preserveAspect = true;
        if (!sprite) return;

        spritePx = sprite.rect.size;
        rt.sizeDelta = spritePx * size;

        var pv = new Vector2(
            Mathf.Clamp01(spritePx.x <= 0 ? 0.5f : hotspotPixels.x / spritePx.x),
            Mathf.Clamp01(spritePx.y <= 0 ? 0.5f : hotspotPixels.y / spritePx.y)
        );
        rt.pivot = pv;
    }

    void OnValidate()
    {
        if (rt && img) ApplySprite();
    }

    void LateUpdate()
    {
        // cập nhật vị trí UI-cursor
        if (rt && canvas)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var pos
            );
            rt.anchoredPosition = pos;
        }

        // phát hiện vào/ra cửa sổ game theo tọa độ chuột + focus
        bool nowInside = Application.isFocused && IsPointerInGameWindow();
        if (nowInside != lastInside)
        {
            lastInside = nowInside;
            Toggle(nowInside);
        }
    }

    bool IsPointerInGameWindow()
    {
        var p = Input.mousePosition;
        return p.x >= -borderPadding &&
               p.y >= -borderPadding &&
               p.x <  Screen.width  + borderPadding &&
               p.y <  Screen.height + borderPadding;
    }

    void Toggle(bool inside)
    {
        Cursor.visible = !inside;     // trong game: ẩn chuột hệ thống
        if (img) img.enabled = inside; // trong game: hiện UI-cursor
    }

    // đổi sprite/kích thước lúc runtime
    public void SetCursor(Sprite s, float newSize, Vector2 newHotspotPx)
    {
        sprite = s; size = newSize; hotspotPixels = newHotspotPx;
        ApplySprite();
    }
}
