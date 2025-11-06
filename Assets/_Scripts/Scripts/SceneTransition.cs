using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class SceneTransition
{
    public static IEnumerator Load(string sceneName, string spawnId, string unloadScene = null)
    {
        // 1) Load map mới additive
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null; // chờ xong :contentReference[oaicite:3]{index=3}

        // 2) Set active scene sang map mới
        var newScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(newScene);    // cần khi load additive :contentReference[oaicite:4]{index=4}

        // 3) Đưa player tới spawn
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            foreach (var sp in Object.FindObjectsOfType<SpawnPoint>())
                if (sp.id == spawnId) { player.transform.position = sp.transform.position; break; }
        }

        // 4) Unload map cũ
        if (!string.IsNullOrEmpty(unloadScene))
            yield return SceneManager.UnloadSceneAsync(unloadScene); // gỡ GameObjects của scene cũ :contentReference[oaicite:5]{index=5}
    }
}
