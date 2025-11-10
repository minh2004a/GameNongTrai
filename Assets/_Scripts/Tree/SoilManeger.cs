
// SoilManager.cs
using System.Collections.Generic;
using UnityEngine;

// Quản lý trạng thái ô đất đã được xới, hỗ trợ lưu/khôi phục
public class SoilManager : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float gridSize = 1f;
    [SerializeField] GameObject tilledSoilPrefab;
    [SerializeField] Transform tilledParent;
    [SerializeField] LayerMask tillableMask;
    [SerializeField] LayerMask blockMask;
    [SerializeField] LayerMask grassBlockMask;
    [SerializeField, Range(0.05f, 1f)] float maskCheckRadiusMultiplier = 0.45f;
    [SerializeField, Range(0.05f, 1f)] float blockCheckRadiusMultiplier = 0.45f;
    [SerializeField, Range(0.05f, 1f)] float grassBlockCheckRadiusMultiplier = 0.45f;
    [SerializeField] bool grassMaskIncludesTriggers = true;
    [SerializeField, Min(1)] int daysUntilRevert = 3;
    [SerializeField, Range(0.05f, 1f)] float plantPresenceCheckRadiusMultiplier = 0.45f;

    static readonly Vector2Int[] CardinalOffsets =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    readonly HashSet<Vector2Int> tilledCells = new();
    readonly Dictionary<Vector2Int, GameObject> visuals = new();
    readonly HashSet<Vector2Int> wetCells = new();
    readonly Dictionary<Vector2Int, int> wetCellDay = new();
    readonly Dictionary<Vector2Int, int> tilledCellDay = new();
    readonly HashSet<Vector2Int> plantedCells = new();
    readonly Collider2D[] blockCheckResults = new Collider2D[8];
    readonly Collider2D[] plantCheckResults = new Collider2D[8];

    string sceneName;
    TimeManager time;

    void Awake()
    {
        gridSize = Mathf.Max(0.01f, gridSize);
        if (!tilledParent) tilledParent = transform;
        sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : null;
        time = FindFirstObjectByType<TimeManager>();
        RestoreFromSave();
    }

    void OnEnable()
    {
        AttachTimeManager(time ?? FindFirstObjectByType<TimeManager>());
    }

    void OnDisable()
    {
        if (time) time.OnNewDay -= HandleNewDay;
    }

    public float GridSize => gridSize;

    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        float s = Mathf.Max(0.01f, gridSize);
        int x = Mathf.FloorToInt(worldPos.x / s);
        int y = Mathf.FloorToInt(worldPos.y / s);
        return new Vector2Int(x, y);
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        float s = Mathf.Max(0.01f, gridSize);
        return new Vector2((cell.x + 0.5f) * s, (cell.y + 0.5f) * s);
    }

    public Vector2 SnapToGrid(Vector2 worldPos) => CellToWorld(WorldToCell(worldPos));

    public bool IsTilled(Vector2 worldPos) => IsCellTilled(WorldToCell(worldPos));

    public bool IsCellTilled(Vector2Int cell) => tilledCells.Contains(cell);

    public bool TryTillAt(Vector2 worldPos)
    {
        return TryTillCell(WorldToCell(worldPos));
    }

    public bool TryTillCell(Vector2Int cell)
    {
        if (!CanTillCell(cell)) return false;
        AddCell(cell, true, CurrentDay, false);
        return true;
    }

    public bool TryClearAt(Vector2 worldPos)
    {
        return TryClearCell(WorldToCell(worldPos));
    }

    public bool TryClearCell(Vector2Int cell)
    {
        if (!tilledCells.Contains(cell)) return false;
        RemoveCell(cell, true);
        return true;
    }

    public void EnsureCellTilledFromSave(Vector2Int cell, int? tilledDay = null, bool? hasPlant = null)
    {
        AddCell(cell, false, tilledDay, hasPlant);
    }

    public void EnsureCellWateredFromSave(Vector2Int cell, int day)
    {
        if (!tilledCells.Contains(cell)) return;
        if (day < CurrentDay) return;
        wetCells.Add(cell);
        wetCellDay[cell] = day;
        RefreshCellAndNeighbors(cell);
    }

    public bool TryWaterAt(Vector2 worldPos)
    {
        return TryWaterCell(WorldToCell(worldPos));
    }

    public bool TryWaterCell(Vector2Int cell)
    {
        if (!tilledCells.Contains(cell)) return false;
        int day = CurrentDay;
        bool changed = !wetCells.Contains(cell) || !wetCellDay.TryGetValue(cell, out var prevDay) || prevDay != day;
        wetCells.Add(cell);
        wetCellDay[cell] = day;
        if (changed && HasScene)
        {
            SaveStore.MarkSoilWateredPending(sceneName, cell, day);
        }
        RefreshCellAndNeighbors(cell);
        return changed;
    }

    public bool TryDryCell(Vector2Int cell)
    {
        return ClearWetCell(cell, true);
    }

    bool CanTillCell(Vector2Int cell)
    {
        if (tilledCells.Contains(cell)) return false;
        if (!IsCellTillable(cell)) return false;
        if (IsCellBlocked(cell)) return false;
        return true;
    }

    bool IsCellTillable(Vector2Int cell)
    {
        if (tillableMask.value == 0) return true;
        Vector2 center = CellToWorld(cell);
        float radius = Mathf.Max(0.01f, gridSize) * Mathf.Clamp(maskCheckRadiusMultiplier, 0.05f, 1f);
        return Physics2D.OverlapCircle(center, radius, tillableMask);
    }

    bool IsCellBlocked(Vector2Int cell)
    {
        Vector2 center = CellToWorld(cell);
        float baseRadius = Mathf.Max(0.01f, gridSize);

        float blockRadius = baseRadius * Mathf.Clamp(blockCheckRadiusMultiplier, 0.05f, 1f);
        if (CheckBlockingMask(center, blockRadius, blockMask, false))
        {
            return true;
        }

        float grassRadius = baseRadius * Mathf.Clamp(grassBlockCheckRadiusMultiplier, 0.05f, 1f);
        if (CheckBlockingMask(center, grassRadius, grassBlockMask, grassMaskIncludesTriggers))
        {
            return true;
        }

        return false;
    }

    bool CheckBlockingMask(Vector2 center, float radius, LayerMask mask, bool includeTriggers)
    {
        if (mask.value == 0) return false;

        var filter = new ContactFilter2D
        {
            useTriggers = includeTriggers,
            useLayerMask = true,
            layerMask = mask
        };

        Transform ignoreRoot = tilledParent ? tilledParent : transform;
        int hits = Physics2D.OverlapCircle(center, radius, filter, blockCheckResults);
        for (int i = 0; i < hits; ++i)
        {
            var col = blockCheckResults[i];
            if (!col) continue;

            if (ignoreRoot && col.transform.IsChildOf(ignoreRoot)) continue;

            System.Array.Clear(blockCheckResults, 0, blockCheckResults.Length);
            return true;
        }

        System.Array.Clear(blockCheckResults, 0, blockCheckResults.Length);
        return false;
    }

    void AddCell(Vector2Int cell, bool markPending, int? tilledDayOverride = null, bool? hasPlant = null)
    {
        bool added = tilledCells.Add(cell);
        SpawnVisual(cell);
        if (added)
        {
            wetCells.Remove(cell);
            wetCellDay.Remove(cell);
            int day = tilledDayOverride ?? CurrentDay;
            tilledCellDay[cell] = day;
        }
        else if (tilledDayOverride.HasValue)
        {
            tilledCellDay[cell] = tilledDayOverride.Value;
        }
        else if (!tilledCellDay.ContainsKey(cell))
        {
            tilledCellDay[cell] = CurrentDay;
        }

        if (hasPlant.HasValue)
        {
            if (hasPlant.Value) plantedCells.Add(cell);
            else plantedCells.Remove(cell);
        }
        else if (added)
        {
            plantedCells.Remove(cell);
        }

        if (added && markPending && !string.IsNullOrEmpty(sceneName))
        {
            int day = tilledCellDay.TryGetValue(cell, out var storedDay) ? storedDay : CurrentDay;
            bool planted = plantedCells.Contains(cell);
            SaveStore.MarkSoilTilledPending(sceneName, cell, day, planted);
        }
        RefreshCellAndNeighbors(cell);
    }

    void SpawnVisual(Vector2Int cell)
    {
        if (!tilledSoilPrefab) return;
        if (visuals.TryGetValue(cell, out var existing) && existing)
        {
            existing.transform.position = CellToWorld(cell);
            return;
        }

        var parent = tilledParent ? tilledParent : transform;
        var inst = Instantiate(tilledSoilPrefab, CellToWorld(cell), Quaternion.identity, parent);
        visuals[cell] = inst;
    }

    void RefreshCellAndNeighbors(Vector2Int cell)
    {
        RefreshVisual(cell);
        foreach (var offset in CardinalOffsets)
        {
            RefreshVisual(cell + offset);
        }
    }

    void RemoveCell(Vector2Int cell, bool markPending)
    {
        if (!tilledCells.Remove(cell)) return;

        ClearWetCell(cell, markPending);
        tilledCellDay.Remove(cell);
        plantedCells.Remove(cell);

        if (visuals.TryGetValue(cell, out var go) && go)
        {
            Destroy(go);
        }
        visuals.Remove(cell);

        if (markPending && !string.IsNullOrEmpty(sceneName))
        {
            SaveStore.MarkSoilClearedPending(sceneName, cell);
        }

        RefreshCellAndNeighbors(cell);
    }

    public void SetCellHasPlant(Vector2Int cell, bool hasPlant)
    {
        if (!tilledCells.Contains(cell)) return;
        if (hasPlant) plantedCells.Add(cell);
        else plantedCells.Remove(cell);

        if (!tilledCellDay.ContainsKey(cell))
        {
            tilledCellDay[cell] = CurrentDay;
        }
        else if (!hasPlant)
        {
            tilledCellDay[cell] = CurrentDay;
        }

        if (HasScene)
        {
            int day = tilledCellDay.TryGetValue(cell, out var storedDay) ? storedDay : CurrentDay;
            SaveStore.UpdateSoilTilePending(sceneName, cell, day, hasPlant);
        }
    }

    void RefreshVisual(Vector2Int cell)
    {
        if (!visuals.TryGetValue(cell, out var go) || !go) return;
        if (!tilledCells.Contains(cell)) return;

        int mask = 0;
        for (int i = 0; i < CardinalOffsets.Length; ++i)
        {
            if (tilledCells.Contains(cell + CardinalOffsets[i]))
            {
                mask |= 1 << i;
            }
        }

        bool isWet = wetCells.Contains(cell);
        int wetMask = 0;
        if (isWet)
        {
            for (int i = 0; i < CardinalOffsets.Length; ++i)
            {
                if (wetCells.Contains(cell + CardinalOffsets[i]))
                {
                    wetMask |= 1 << i;
                }
            }
        }

        if (go.TryGetComponent<TilledSoilVisual>(out var visual))
        {
            visual.ApplyState(mask, isWet, wetMask);
        }
    }

    void RestoreFromSave()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        foreach (var state in SaveStore.GetTilledSoilInScene(sceneName))
        {
            var cell = new Vector2Int(state.x, state.y);
            AddCell(cell, false, state.tilledDay, state.hasPlant);
        }
        foreach (var state in SaveStore.GetWateredSoilInScene(sceneName))
        {
            EnsureCellWateredFromSave(new Vector2Int(state.x, state.y), state.day);
        }
    }

    int CurrentDay
    {
        get
        {
            if (time && time.isActiveAndEnabled) return time.day;
            var tm = FindFirstObjectByType<TimeManager>();
            if (tm)
            {
                AttachTimeManager(tm);
                return tm.day;
            }
            return SaveStore.PeekSavedDay();
        }
    }

    bool HasScene => !string.IsNullOrEmpty(sceneName);

    bool ClearWetCell(Vector2Int cell, bool markPending)
    {
        if (!wetCells.Remove(cell)) return false;
        wetCellDay.Remove(cell);
        if (markPending && HasScene)
        {
            SaveStore.MarkSoilDriedPending(sceneName, cell);
        }
        RefreshCellAndNeighbors(cell);
        return true;
    }

    void HandleNewDay()
    {
        DryWetSoil();
        SyncPlantedCellsWithScene();
        RevertExpiredTilledCells();
    }

    void AttachTimeManager(TimeManager tm)
    {
        if (!tm) return;
        if (time) time.OnNewDay -= HandleNewDay;
        time = tm;
        if (isActiveAndEnabled)
        {
            time.OnNewDay += HandleNewDay;
        }
    }

    void DryWetSoil()
    {
        if (wetCells.Count == 0) return;
        var toDry = new List<Vector2Int>(wetCells);
        foreach (var cell in toDry)
        {
            ClearWetCell(cell, true);
        }
    }

    void SyncPlantedCellsWithScene()
    {
        if (plantedCells.Count == 0) return;
        var toCheck = new List<Vector2Int>(plantedCells);
        foreach (var cell in toCheck)
        {
            if (IsPlantPresentAtCell(cell)) continue;
            plantedCells.Remove(cell);
            tilledCellDay[cell] = CurrentDay;
            if (HasScene)
            {
                int day = tilledCellDay.TryGetValue(cell, out var storedDay) ? storedDay : CurrentDay;
                SaveStore.UpdateSoilTilePending(sceneName, cell, day, false);
            }
        }
    }

    bool IsPlantPresentAtCell(Vector2Int cell)
    {
        Vector2 center = CellToWorld(cell);
        float radius = Mathf.Max(0.01f, gridSize) * Mathf.Clamp(plantPresenceCheckRadiusMultiplier, 0.05f, 1f);

        var filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };

        int hits = Physics2D.OverlapCircle(center, radius, filter, plantCheckResults);
        Transform ignoreRoot = tilledParent ? tilledParent : transform;
        for (int i = 0; i < hits; ++i)
        {
            var col = plantCheckResults[i];
            if (!col) continue;
            if (ignoreRoot && col.transform.IsChildOf(ignoreRoot)) continue;
            if (col.GetComponentInParent<PlantGrowth>())
            {
                System.Array.Clear(plantCheckResults, 0, plantCheckResults.Length);
                return true;
            }
        }

        System.Array.Clear(plantCheckResults, 0, plantCheckResults.Length);
        return false;
    }

    void RevertExpiredTilledCells()
    {
        if (tilledCells.Count == 0) return;
        int today = CurrentDay;
        int threshold = Mathf.Max(1, daysUntilRevert);
        var toClear = new List<Vector2Int>();

        foreach (var cell in tilledCells)
        {
            if (plantedCells.Contains(cell)) continue;
            if (!tilledCellDay.TryGetValue(cell, out var tilledDay)) continue;
            if (today - tilledDay >= threshold)
            {
                toClear.Add(cell);
            }
        }

        foreach (var cell in toClear)
        {
            TryClearCell(cell);
        }
    }
}
