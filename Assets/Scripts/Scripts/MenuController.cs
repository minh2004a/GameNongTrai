using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] string persistentScene = "Persistent";
    [SerializeField] string startMapScene = "House";

    [Header("UI")]
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button quitButton;
    [SerializeField] GameObject confirmWipePanel;

    [Header("Loading Screen")]
    [SerializeField] GameObject loadingPanel;
    [SerializeField] Text loadingText;
    [SerializeField] string loadingBaseText = "Đang tải";
    [SerializeField, Min(1)] int maxLoadingDots = 3;
    [SerializeField, Min(0f)] float dotAnimationInterval = 0.3f;
    [SerializeField, Min(0f)] float minimumLoadingScreenTime = 1.5f;

    Coroutine loadingDotsRoutine;
    bool autoCreatedLoadingPanel;

    void Awake()
    {
        SaveStore.LoadFromDisk();

        if (continueButton)
        {
            continueButton.interactable = SaveStore.HasAnySave();
        }

        if (confirmWipePanel)
        {
            confirmWipePanel.SetActive(false);
        }

        if (loadingPanel)
        {
            loadingPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void OnClickNewGame()
    {
        if (SaveStore.HasAnySave() && confirmWipePanel)
        {
            confirmWipePanel.SetActive(true);
            return;
        }

        StartCoroutine(StartNewGameRoutine());
    }

    public void OnConfirmWipe()
    {
        if (confirmWipePanel)
        {
            confirmWipePanel.SetActive(false);
        }

        StartCoroutine(StartNewGameRoutine());
    }

    public void OnCancelWipe()
    {
        if (confirmWipePanel)
        {
            confirmWipePanel.SetActive(false);
        }
    }

    IEnumerator StartNewGameRoutine()
    {
        ShowLoadingScreen();
        var loadingStartTime = Time.unscaledTime;

        SaveStore.NewGame(startMapScene);

        yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);
        yield return SceneManager.LoadSceneAsync(startMapScene, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(startMapScene));

        yield return EnsureMinimumLoadingTime(loadingStartTime);
        HideLoadingScreen();
    }

    public void OnClickContinue()
    {
        if (!SaveStore.HasAnySave())
        {
            return;
        }

        StartCoroutine(ContinueRoutine());
    }

    IEnumerator ContinueRoutine()
    {
        ShowLoadingScreen();
        var loadingStartTime = Time.unscaledTime;

        SaveStore.LoadFromDisk();

        yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);

        var sceneToLoad = SaveStore.GetLastScene();
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = startMapScene;
        }

        yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));

        yield return EnsureMinimumLoadingTime(loadingStartTime);
        HideLoadingScreen();
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowLoadingScreen()
    {
        EnsureLoadingUI();

        if (loadingPanel)
        {
            loadingPanel.SetActive(true);

            if (autoCreatedLoadingPanel)
            {
                DontDestroyOnLoad(loadingPanel);
            }
        }

        if (loadingDotsRoutine != null)
        {
            StopCoroutine(loadingDotsRoutine);
        }

        if (loadingText)
        {
            loadingText.text = loadingBaseText;
            loadingDotsRoutine = StartCoroutine(AnimateLoadingDots());
        }
    }

    void HideLoadingScreen()
    {
        if (loadingDotsRoutine != null)
        {
            StopCoroutine(loadingDotsRoutine);
            loadingDotsRoutine = null;
        }

        if (loadingText)
        {
            loadingText.text = loadingBaseText;
        }

        if (loadingPanel)
        {
            loadingPanel.SetActive(false);

            if (autoCreatedLoadingPanel)
            {
                Destroy(loadingPanel);
                loadingPanel = null;
                loadingText = null;
                autoCreatedLoadingPanel = false;
            }
        }
    }

    void OnDestroy()
    {
        if (loadingDotsRoutine != null)
        {
            StopCoroutine(loadingDotsRoutine);
            loadingDotsRoutine = null;
        }

        if (autoCreatedLoadingPanel && loadingPanel)
        {
            Destroy(loadingPanel);
            loadingPanel = null;
            loadingText = null;
            autoCreatedLoadingPanel = false;
        }
    }

    void EnsureLoadingUI()
    {
        if (loadingPanel)
        {
            return;
        }

        loadingPanel = new GameObject("LoadingScreen");
        autoCreatedLoadingPanel = true;

        var canvas = loadingPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = loadingPanel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        loadingPanel.AddComponent<GraphicRaycaster>();

        var backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(loadingPanel.transform, false);
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
        loadingText.text = loadingBaseText;

        var textRect = loadingText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(800f, 160f);

        loadingPanel.SetActive(false);
    }

    IEnumerator AnimateLoadingDots()
    {
        if (maxLoadingDots <= 0)
        {
            maxLoadingDots = 3;
        }

        var dotCount = 0;
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.01f, dotAnimationInterval));

        while (true)
        {
            if (loadingText)
            {
                loadingText.text = loadingBaseText + new string('.', dotCount);
            }

            dotCount = (dotCount + 1) % (maxLoadingDots + 1);

            yield return wait;
        }
    }

    IEnumerator EnsureMinimumLoadingTime(float startTime)
    {
        var targetDuration = Mathf.Max(0f, minimumLoadingScreenTime);
        var elapsed = Time.unscaledTime - startTime;
        var remaining = targetDuration - elapsed;

        if (remaining > 0f)
        {
            yield return new WaitForSecondsRealtime(remaining);
        }
    }
}
