

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
/// <summary>
/// Lưu trữ trạng thái đã lưu của các đối tượng trong game (cây, gốc cây đã chặt, cây trồng).
/// </summary>
public static class SaveStore
{
    [System.Serializable] public struct ItemStackDTO { public string key; public string id; public int count; }
    [System.Serializable] public class InventoryDTO {
        public ItemStackDTO[] hotbar;
        public ItemStackDTO[] bag;
        public ItemStackDTO[] equipment;
        public int selected;
        public int unlockedBagSlots;
    }
    static Meta meta = new Meta();
    [System.Serializable]
    public struct PlantState
    {
        public string id;
        public string seedId;
        public float x;
        public float y;
        public int stage;
        public int daysInStage;
        public int targetDaysForStage;
        public int lastUpdatedDay;
        public bool wateredToday;
        public int lastWateredDay;
        public bool isStump;
    }
    [System.Serializable]
    public struct SoilTileState
    {
        public int x;
        public int y;
        public int tilledDay;
        public bool hasPlant;
    }
    [System.Serializable]
    public struct WateredSoilState
    {
        public int x;
        public int y;
        public int day;
    }
    [System.Serializable]
    public struct GrassInstanceState
    {
        public string id;
        public float x;
        public float y;
    }
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
            equipment = new ItemStackDTO[inv.equipment.Length],
            selected = inv.selected,
            unlockedBagSlots = inv.UnlockedBagSlots
        };
        for (int i = 0; i < inv.hotbar.Length; i++)
        {
            var s = inv.hotbar[i];
            var key = db.GetKey(s.item);
            var id = s.item ? s.item.id : null;
            if (string.IsNullOrEmpty(key)) key = id;
            dto.hotbar[i] = new ItemStackDTO { key = key, id = id, count = s.count };
        }
        for (int i = 0; i < inv.bag.Length; i++)
        {
            var s = inv.bag[i];
            var key = db.GetKey(s.item);
            var id = s.item ? s.item.id : null;
            if (string.IsNullOrEmpty(key)) key = id;
            dto.bag[i] = new ItemStackDTO { key = key, id = id, count = s.count };
        }
        for (int i = 0; i < inv.equipment.Length; i++)
        {
            var s = inv.equipment[i];
            var key = db.GetKey(s.item);
            var id = s.item ? s.item.id : null;
            if (string.IsNullOrEmpty(key)) key = id;
            dto.equipment[i] = new ItemStackDTO { key = key, id = id, count = s.count };
        }
        meta.inventory = dto;
        SaveToDisk();
    }
    public static bool JustStartedNewGame { get; set; }
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
            if (!it && !string.IsNullOrEmpty(d.id))
                it = db.Find(d.id);
            inv.SetHotbar(i, it, Mathf.Max(0, d.count)); // UI sẽ refresh và fire events
        }
        for (int i = n; i < inv.hotbar.Length; i++) inv.SetHotbar(i, null, 0);

        // Bag
        int m = Mathf.Min(inv.bag.Length, meta.inventory.bag?.Length ?? 0);
        for (int i = 0; i < m; i++)
        {
            var d = meta.inventory.bag[i];
            var it = db.Find(d.key);
            if (!it && !string.IsNullOrEmpty(d.id))
                it = db.Find(d.id);
            inv.SetBag(i, it, Mathf.Max(0, d.count));
        }
        for (int i = m; i < inv.bag.Length; i++) inv.SetBag(i, null, 0);

        // Equipment
        if (meta.inventory.equipment != null)
        {
            int e = Mathf.Min(inv.equipment.Length, meta.inventory.equipment.Length);
            for (int i = 0; i < e; i++)
            {
                var d = meta.inventory.equipment[i];
                var it = db.Find(d.key);
                if (!it && !string.IsNullOrEmpty(d.id))
                    it = db.Find(d.id);
                inv.SetEquipment(i, it, Mathf.Max(0, d.count));
            }
            for (int i = e; i < inv.equipment.Length; i++) inv.SetEquipment(i, null, 0);
        }

        if (meta.inventory.unlockedBagSlots > 0)
            inv.SetUnlockedBagSlots(meta.inventory.unlockedBagSlots);

        // Selected
        int sel = Mathf.Clamp(meta.inventory.selected, 0, inv.hotbar.Length - 1);
        inv.SelectSlot(sel);
    }
    public static void SetTime(int d,int h,int m){ meta.day=d; meta.hour=h; meta.minute=m; SaveToDisk(); }
    public static void GetTime(out int d,out int h,out int m){ d=meta.day; h=meta.hour; m=meta.minute; }
    public static int PeekSavedDay(){ return meta?.day ?? 1; }

    public static void SetVitals01(float hp,float sta){
    meta.hp01=Mathf.Clamp01(hp); meta.sta01=Mathf.Clamp01(sta); SaveToDisk();
    }
    public static void GetVitals01(out float hp,out float sta){ hp=meta.hp01; sta=meta.sta01; }

