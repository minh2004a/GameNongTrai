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
    [SerializeField] string loadingBaseText = "Đang tải";
    [SerializeField, Min(1)] int maxLoadingDots = 3;
    [SerializeField, Min(0f)] float dotAnimationInterval = 0.3f;
    [SerializeField, Min(0f)] float minimumLoadingScreenTime = 1.5f;

    bool isLoading;

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

        Time.timeScale = 1f;
    }

    public void OnClickNewGame()
    {
        if (isLoading)
        {
            return;
        }

        if (SaveStore.HasAnySave() && confirmWipePanel)
        {
            confirmWipePanel.SetActive(true);
            return;
        }

        StartNewGame();
    }

    public void OnConfirmWipe()
    {
        if (isLoading)
        {
            return;
        }

        if (confirmWipePanel)
        {
            confirmWipePanel.SetActive(false);
        }

        StartNewGame();
    }

    public void OnCancelWipe()
    {
        if (confirmWipePanel)
        {
            confirmWipePanel.SetActive(false);
        }
    }

    void StartNewGame()
    {
        DisableMenuButtons();

        var persistent = persistentScene;
        var startMap = startMapScene;
        isLoading = LoadingScreenService.Instance.RunLoadingOperation(
            () => StartNewGameRoutine(persistent, startMap),
            BuildLoadingConfig());

        if (!isLoading)
        {
            RestoreMenuButtons();
        }
    }

    public void OnClickContinue()
    {
        if (isLoading || !SaveStore.HasAnySave())
        {
            return;
        }

        DisableMenuButtons();

        var persistent = persistentScene;
        var startMap = startMapScene;
        isLoading = LoadingScreenService.Instance.RunLoadingOperation(
            () => ContinueRoutine(persistent, startMap),
            BuildLoadingConfig());

        if (!isLoading)
        {
            RestoreMenuButtons();
        }
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator StartNewGameRoutine(string persistent, string startMap)
    {
        SaveStore.NewGame(startMap);

        yield return SceneManager.LoadSceneAsync(persistent, LoadSceneMode.Single);
        yield return SceneManager.LoadSceneAsync(startMap, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(startMap));
    }

    IEnumerator ContinueRoutine(string persistent, string defaultScene)
    {
        SaveStore.LoadFromDisk();

        yield return SceneManager.LoadSceneAsync(persistent, LoadSceneMode.Single);

        var sceneToLoad = SaveStore.GetLastScene();
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = defaultScene;
        }

        yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
    }

    LoadingScreenService.Config BuildLoadingConfig()
    {
        return new LoadingScreenService.Config
        {
            BaseText = loadingBaseText,
            MaxDots = maxLoadingDots,
            DotInterval = dotAnimationInterval,
            MinimumDuration = minimumLoadingScreenTime
        };
    }

    void DisableMenuButtons()
    {
        if (newGameButton)
        {
            newGameButton.interactable = false;
        }

        if (continueButton)
        {
            continueButton.interactable = false;
        }

        if (quitButton)
        {
            quitButton.interactable = false;
        }
    }

    void RestoreMenuButtons()
    {
        if (newGameButton)
        {
            newGameButton.interactable = true;
        }

        if (continueButton)
        {
            continueButton.interactable = SaveStore.HasAnySave();
        }

        if (quitButton)
        {
            quitButton.interactable = true;
        }

        isLoading = false;
    }
}
