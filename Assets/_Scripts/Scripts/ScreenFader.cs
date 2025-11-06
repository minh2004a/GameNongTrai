using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour {
    public static ScreenFader I;
    public CanvasGroup group;
    Coroutine co;

    void Awake(){
        if (I != null){ Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (!group) group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public Coroutine FadeOut(float d=0.35f)=>StartFade(1f,d);
    public Coroutine FadeIn (float d=0.35f)=>StartFade(0f,d);

    Coroutine StartFade(float target, float dur){
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(FadeCo(target, dur));
        return co;
    }

    IEnumerator FadeCo(float target, float dur)
    {
        float start = group.alpha, t = 0f;
        group.blocksRaycasts = true;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;               // KHÔNG phụ thuộc timeScale
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            group.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }
        group.alpha = target;
        
        group.blocksRaycasts = target >= 0.99f;        // chỉ chặn khi đen hẳn
    }
    
}

