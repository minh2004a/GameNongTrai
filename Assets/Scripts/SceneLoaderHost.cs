using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // New Input System
using System.Collections;

public class SceneLoaderHost : MonoBehaviour
{
    public static SceneLoaderHost I;
    public static bool IsSwitching { get; private set; }

    void Awake(){ if (I!=null){ Destroy(gameObject); return; } I=this; DontDestroyOnLoad(gameObject); }

    public void Switch(string targetScene, string targetSpawnId){
        if (IsSwitching) return;
        StartCoroutine(SwitchCo(targetScene, targetSpawnId));
    }

    Transform player; PlayerInput pi; Rigidbody2D rb; RigidbodyConstraints2D rbOld;

    void LockPlayer(){
        var go = GameObject.FindGameObjectWithTag("Player");
        if (!go) return;
        player = go.transform;

        // tắt input
        pi = go.GetComponent<PlayerInput>();
        if (pi) pi.DeactivateInput();

        // đứng yên tuyệt đối
        rb = go.GetComponent<Rigidbody2D>();
        if (rb){
            rbOld = rb.constraints;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    void UnlockPlayer(){
        if (rb){
            rb.constraints = rbOld;
            rb = null;
        }
        if (pi){
            pi.ActivateInput();
            pi = null;
        }
        player = null;
    }

    IEnumerator SwitchCo(string targetScene, string targetSpawnId){
        IsSwitching = true;
        LockPlayer();

        if (ScreenFader.I) yield return ScreenFader.I.FadeOut(0.7f);

        var cur = SceneManager.GetActiveScene();
        var op  = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        yield return op;

        var newScene = SceneManager.GetSceneByName(targetScene);
        if (!newScene.IsValid()) newScene = SceneManager.GetSceneByPath(targetScene);
        SceneManager.SetActiveScene(newScene);

        // đặt vị trí spawn
        if (player){
            foreach (var sp in Object.FindObjectsOfType<SpawnPoint>())
                if (sp.id == targetSpawnId){ player.position = sp.transform.position; break; }
        }

        yield return SceneManager.UnloadSceneAsync(cur);

        if (ScreenFader.I) yield return ScreenFader.I.FadeIn(0.7f);

        UnlockPlayer();
        IsSwitching = false;
    }
}
