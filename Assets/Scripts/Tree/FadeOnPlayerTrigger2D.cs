using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FadeOnPlayerTrigger2D : MonoBehaviour
{
    [Range(0f,1f)] public float fadedAlpha = 0.25f;
    public float fadeDuration = 0.15f;
    public string playerTag = "Player";
    public SpriteRenderer[] renderers; // để trống sẽ tự lấy ở parent

    int insideCount;
    Coroutine co;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // bắt buộc là trigger
    }

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInParent<SpriteRenderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        insideCount++;
        StartFade(fadedAlpha);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
        if (insideCount == 0) StartFade(1f);
    }

    void OnDisable() { if (co != null) StopCoroutine(co); SetAlpha(1f); }

    void StartFade(float target) { if (co != null) StopCoroutine(co); co = StartCoroutine(FadeTo(target)); }

    IEnumerator FadeTo(float target)
    {
        float t = 0f, dur = Mathf.Max(0.0001f, fadeDuration);
        float[] start = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) start[i] = renderers[i].color.a;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color; c.a = Mathf.Lerp(start[i], target, k);
                renderers[i].color = c;
            }
            yield return null;
        }
        SetAlpha(target);
        co = null;
    }

    void SetAlpha(float a)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var c = renderers[i].color; c.a = a; renderers[i].color = c;
        }
    }
}
