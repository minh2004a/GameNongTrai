using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenService : MonoBehaviour
{
    public struct Config
    {
        public string BaseText;
        public int MaxDots;
        public float DotInterval;
        public float MinimumDuration;
    }

    static LoadingScreenService instance;

    public static LoadingScreenService Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(LoadingScreenService));
                instance = go.AddComponent<LoadingScreenService>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }
    }

    GameObject overlayRoot;
    Text loadingText;
    Coroutine dotsRoutine;
    string currentBaseText = "Đang tải";
    int currentMaxDots = 3;
    float currentDotInterval = 0.3f;
    bool isRunning;

    public bool RunLoadingOperation(Func<IEnumerator> operationFactory, Config config)
    {
        if (isRunning || operationFactory == null)
        {
            return false;
        }

        StartCoroutine(RunOperation(operationFactory, config));
        return true;
    }

    void EnsureOverlay()
    {
        if (overlayRoot)
        {
            return;
        }

        overlayRoot = new GameObject("LoadingScreen");
        overlayRoot.transform.SetParent(transform, false);

        var canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        overlayRoot.AddComponent<GraphicRaycaster>();

        var backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(overlayRoot.transform, false);
        var backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.75f);

        var backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        var textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(backgroundObj.transform, false);
        loadingText = textObj.AddComponent<Text>();
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;
        loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 48;
        loadingText.text = currentBaseText;

        var textRect = loadingText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(800f, 160f);

        overlayRoot.SetActive(false);
    }

    IEnumerator RunOperation(Func<IEnumerator> operationFactory, Config config)
    {
        isRunning = true;

        EnsureOverlay();
        ApplyConfig(config);
        ShowOverlay();
        yield return null;

        var loadingStart = Time.unscaledTime;
        var operation = operationFactory();
        if (operation != null)
        {
            yield return operation;
        }

        var elapsed = Time.unscaledTime - loadingStart;
        var remaining = Mathf.Max(0f, config.MinimumDuration - elapsed);
        if (remaining > 0f)
        {
            yield return new WaitForSecondsRealtime(remaining);
        }

        HideOverlay();
        isRunning = false;
    }

    void ApplyConfig(Config config)
    {
        currentBaseText = string.IsNullOrEmpty(config.BaseText) ? "Đang tải" : config.BaseText;
        currentMaxDots = Mathf.Max(1, config.MaxDots);
        currentDotInterval = Mathf.Max(0.01f, config.DotInterval);

        if (loadingText)
        {
            loadingText.text = currentBaseText;
        }
    }

    void ShowOverlay()
    {
        if (!overlayRoot)
        {
            return;
        }

        overlayRoot.SetActive(true);

        if (dotsRoutine != null)
        {
            StopCoroutine(dotsRoutine);
        }

        dotsRoutine = StartCoroutine(AnimateDots());
    }

    void HideOverlay()
    {
        if (!overlayRoot)
        {
            return;
        }

        if (dotsRoutine != null)
        {
            StopCoroutine(dotsRoutine);
            dotsRoutine = null;
        }

        if (loadingText)
        {
            loadingText.text = currentBaseText;
        }

        overlayRoot.SetActive(false);
    }

    IEnumerator AnimateDots()
    {
        var wait = new WaitForSecondsRealtime(currentDotInterval);
        var dotCount = 0;

        while (true)
        {
            if (loadingText)
            {
                loadingText.text = currentBaseText + new string('.', dotCount);
            }

            dotCount++;
            if (dotCount > currentMaxDots)
            {
                dotCount = 0;
            }

            yield return wait;
        }
    }
}
