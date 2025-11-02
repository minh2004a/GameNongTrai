using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
/// <summary>
/// Lưu trữ trạng thái đã lưu của các đối tượng trong game (cây, gốc cây đã chặt).
/// </summary>
public static class SaveStore
{
    [System.Serializable] public struct ItemStackDTO { public string key; public int count; }
    [System.Serializable] public class InventoryDTO {
    public ItemStackDTO[] hotbar;
    public ItemStackDTO[] bag;
    public int selected;
}
    static Meta meta = new Meta();
    [System.Serializable]
    class Meta
    {
        public string lastScene = "House";
        public bool hasSave = false;
        public int day = 1, hour = 6, minute = 0;
        public float hp01 = 1f, sta01 = 1f;
        public InventoryDTO inventory = new InventoryDTO();
    }
    public static void CaptureInventory(PlayerInventory inv, ItemDB db)
    {
        if (!inv || !db) return;
        var dto = new InventoryDTO
        {
            hotbar = new ItemStackDTO[inv.hotbar.Length],
            bag = new ItemStackDTO[inv.bag.Length],
            selected = inv.selected
        };
        for (int i = 0; i < inv.hotbar.Length; i++)
        {
            var s = inv.hotbar[i];
            dto.hotbar[i] = new ItemStackDTO { key = db.GetKey(s.item), count = s.count };
        }
        for (int i = 0; i < inv.bag.Length; i++)
        {
            var s = inv.bag[i];
            dto.bag[i] = new ItemStackDTO { key = db.GetKey(s.item), count = s.count };
        }
        meta.inventory = dto;
        SaveToDisk();
    }
    // Áp từ save → runtime
    public static void ApplyInventory(PlayerInventory inv, ItemDB db)
    {
        if (!inv || !db || meta.inventory == null) return;

        // Hotbar
        int n = Mathf.Min(inv.hotbar.Length, meta.inventory.hotbar?.Length ?? 0);
        for (int i = 0; i < n; i++)
        {
            var d = meta.inventory.hotbar[i];
            var it = db.Find(d.key);
            inv.SetHotbar(i, it, Mathf.Max(0, d.count)); // UI sẽ refresh và fire events
        }
        for (int i = n; i < inv.hotbar.Length; i++) inv.SetHotbar(i, null, 0);

        // Bag
        int m = Mathf.Min(inv.bag.Length, meta.inventory.bag?.Length ?? 0);
        for (int i = 0; i < m; i++)
        {
            var d = meta.inventory.bag[i];
            var it = db.Find(d.key);
            inv.SetBag(i, it, Mathf.Max(0, d.count));
        }
        for (int i = m; i < inv.bag.Length; i++) inv.SetBag(i, null, 0);

        // Selected
        int sel = Mathf.Clamp(meta.inventory.selected, 0, inv.hotbar.Length - 1);
        inv.SelectSlot(sel);
    }
    public static void SetTime(int d,int h,int m){ meta.day=d; meta.hour=h; meta.minute=m; SaveToDisk(); }
    public static void GetTime(out int d,out int h,out int m){ d=meta.day; h=meta.hour; m=meta.minute; }

    public static void SetVitals01(float hp,float sta){
    meta.hp01=Mathf.Clamp01(hp); meta.sta01=Mathf.Clamp01(sta); SaveToDisk();
    }
    public static void GetVitals01(out float hp,out float sta){ hp=meta.hp01; sta=meta.sta01; }

// NewGame: set mặc định đầy
// meta = new Meta { lastScene = ..., hasSave = true, day=1, hour=6, minute=0, hp01=1f, sta01=1f };

    [Serializable] class SceneRecord { public string scene; public List<string> ids = new(); }
    [Serializable]
    class SaveData
    {
        public List<SceneRecord> trees = new();
        public List<SceneRecord> stumps = new();
        public Meta meta = new Meta();
    }
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
        meta = data.meta ?? new Meta();
        // migrate file cũ
        if (empty && json.Contains("\"scenes\""))
        {
            var old = JsonUtility.FromJson<Legacy>(json) ?? new Legacy();
            foreach (var r in old.scenes) committedTrees[r.scene] = new HashSet<string>(r.ids);
            return;
        }

        foreach (var r in data.trees  ?? new List<SceneRecord>()) committedTrees[r.scene]  = new HashSet<string>(r.ids);
        foreach (var r in data.stumps ?? new List<SceneRecord>()) committedStumps[r.scene] = new HashSet<string>(r.ids);
        meta = data.meta ?? new Meta();
    }

    public static void SaveToDisk()
    {
        var data = new SaveData();
        foreach (var kv in committedTrees) data.trees.Add(new SceneRecord { scene = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in committedStumps) data.stumps.Add(new SceneRecord { scene = kv.Key, ids = new List<string>(kv.Value) });
        data.meta = meta;
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

    public static void DiscardPending() { pendingTrees.Clear(); pendingStumps.Clear(); }
    // =============== MENU SUPPORT ===============
    public static bool HasAnySave()
    {
        // có file save.json là coi như có save
        return File.Exists(PathFile);
    }

    public static string GetLastScene()
    {
        return meta?.lastScene ?? "House";
    }

    public static void SetLastScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        meta.lastScene = scene;
        meta.hasSave = true;
        SaveToDisk();
    }

    public static void NewGame(string startScene)
    {
        // xoá trạng thái cũ trong bộ nhớ
        committedTrees.Clear(); committedStumps.Clear();
        pendingTrees.Clear();   pendingStumps.Clear();
        // meta mới
        meta = new Meta
        {
            lastScene = string.IsNullOrEmpty(startScene) ? "House" : startScene,
            hasSave = true,
            day = 1,
            hour = 6,
            minute = 0,
            hp01 = 1f,
            sta01 = 1f
        };
        SaveToDisk();
    }

}