// NewGame: set mặc định đầy
// meta = new Meta { lastScene = ..., hasSave = true, day=1, hour=6, minute=0, hp01=1f, sta01=1f };

    [Serializable] class SceneRecord { public string scene; public List<string> ids = new(); }
    [Serializable] class PlantSceneRecord { public string scene; public List<PlantState> plants = new(); }
    [Serializable] class SoilSceneRecord { public string scene; public List<SoilTileState> tiles = new(); }
    [Serializable] class WateredSoilSceneRecord { public string scene; public List<WateredSoilState> tiles = new(); }
    [Serializable] class GrassInstanceSceneRecord { public string scene; public List<GrassInstanceState> grasses = new(); }
    [Serializable]
    class SaveData
    {
        public List<SceneRecord> trees = new();
        public List<SceneRecord> stumps = new();
        public List<SceneRecord> grasses = new();
        public List<GrassInstanceSceneRecord> grassInstances = new();
        public List<PlantSceneRecord> plants = new();
        public List<SoilSceneRecord> soils = new();
        public List<WateredSoilSceneRecord> wateredSoils = new();
        public Meta meta = new Meta();
    }
    // Hỗ trợ file cũ (chỉ có "scenes")
    [Serializable] class Legacy { public List<SceneRecord> scenes = new(); }

    static readonly Dictionary<string, HashSet<string>> committedTrees  = new();
    static readonly Dictionary<string, HashSet<string>> committedStumps = new();
    static readonly Dictionary<string, HashSet<string>> committedGrasses = new();
    static readonly Dictionary<string, HashSet<string>> pendingTrees    = new();
    static readonly Dictionary<string, HashSet<string>> pendingStumps   = new();
    static readonly Dictionary<string, HashSet<string>> pendingGrasses  = new();
    static readonly Dictionary<string, Dictionary<string, GrassInstanceState>> committedGrassInstances = new();
    static readonly Dictionary<string, Dictionary<string, GrassInstanceState>> pendingGrassInstances = new();
    static readonly Dictionary<string, HashSet<string>> pendingRemovedGrassInstances = new();

    static readonly Dictionary<string, Dictionary<string, PlantState>> committedPlants = new();
    static readonly Dictionary<string, Dictionary<string, PlantState>> pendingPlants = new();
    static readonly Dictionary<string, HashSet<string>> pendingRemovedPlants = new();

    static readonly Dictionary<string, HashSet<Vector2Int>> committedSoil = new();
    static readonly Dictionary<string, HashSet<Vector2Int>> pendingSoil = new();
    static readonly Dictionary<string, HashSet<Vector2Int>> pendingClearedSoil = new();
    static readonly Dictionary<string, Dictionary<Vector2Int, SoilTileState>> committedSoilInfo = new();
    static readonly Dictionary<string, Dictionary<Vector2Int, SoilTileState>> pendingSoilInfo = new();
    static readonly Dictionary<string, Dictionary<Vector2Int, int>> committedWateredSoil = new();
    static readonly Dictionary<string, Dictionary<Vector2Int, int>> pendingWateredSoil = new();
    static readonly Dictionary<string, HashSet<Vector2Int>> pendingDriedSoil = new();

    static string PathFile => Path.Combine(Application.persistentDataPath, "save.json");

    public static void LoadFromDisk()
    {
        var pendingTreeSnapshot = CloneSceneSets(pendingTrees);
        var pendingStumpSnapshot = CloneSceneSets(pendingStumps);
        var pendingPlantSnapshot = ClonePlantScenes(pendingPlants);
        var pendingRemovedSnapshot = CloneSceneSets(pendingRemovedPlants);
        var pendingSoilSnapshot = CloneSoilSets(pendingSoil);
        var pendingClearedSoilSnapshot = CloneSoilSets(pendingClearedSoil);
        var pendingWateredSnapshot = CloneSoilDayMaps(pendingWateredSoil);
        var pendingDriedSnapshot = CloneSoilSets(pendingDriedSoil);
        var pendingSoilInfoSnapshot = CloneSoilInfoMaps(pendingSoilInfo);
        var pendingGrassSnapshot = CloneSceneSets(pendingGrasses);
        var pendingGrassInstanceSnapshot = CloneGrassInstanceScenes(pendingGrassInstances);
        var pendingRemovedGrassInstanceSnapshot = CloneSceneSets(pendingRemovedGrassInstances);

        committedTrees.Clear(); committedStumps.Clear(); committedGrasses.Clear();
        committedGrassInstances.Clear();
        pendingTrees.Clear();   pendingStumps.Clear();   pendingGrasses.Clear();
        pendingGrassInstances.Clear(); pendingRemovedGrassInstances.Clear();
        committedPlants.Clear(); pendingPlants.Clear(); pendingRemovedPlants.Clear();
        committedSoil.Clear(); pendingSoil.Clear(); pendingClearedSoil.Clear();
        committedSoilInfo.Clear(); pendingSoilInfo.Clear();
        committedWateredSoil.Clear(); pendingWateredSoil.Clear(); pendingDriedSoil.Clear();

        if (!File.Exists(PathFile))
        {
            meta = new Meta();
            return;
        }
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
        foreach (var r in data.grasses ?? new List<SceneRecord>()) committedGrasses[r.scene] = new HashSet<string>(r.ids);
        foreach (var r in data.grassInstances ?? new List<GrassInstanceSceneRecord>())
        {
            if (string.IsNullOrEmpty(r.scene)) continue;
            if (r.grasses == null || r.grasses.Count == 0) continue;
            var dict = new Dictionary<string, GrassInstanceState>();
            foreach (var g in r.grasses)
            {
                if (string.IsNullOrEmpty(g.id)) continue;
                dict[g.id] = g;
            }
            if (dict.Count > 0) committedGrassInstances[r.scene] = dict;
        }
        foreach (var r in data.plants ?? new List<PlantSceneRecord>())
        {
            if (string.IsNullOrEmpty(r.scene)) continue;
            if (r.plants == null || r.plants.Count == 0) continue;
            var dict = new Dictionary<string, PlantState>();
            foreach (var p in r.plants)
            {
                if (string.IsNullOrEmpty(p.id)) continue;
                dict[p.id] = p;
            }
            if (dict.Count > 0) committedPlants[r.scene] = dict;
        }
        foreach (var r in data.soils ?? new List<SoilSceneRecord>())
        {
            if (string.IsNullOrEmpty(r.scene)) continue;
            if (r.tiles == null || r.tiles.Count == 0) continue;
            var set = new HashSet<Vector2Int>();
            var info = new Dictionary<Vector2Int, SoilTileState>();
            foreach (var tile in r.tiles)
            {
                var cell = new Vector2Int(tile.x, tile.y);
                set.Add(cell);
                info[cell] = new SoilTileState
                {
                    x = tile.x,
                    y = tile.y,
                    tilledDay = tile.tilledDay,
                    hasPlant = tile.hasPlant
                };
            }
            if (set.Count > 0) committedSoil[r.scene] = set;
            if (info.Count > 0) committedSoilInfo[r.scene] = info;
        }
        foreach (var r in data.wateredSoils ?? new List<WateredSoilSceneRecord>())
        {
            if (string.IsNullOrEmpty(r.scene)) continue;
            if (r.tiles == null || r.tiles.Count == 0) continue;
            var dict = new Dictionary<Vector2Int, int>();
            foreach (var tile in r.tiles)
            {
                dict[new Vector2Int(tile.x, tile.y)] = tile.day;
            }
            if (dict.Count > 0) committedWateredSoil[r.scene] = dict;
        }
        meta = data.meta ?? new Meta();

        RestoreSceneSets(pendingTrees, pendingTreeSnapshot);
        RestoreSceneSets(pendingStumps, pendingStumpSnapshot);
        RestoreSceneSets(pendingGrasses, pendingGrassSnapshot);
        RestoreGrassInstanceScenes(pendingGrassInstances, pendingGrassInstanceSnapshot);
        RestoreSceneSets(pendingRemovedGrassInstances, pendingRemovedGrassInstanceSnapshot);
        RestorePlantScenes(pendingPlants, pendingPlantSnapshot);
        RestoreSceneSets(pendingRemovedPlants, pendingRemovedSnapshot);
        RestoreSoilSets(pendingSoil, pendingSoilSnapshot);
        RestoreSoilSets(pendingClearedSoil, pendingClearedSoilSnapshot);
        RestoreSoilDayMaps(pendingWateredSoil, pendingWateredSnapshot);
        RestoreSoilSets(pendingDriedSoil, pendingDriedSnapshot);
        RestoreSoilInfoMaps(pendingSoilInfo, pendingSoilInfoSnapshot);
    }

    public static void SaveToDisk()
    {
        var data = new SaveData();
        foreach (var kv in committedTrees) data.trees.Add(new SceneRecord { scene = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in committedStumps) data.stumps.Add(new SceneRecord { scene = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in committedGrasses) data.grasses.Add(new SceneRecord { scene = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in committedGrassInstances)
        {
            var rec = new GrassInstanceSceneRecord { scene = kv.Key };
            foreach (var g in kv.Value.Values) rec.grasses.Add(g);
            data.grassInstances.Add(rec);
        }
        foreach (var kv in committedPlants)
        {
            var rec = new PlantSceneRecord { scene = kv.Key };
            foreach (var plant in kv.Value.Values)
            {
                rec.plants.Add(plant);
            }
            data.plants.Add(rec);
        }
        foreach (var kv in committedSoil)
        {
            var rec = new SoilSceneRecord { scene = kv.Key };
            committedSoilInfo.TryGetValue(kv.Key, out var info);
            foreach (var cell in kv.Value)
            {
                SoilTileState state;
                if (info != null && info.TryGetValue(cell, out var stored))
                {
                    state = stored;
                }
                else
                {
                    state = new SoilTileState { x = cell.x, y = cell.y, tilledDay = 0, hasPlant = false };
                }
                state.x = cell.x;
                state.y = cell.y;
                rec.tiles.Add(state);
            }
            data.soils.Add(rec);
        }
        foreach (var kv in committedWateredSoil)
        {
            var rec = new WateredSoilSceneRecord { scene = kv.Key };
            foreach (var entry in kv.Value)
            {
                rec.tiles.Add(new WateredSoilState { x = entry.Key.x, y = entry.Key.y, day = entry.Value });
            }
            data.wateredSoils.Add(rec);
        }
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
        if (!string.IsNullOrEmpty(scene) && !string.IsNullOrEmpty(id))
        {
            if (pendingPlants.TryGetValue(scene, out var dict)) dict.Remove(id);
            if (!pendingRemovedPlants.TryGetValue(scene, out var removed)) pendingRemovedPlants[scene] = removed = new HashSet<string>();
            removed.Add(id);
        }
    }

    public static void MarkGrassReapedPending(string scene, string id)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(id)) return;
        if (!pendingGrasses.TryGetValue(scene, out var set)) pendingGrasses[scene] = set = new HashSet<string>();
        set.Add(id);
        if (pendingGrassInstances.TryGetValue(scene, out var dict)) dict.Remove(id);
        if (!pendingRemovedGrassInstances.TryGetValue(scene, out var removed)) pendingRemovedGrassInstances[scene] = removed = new HashSet<string>();
        removed.Add(id);
    }

    public static void SetGrassInstancePending(string scene, GrassInstanceState state)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(state.id)) return;
        if (!pendingGrassInstances.TryGetValue(scene, out var dict)) pendingGrassInstances[scene] = dict = new Dictionary<string, GrassInstanceState>();
        dict[state.id] = state;
        if (pendingRemovedGrassInstances.TryGetValue(scene, out var removed)) removed.Remove(state.id);
    }

    public static void RemoveGrassInstancePending(string scene, string id)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(id)) return;
        if (!pendingRemovedGrassInstances.TryGetValue(scene, out var removed)) pendingRemovedGrassInstances[scene] = removed = new HashSet<string>();
        removed.Add(id);
        if (pendingGrassInstances.TryGetValue(scene, out var dict)) dict.Remove(id);
    }
    public static void MarkSoilTilledPending(string scene, Vector2Int cell, int tilledDay, bool hasPlant){
        if (string.IsNullOrEmpty(scene)) return;
        if (!pendingSoil.TryGetValue(scene, out var set)) pendingSoil[scene] = set = new HashSet<Vector2Int>();
        set.Add(cell);
        if (pendingClearedSoil.TryGetValue(scene, out var cleared)) cleared.Remove(cell);
        if (!pendingSoilInfo.TryGetValue(scene, out var dict)) pendingSoilInfo[scene] = dict = new Dictionary<Vector2Int, SoilTileState>();
        dict[cell] = new SoilTileState { x = cell.x, y = cell.y, tilledDay = tilledDay, hasPlant = hasPlant };
    }

    public static void MarkSoilClearedPending(string scene, Vector2Int cell){
        if (string.IsNullOrEmpty(scene)) return;
        if (!pendingClearedSoil.TryGetValue(scene, out var set)) pendingClearedSoil[scene] = set = new HashSet<Vector2Int>();
        set.Add(cell);
        if (pendingSoil.TryGetValue(scene, out var tilled))
        {
            tilled.Remove(cell);
            if (tilled.Count == 0) pendingSoil.Remove(scene);
        }
        if (pendingSoilInfo.TryGetValue(scene, out var info))
        {
            info.Remove(cell);
            if (info.Count == 0) pendingSoilInfo.Remove(scene);
        }
        MarkSoilDriedPending(scene, cell);
    }

    public static void UpdateSoilTilePending(string scene, Vector2Int cell, int tilledDay, bool hasPlant)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (!pendingSoilInfo.TryGetValue(scene, out var dict)) pendingSoilInfo[scene] = dict = new Dictionary<Vector2Int, SoilTileState>();

        SoilTileState state;
        if (dict.TryGetValue(cell, out var existing))
        {
            state = existing;
        }
        else if (pendingSoil.TryGetValue(scene, out var pending) && pending.Contains(cell))
        {
            state = new SoilTileState { x = cell.x, y = cell.y };
        }
        else if (committedSoilInfo.TryGetValue(scene, out var committed) && committed.TryGetValue(cell, out var committedState))
        {
            state = committedState;
        }
        else
        {
            state = new SoilTileState { x = cell.x, y = cell.y };
        }

        state.x = cell.x;
        state.y = cell.y;
        state.tilledDay = tilledDay;
        state.hasPlant = hasPlant;
        dict[cell] = state;
    }

    public static void MarkSoilWateredPending(string scene, Vector2Int cell, int day)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (!pendingWateredSoil.TryGetValue(scene, out var dict)) pendingWateredSoil[scene] = dict = new Dictionary<Vector2Int, int>();
        dict[cell] = day;
        if (pendingDriedSoil.TryGetValue(scene, out var dried)) dried.Remove(cell);
    }

    public static void MarkSoilDriedPending(string scene, Vector2Int cell)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (!pendingDriedSoil.TryGetValue(scene, out var set)) pendingDriedSoil[scene] = set = new HashSet<Vector2Int>();
        set.Add(cell);
        if (pendingWateredSoil.TryGetValue(scene, out var dict))
        {
            dict.Remove(cell);
            if (dict.Count == 0) pendingWateredSoil.Remove(scene);
        }
    }

    // kiểm tra trong phiên (committed ∪ pending)
    public static bool IsTreeChoppedInSession(string scene, string id) =>
        (committedTrees.TryGetValue(scene, out var c) && c.Contains(id)) ||
        (pendingTrees.TryGetValue(scene, out var p) && p.Contains(id));

    public static bool IsStumpClearedInSession(string scene, string id) =>
        (committedStumps.TryGetValue(scene, out var c) && c.Contains(id)) ||
        (pendingStumps.TryGetValue(scene, out var p) && p.Contains(id));

    public static bool IsGrassReapedInSession(string scene, string id) =>
        (committedGrasses.TryGetValue(scene, out var c) && c.Contains(id)) ||
        (pendingGrasses.TryGetValue(scene, out var p) && p.Contains(id));

    public static IEnumerable<GrassInstanceState> GetGrassInstancesInScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) yield break;

        pendingRemovedGrassInstances.TryGetValue(scene, out var removed);
        var emitted = new HashSet<string>();

        if (pendingGrassInstances.TryGetValue(scene, out var pending))
        {
            foreach (var kv in pending)
            {
                if (removed != null && removed.Contains(kv.Key)) continue;
                if (IsGrassReapedInSession(scene, kv.Key)) continue;
                emitted.Add(kv.Key);
                yield return kv.Value;
            }
        }

        if (committedGrassInstances.TryGetValue(scene, out var committed))
        {
            foreach (var kv in committed)
            {
                if (emitted.Contains(kv.Key)) continue;
                if (removed != null && removed.Contains(kv.Key)) continue;
                if (IsGrassReapedInSession(scene, kv.Key)) continue;
                yield return kv.Value;
            }
        }
    }

    public static bool IsSoilTilledInSession(string scene, Vector2Int cell)
    {
        if (string.IsNullOrEmpty(scene)) return false;
        if (pendingClearedSoil.TryGetValue(scene, out var cleared) && cleared.Contains(cell)) return false;
        return (pendingSoil.TryGetValue(scene, out var p) && p.Contains(cell)) ||
               (committedSoil.TryGetValue(scene, out var c) && c.Contains(cell));
    }

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
        foreach (var kv in pendingGrasses)
        {
            if (!committedGrasses.TryGetValue(kv.Key, out var set)) committedGrasses[kv.Key] = set = new HashSet<string>();
            set.UnionWith(kv.Value);
        }
        foreach (var kv in pendingGrassInstances)
        {
            if (!committedGrassInstances.TryGetValue(kv.Key, out var dict)) committedGrassInstances[kv.Key] = dict = new Dictionary<string, GrassInstanceState>();
            foreach (var entry in kv.Value) dict[entry.Key] = entry.Value;
        }
        foreach (var kv in pendingRemovedGrassInstances)
        {
            if (committedGrassInstances.TryGetValue(kv.Key, out var dict))
            {
                foreach (var id in kv.Value) dict.Remove(id);
                if (dict.Count == 0) committedGrassInstances.Remove(kv.Key);
            }
        }
        foreach (var kv in pendingPlants)
        {
            if (!committedPlants.TryGetValue(kv.Key, out var dict)) committedPlants[kv.Key] = dict = new Dictionary<string, PlantState>();
            foreach (var plant in kv.Value)
            {
                dict[plant.Key] = plant.Value;
            }
        }
        foreach (var kv in pendingRemovedPlants)
        {
            if (!committedPlants.TryGetValue(kv.Key, out var dict)) continue;
            foreach (var id in kv.Value) dict.Remove(id);
        }
        foreach (var kv in pendingClearedSoil)
        {
            if (pendingSoil.TryGetValue(kv.Key, out var pending))
            {
                pending.ExceptWith(kv.Value);
                if (pending.Count == 0) pendingSoil.Remove(kv.Key);
            }

            if (committedSoil.TryGetValue(kv.Key, out var committed))
            {
                committed.ExceptWith(kv.Value);
                if (committed.Count == 0) committedSoil.Remove(kv.Key);
            }
            if (committedSoilInfo.TryGetValue(kv.Key, out var committedInfo))
            {
                foreach (var cell in kv.Value) committedInfo.Remove(cell);
                if (committedInfo.Count == 0) committedSoilInfo.Remove(kv.Key);
            }
        }

        foreach (var kv in pendingDriedSoil)
        {
            if (pendingWateredSoil.TryGetValue(kv.Key, out var pending))
            {
                foreach (var cell in kv.Value) pending.Remove(cell);
                if (pending.Count == 0) pendingWateredSoil.Remove(kv.Key);
            }

            if (committedWateredSoil.TryGetValue(kv.Key, out var committed))
            {
                foreach (var cell in kv.Value) committed.Remove(cell);
                if (committed.Count == 0) committedWateredSoil.Remove(kv.Key);
            }
        }

        foreach (var kv in pendingSoil)
        {
            if (!committedSoil.TryGetValue(kv.Key, out var set)) committedSoil[kv.Key] = set = new HashSet<Vector2Int>();
            set.UnionWith(kv.Value);
        }
        foreach (var kv in pendingSoilInfo)
        {
            if (!committedSoilInfo.TryGetValue(kv.Key, out var dict)) committedSoilInfo[kv.Key] = dict = new Dictionary<Vector2Int, SoilTileState>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
        }
        foreach (var kv in pendingWateredSoil)
        {
            if (!committedWateredSoil.TryGetValue(kv.Key, out var dict)) committedWateredSoil[kv.Key] = dict = new Dictionary<Vector2Int, int>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
        }
        pendingTrees.Clear(); pendingStumps.Clear(); pendingGrasses.Clear();
        pendingGrassInstances.Clear(); pendingRemovedGrassInstances.Clear();
        pendingPlants.Clear(); pendingRemovedPlants.Clear();
        pendingSoil.Clear(); pendingClearedSoil.Clear();
        pendingSoilInfo.Clear();
        pendingWateredSoil.Clear(); pendingDriedSoil.Clear();
        meta.hasSave = true;
        SaveToDisk();
    }

    public static void DiscardPending()
    {
        pendingTrees.Clear(); pendingStumps.Clear(); pendingGrasses.Clear();
        pendingGrassInstances.Clear(); pendingRemovedGrassInstances.Clear();
        pendingPlants.Clear(); pendingRemovedPlants.Clear();
        pendingSoil.Clear(); pendingClearedSoil.Clear();
        pendingSoilInfo.Clear();
        pendingWateredSoil.Clear(); pendingDriedSoil.Clear();
    }
    // =============== MENU SUPPORT ===============
    public static bool HasAnySave()
    {
        if (meta != null && meta.hasSave) return true;
        if (!File.Exists(PathFile)) return false;

        LoadFromDisk();
        return meta != null && meta.hasSave;
    }

    public static string GetLastScene()
    {
        return meta?.lastScene ?? "House";
    }

    public static void SetLastScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        meta.lastScene = scene;
        SaveToDisk();
    }

    public static void NewGame(string startScene)
    {
        // xoá trạng thái cũ trong bộ nhớ
        committedTrees.Clear(); committedStumps.Clear(); committedGrasses.Clear();
        committedGrassInstances.Clear();
        pendingTrees.Clear();   pendingStumps.Clear();   pendingGrasses.Clear();
        pendingGrassInstances.Clear(); pendingRemovedGrassInstances.Clear();
        committedPlants.Clear(); pendingPlants.Clear(); pendingRemovedPlants.Clear();
        committedSoil.Clear(); pendingSoil.Clear(); pendingClearedSoil.Clear();
        committedSoilInfo.Clear(); pendingSoilInfo.Clear();
        committedWateredSoil.Clear(); pendingWateredSoil.Clear(); pendingDriedSoil.Clear();
        JustStartedNewGame = true;
        // meta mới
        meta = new Meta
        {
            lastScene = string.IsNullOrEmpty(startScene) ? "House" : startScene,
            hasSave = false,
            day = 1,
            hour = 6,
            minute = 0,
            hp01 = 1f,
            sta01 = 1f
        };
        SaveToDisk();
    }

    public static string CreatePlantId() => System.Guid.NewGuid().ToString();

    static Dictionary<string, PlantState> GetOrCreatePendingPlantScene(string scene)
    {
        if (!pendingPlants.TryGetValue(scene, out var dict)) pendingPlants[scene] = dict = new Dictionary<string, PlantState>();
        return dict;
    }

    public static void SetPlantStatePending(string scene, PlantState state)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(state.id)) return;
        var dict = GetOrCreatePendingPlantScene(scene);
        dict[state.id] = state;
        if (pendingRemovedPlants.TryGetValue(scene, out var removed)) removed.Remove(state.id);
    }

    public static void RemovePlantPending(string scene, string plantId)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(plantId)) return;
        if (!pendingRemovedPlants.TryGetValue(scene, out var set)) pendingRemovedPlants[scene] = set = new HashSet<string>();
        set.Add(plantId);
        if (pendingPlants.TryGetValue(scene, out var dict)) dict.Remove(plantId);
    }

    public static IEnumerable<SoilTileState> GetTilledSoilInScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) yield break;

        var emitted = new HashSet<Vector2Int>();

        pendingClearedSoil.TryGetValue(scene, out var cleared);

        if (committedSoil.TryGetValue(scene, out var committed))
        {
            committedSoilInfo.TryGetValue(scene, out var info);
            foreach (var cell in committed)
            {
                if (cleared != null && cleared.Contains(cell)) continue;
                SoilTileState state;
                if (info != null && info.TryGetValue(cell, out var stored))
                {
                    state = stored;
                }
                else
                {
                    state = new SoilTileState { x = cell.x, y = cell.y, tilledDay = 0, hasPlant = false };
                }
                state.x = cell.x;
                state.y = cell.y;
                yield return state;
                emitted.Add(cell);
            }
        }

        if (pendingSoil.TryGetValue(scene, out var pending))
        {
            pendingSoilInfo.TryGetValue(scene, out var info);
            foreach (var cell in pending)
            {
                if (cleared != null && cleared.Contains(cell)) continue;
                if (!emitted.Add(cell)) continue;
                SoilTileState state;
                if (info != null && info.TryGetValue(cell, out var stored))
                {
                    state = stored;
                }
                else if (committedSoilInfo.TryGetValue(scene, out var committedInfo) && committedInfo.TryGetValue(cell, out var committedState))
                {
                    state = committedState;
                }
                else
                {
                    state = new SoilTileState { x = cell.x, y = cell.y, tilledDay = 0, hasPlant = false };
                }
                state.x = cell.x;
                state.y = cell.y;
                yield return state;
            }
        }

        if (pendingSoilInfo.TryGetValue(scene, out var pendingInfo))
        {
            foreach (var kv in pendingInfo)
            {
                var cell = kv.Key;
                if (emitted.Contains(cell)) continue;
                if (cleared != null && cleared.Contains(cell)) continue;
                if (committedSoil.TryGetValue(scene, out var committedSet2))
                {
                    if (committedSet2.Contains(cell))
                    {
                        var state = kv.Value;
                        state.x = cell.x; state.y = cell.y;
                        yield return state;
                        emitted.Add(cell);
                    }
                }

            }
        }
    }
    public static IEnumerable<WateredSoilState> GetWateredSoilInScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) yield break;

        pendingDriedSoil.TryGetValue(scene, out var dried);
        var emitted = new HashSet<Vector2Int>();

        if (committedWateredSoil.TryGetValue(scene, out var committed))
        {
            foreach (var kv in committed)
            {
                if (dried != null && dried.Contains(kv.Key)) continue;
                yield return new WateredSoilState { x = kv.Key.x, y = kv.Key.y, day = kv.Value };
                emitted.Add(kv.Key);
            }
        }

        if (pendingWateredSoil.TryGetValue(scene, out var pending))
        {
            foreach (var kv in pending)
            {
                if (dried != null && dried.Contains(kv.Key)) continue;
                if (emitted.Contains(kv.Key)) continue;
                yield return new WateredSoilState { x = kv.Key.x, y = kv.Key.y, day = kv.Value };
            }
        }
    }

    public static IEnumerable<PlantState> GetPlantsInScene(string scene)
    {
        if (string.IsNullOrEmpty(scene)) yield break;

        pendingRemovedPlants.TryGetValue(scene, out var removed);
        var emitted = new HashSet<string>();

        if (committedPlants.TryGetValue(scene, out var committed))
        {
            foreach (var kv in committed)
            {
                if (removed != null && removed.Contains(kv.Key)) continue;
                if (pendingPlants.TryGetValue(scene, out var pending) && pending.TryGetValue(kv.Key, out var pendingState))
                {
                    yield return pendingState;
                    emitted.Add(kv.Key);
                }
                else
                {
                    yield return kv.Value;
                    emitted.Add(kv.Key);
                }
            }
        }

        if (pendingPlants.TryGetValue(scene, out var pendingOnly))
        {
            foreach (var kv in pendingOnly)
            {
                if (emitted.Contains(kv.Key)) continue;
                if (removed != null && removed.Contains(kv.Key)) continue;
                yield return kv.Value;
            }
        }
    }
    public static bool TryGetPlantState(string scene, string id, out PlantState state)
    {
        state = default;
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(id)) return false;
        if (pendingPlants.TryGetValue(scene, out var pending) && pending.TryGetValue(id, out state)) return true;
        if (pendingRemovedPlants.TryGetValue(scene, out var removed) && removed.Contains(id)) return false;
        if (committedPlants.TryGetValue(scene, out var committed) && committed.TryGetValue(id, out state)) return true;
        return false;
    }

    static Dictionary<string, HashSet<string>> CloneSceneSets(Dictionary<string, HashSet<string>> source)
    {
        var clone = new Dictionary<string, HashSet<string>>();
        foreach (var kv in source)
        {
            clone[kv.Key] = new HashSet<string>(kv.Value);
        }
        return clone;
    }

    static Dictionary<string, Dictionary<string, PlantState>> ClonePlantScenes(Dictionary<string, Dictionary<string, PlantState>> source)
    {
        var clone = new Dictionary<string, Dictionary<string, PlantState>>();
        foreach (var kv in source)
        {
            var plants = new Dictionary<string, PlantState>();
            foreach (var plant in kv.Value)
            {
                plants[plant.Key] = plant.Value;
            }
            clone[kv.Key] = plants;
        }
        return clone;
    }

    static Dictionary<string, HashSet<Vector2Int>> CloneSoilSets(Dictionary<string, HashSet<Vector2Int>> source)
    {
        var clone = new Dictionary<string, HashSet<Vector2Int>>();
        foreach (var kv in source)
        {
            clone[kv.Key] = new HashSet<Vector2Int>(kv.Value);
        }
        return clone;
    }

    static Dictionary<string, Dictionary<string, GrassInstanceState>> CloneGrassInstanceScenes(Dictionary<string, Dictionary<string, GrassInstanceState>> source)
    {
        var clone = new Dictionary<string, Dictionary<string, GrassInstanceState>>();
        foreach (var kv in source)
        {
            clone[kv.Key] = new Dictionary<string, GrassInstanceState>(kv.Value);
        }
        return clone;
    }

    static Dictionary<string, Dictionary<Vector2Int, int>> CloneSoilDayMaps(Dictionary<string, Dictionary<Vector2Int, int>> source)
    {
        var clone = new Dictionary<string, Dictionary<Vector2Int, int>>();
        foreach (var kv in source)
        {
            var dict = new Dictionary<Vector2Int, int>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
            clone[kv.Key] = dict;
        }
        return clone;
    }

    static Dictionary<string, Dictionary<Vector2Int, SoilTileState>> CloneSoilInfoMaps(Dictionary<string, Dictionary<Vector2Int, SoilTileState>> source)
    {
        var clone = new Dictionary<string, Dictionary<Vector2Int, SoilTileState>>();
        foreach (var kv in source)
        {
            var dict = new Dictionary<Vector2Int, SoilTileState>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
            clone[kv.Key] = dict;
        }
        return clone;
    }

    static void RestoreSceneSets(Dictionary<string, HashSet<string>> target, Dictionary<string, HashSet<string>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (target.TryGetValue(kv.Key, out var set)) set.UnionWith(kv.Value);
            else target[kv.Key] = new HashSet<string>(kv.Value);
        }
    }

    static void RestorePlantScenes(Dictionary<string, Dictionary<string, PlantState>> target,
                                   Dictionary<string, Dictionary<string, PlantState>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (!target.TryGetValue(kv.Key, out var dict)) target[kv.Key] = dict = new Dictionary<string, PlantState>();
            foreach (var plant in kv.Value)
            {
                dict[plant.Key] = plant.Value;
            }
        }
    }

    static void RestoreSoilSets(Dictionary<string, HashSet<Vector2Int>> target, Dictionary<string, HashSet<Vector2Int>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (target.TryGetValue(kv.Key, out var set)) set.UnionWith(kv.Value);
            else target[kv.Key] = new HashSet<Vector2Int>(kv.Value);
        }
    }

    static void RestoreGrassInstanceScenes(Dictionary<string, Dictionary<string, GrassInstanceState>> target,
                                           Dictionary<string, Dictionary<string, GrassInstanceState>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (!target.TryGetValue(kv.Key, out var dict)) target[kv.Key] = dict = new Dictionary<string, GrassInstanceState>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
        }
    }

    static void RestoreSoilDayMaps(Dictionary<string, Dictionary<Vector2Int, int>> target,
                                   Dictionary<string, Dictionary<Vector2Int, int>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (!target.TryGetValue(kv.Key, out var dict)) target[kv.Key] = dict = new Dictionary<Vector2Int, int>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
        }
    }

    static void RestoreSoilInfoMaps(Dictionary<string, Dictionary<Vector2Int, SoilTileState>> target,
                                     Dictionary<string, Dictionary<Vector2Int, SoilTileState>> snapshot)
    {
        foreach (var kv in snapshot)
        {
            if (!target.TryGetValue(kv.Key, out var dict)) target[kv.Key] = dict = new Dictionary<Vector2Int, SoilTileState>();
            foreach (var entry in kv.Value)
            {
                dict[entry.Key] = entry.Value;
            }
        }
    }
}
