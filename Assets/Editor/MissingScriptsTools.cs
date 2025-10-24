#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MissingScriptsTools
{
    [MenuItem("Tools/Missing Scripts/Find In Selection")]
    static void FindInSelection(){ foreach (var go in Selection.gameObjects) FindInGO(go); }

    [MenuItem("Tools/Missing Scripts/Remove In Selection")]
    static void RemoveInSelection(){
        int removed = 0;
        foreach (var go in Selection.gameObjects)
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        Debug.Log($"Removed {removed} missing scripts from selection.");
    }

    [MenuItem("Tools/Missing Scripts/Find In Scene")]
    static void FindInScene(){
        foreach (var go in Object.FindObjectsOfType<GameObject>(true)) FindInGO(go);
    }

    static void FindInGO(GameObject go){
        var comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
            if (comps[i] == null)
                Debug.LogWarning($"Missing script on '{GetPath(go)}'", go);
        foreach (Transform c in go.transform) FindInGO(c.gameObject);
    }

    static string GetPath(GameObject go){
        string path = go.name;
        while (go.transform.parent != null){
            go = go.transform.parent.gameObject;
            path = go.name + "/" + path;
        }
        return path;
    }
}
#endif
