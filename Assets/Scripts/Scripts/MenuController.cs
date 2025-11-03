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
        SaveStore.NewGame(startMapScene);

        yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);
        yield return SceneManager.LoadSceneAsync(startMapScene, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(startMapScene));
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
        SaveStore.LoadFromDisk();

        yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);

        var sceneToLoad = SaveStore.GetLastScene();
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = startMapScene;
        }

        yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}