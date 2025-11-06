using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Quản lý việc tải scene khi chuyển đổi giữa các khu vực như Nhà và Nông
public class SceneLoader : MonoBehaviour
{
    [SerializeField] string currentScene; // ví dụ "House" lúc start

    void Awake()
    {
        SaveStore.LoadFromDisk();
        SceneManager.sceneLoaded += OnSceneLoaded; // đăng ký 1 lần
    }
    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public IEnumerator SwitchTo(string nextScene)
    {
        if (!string.IsNullOrEmpty(currentScene))
            yield return SceneManager.UnloadSceneAsync(currentScene);

        yield return SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(nextScene)); // khuyến nghị
        currentScene = nextScene;
        SaveStore.SetLastScene(nextScene);
    }
    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // Nếu cần chạy logic sau khi scene House/Farm xong load.
        // Cây sẽ tự kiểm tra trong Awake của TreePersistent.
    }
}
