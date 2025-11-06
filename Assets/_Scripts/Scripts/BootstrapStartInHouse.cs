using UnityEngine;
using System.Collections;

public class BootstrapStartInHouse : MonoBehaviour
{
    public GameObject playerRoot;   // Prefab hoặc instance sẵn trong scene
    public GameObject cameraRoot;
    public GameObject uiRoot;

    [Header("Start")]
    public string firstScene = "House";
    public string firstSpawnId = "HouseDoorInside";

    void Awake(){
        if (playerRoot) DontDestroyOnLoad(playerRoot);   // chỉ hoạt động trên root objects :contentReference[oaicite:6]{index=6}
        if (cameraRoot) DontDestroyOnLoad(cameraRoot);
        if (uiRoot)     DontDestroyOnLoad(uiRoot);
        DontDestroyOnLoad(gameObject); // chính Bootstrap
    }

    IEnumerator Start(){
        yield return SceneTransition.Load(firstScene, firstSpawnId);
    }
}
