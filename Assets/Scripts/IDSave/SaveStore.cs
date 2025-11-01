using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveStore
{
    [Serializable] class SceneRecord { public string scene; public List<string> ids = new(); }
    [Serializable] class SaveData { public List<SceneRecord> trees = new(); public List<SceneRecord> stumps = new(); }
    // Hỗ trợ file cũ (chỉ có "scenes")
    [Serializable] class Legacy { public List<SceneRecord> scenes = new(); }

    static readonly Dictionary<string, HashSet<string>> committedTrees  = new();
    static readonly Dictionary<string, HashSet<string>> committedStumps = new();
    static readonly Dictionary<string, HashSet<string>> pendingTrees    = new();
    static readonly Dictionary<string, HashSet<string>> pendingStumps   = new();

    static string PathFile => Path.Combine(Application.persistentDataPath, "save.json");

    public static void LoadFromDisk()
    {
        committedTrees.Clear(); committedStumps.Clear();
        pendingTrees.Clear();   pendingStumps.Clear();

        if (!File.Exists(PathFile)) return;
        var json = File.ReadAllText(PathFile);

        var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        bool empty = (data.trees == null || data.trees.Count == 0) && (data.stumps == null || data.stumps.Count == 0);

        // migrate file cũ
        if (empty && json.Contains("\"scenes\""))
        {
            var old = JsonUtility.FromJson<Legacy>(json) ?? new Legacy();
            foreach (var r in old.scenes) committedTrees[r.scene] = new HashSet<string>(r.ids);
            return;
        }

        foreach (var r in data.trees  ?? new List<SceneRecord>()) committedTrees[r.scene]  = new HashSet<string>(r.ids);
        foreach (var r in data.stumps ?? new List<SceneRecord>()) committedStumps[r.scene] = new HashSet<string>(r.ids);
    }

    public static void SaveToDisk()
    {
        var data = new SaveData();
        foreach (var kv in committedTrees)  data.trees .Add(new SceneRecord{ scene = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in committedStumps) data.stumps.Add(new SceneRecord{ scene = kv.Key, ids = new List<string>(kv.Value) });
        File.WriteAllText(PathFile, JsonUtility.ToJson(data, true));
    }

    // đánh dấu PENDING trong phiên
    public static void MarkTreeChoppedPending(string scene, string id){
        if (!pendingTrees.TryGetValue(scene, out var s)) pendingTrees[scene] = s = new HashSet<string>();
        s.Add(id);
    }
    public static void MarkStumpClearedPending(string scene, string id){
        if (!pendingStumps.TryGetValue(scene, out var s)) pendingStumps[scene] = s = new HashSet<string>();
        s.Add(id);
    }

    // kiểm tra trong phiên (committed ∪ pending)
    public static bool IsTreeChoppedInSession(string scene, string id) =>
        (committedTrees.TryGetValue(scene, out var c) && c.Contains(id)) ||
        (pendingTrees.TryGetValue(scene, out var p) && p.Contains(id));

    public static bool IsStumpClearedInSession(string scene, string id) =>
        (committedStumps.TryGetValue(scene, out var c) && c.Contains(id)) ||
        (pendingStumps.TryGetValue(scene, out var p) && p.Contains(id));

    // người chơi Save/Ngủ
    public static void CommitPendingAndSave(){
        foreach (var kv in pendingTrees){
            if (!committedTrees.TryGetValue(kv.Key, out var set)) committedTrees[kv.Key] = set = new HashSet<string>();
            set.UnionWith(kv.Value);
        }
        foreach (var kv in pendingStumps){
            if (!committedStumps.TryGetValue(kv.Key, out var set)) committedStumps[kv.Key] = set = new HashSet<string>();
            set.UnionWith(kv.Value);
        }
        pendingTrees.Clear(); pendingStumps.Clear();
        SaveToDisk();
    }

    public static void DiscardPending(){ pendingTrees.Clear(); pendingStumps.Clear(); }
}
