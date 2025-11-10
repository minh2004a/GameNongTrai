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

    [Header("Audio")]
    public AudioSource openAudioSource;
    public AudioClip openSound;

    [Header("Input System")]
    public InputActionReference interact; // trỏ tới action "Interact" trong .inputactions

    bool inside, busy;

    void Reset(){ GetComponent<Collider2D>().isTrigger = true; }
    void OnEnable(){ if (interact) interact.action.Enable(); }
    void OnDisable(){ if (interact) interact.action.Disable(); }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!c.CompareTag("Player")) return;
        inside = true;
        if (mode == Mode.AutoOnEnter && !busy)
        {
            busy = true;
            if (SceneLoaderHost.IsSwitching) return;
            PlayOpenSound();
            SceneLoaderHost.I.Switch(targetScene, targetSpawnId);  // gọi host
        }
    }
    void OnTriggerExit2D(Collider2D c){ if (c.CompareTag("Player")) inside = false; }

    void Update()
    {
        if (busy || mode != Mode.InteractAction || !inside || interact == null) return;
        if (interact.action.WasPressedThisFrame())
        {
            busy = true;
            if (SceneLoaderHost.IsSwitching) return;
            PlayOpenSound();
            SceneLoaderHost.I.Switch(targetScene, targetSpawnId);  // gọi host
        }
    }
    IEnumerator Go()
    {
        if (busy) yield break;
        busy = true;

        PlayOpenSound();

        var fader = ScreenFader.I;
        if (fader != null) yield return fader.FadeOut(0.35f);

        var cur = SceneManager.GetActiveScene();

        var load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        if (load != null)
        {
            yield return load;

            var newScene = SceneManager.GetSceneByName(targetScene);
            if (newScene.IsValid()) SceneManager.SetActiveScene(newScene);

            var player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player != null)
            {
                foreach (var sp in Object.FindObjectsOfType<SpawnPoint>())
                {
                    if (sp.id == targetSpawnId) { player.position = sp.transform.position; break; }
                }
            }

            yield return SceneManager.UnloadSceneAsync(cur);
        }

        if (fader != null) yield return fader.FadeIn(0.35f);
        busy = false;
    }

    void PlayOpenSound()
    {
        if (openAudioSource != null)
        {
            if (openSound != null)
            {
                openAudioSource.PlayOneShot(openSound);
            }
            else
            {
                openAudioSource.Play();
            }
        }
        else if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }
    }
}
