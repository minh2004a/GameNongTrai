using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    public enum Mode { AutoOnEnter, InteractAction }
    public Mode mode = Mode.AutoOnEnter;

    [Header("Target")]
    public string targetScene;
    public string targetSpawnId;

    [Header("Input System")]
    public InputActionReference interact; // trỏ tới action "Interact" trong .inputactions

    bool inside, busy;

    void Reset(){ GetComponent<Collider2D>().isTrigger = true; }
    void OnEnable(){ if (interact) interact.action.Enable(); }
    void OnDisable(){ if (interact) interact.action.Disable(); }

    void OnTriggerEnter2D(Collider2D c){
        if (!c.CompareTag("Player")) return;
        inside = true;
        if (mode == Mode.AutoOnEnter && !busy) StartCoroutine(Go());
    }
    void OnTriggerExit2D(Collider2D c){ if (c.CompareTag("Player")) inside = false; }

    void Update(){
        if (busy || mode != Mode.InteractAction || !inside || interact == null) return;
        // Input Actions: kiểm tra nhấn trong frame hiện tại
        if (interact.action.WasPressedThisFrame()) StartCoroutine(Go());
    }

    IEnumerator Go(){
        busy = true;
        var cur = SceneManager.GetActiveScene();
        var op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        if (op == null){ Debug.LogError($"LoadSceneAsync null: {targetScene}"); busy = false; yield break; }
        yield return op;

        var newScene = SceneManager.GetSceneByName(targetScene);
        if (!newScene.IsValid()) newScene = SceneManager.GetSceneByPath(targetScene);
        SceneManager.SetActiveScene(newScene);                                     // active scene mới

        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        foreach (var sp in Object.FindObjectsOfType<SpawnPoint>())
            if (sp.id == targetSpawnId){ player.position = sp.transform.position; break; }

        yield return SceneManager.UnloadSceneAsync(cur);                            // gỡ map cũ
        busy = false;
    }
}
