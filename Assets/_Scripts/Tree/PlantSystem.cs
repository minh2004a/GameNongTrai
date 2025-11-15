

// PlantSystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
// Hệ thống trồng cây từ hạt giống
// PlantSystem.cs
public class PlantSystem : MonoBehaviour
{
    [Header("Prefab gốc chứa PlantGrowth")]
    public GameObject plantRootPrefab;
    [Header("Đất canh tác")]
    public SoilManager soilManager;

    SoilManager GetSoilManager()
    {
        if (soilManager && soilManager.isActiveAndEnabled) return soilManager;
        soilManager = FindFirstObjectByType<SoilManager>();
        return soilManager;
    }

    SeasonManager seasonManager;

    SeasonManager GetSeasonManager()
    {
        if (seasonManager && seasonManager.isActiveAndEnabled) return seasonManager;
        seasonManager = FindFirstObjectByType<SeasonManager>();
        return seasonManager;
    }

    bool IsSeedAllowedInCurrentSeason(SeedSO seed)
    {
        if (!seed || !seed.HasSeasonRestrictions) return true;
        var sm = GetSeasonManager();
        if (!sm) return true;
        return seed.IsSeasonAllowed(sm.CurrentSeason);
    }

    void Start()
    {
        RestorePlantsFromSave();
    }

    public bool TryPlantAt(Vector2 worldPos, SeedSO seed, out PlantGrowth plant)
    {
        plant = null; if (!seed || !plantRootPrefab) return false;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false; // tránh click UI
        if (!IsSeedAllowedInCurrentSeason(seed)) return false;

        SoilManager soil = GetSoilManager();
        Vector2Int? tilledCellToClear = null;

        if (seed.requiresTilledSoil)
        {
            if (!soil) return false;
            var cell = soil.WorldToCell(worldPos);
            if (!soil.IsCellTilled(cell)) return false;
            worldPos = soil.CellToWorld(cell);
        }
        else if (seed.snapToGrid){
            float s = Mathf.Max(0.01f, seed.gridSize);
            worldPos = new Vector2(
                Mathf.Floor(worldPos.x / s) * s + 0.5f * s,
                Mathf.Floor(worldPos.y / s) * s + 0.5f * s
            );
        }

        if (!seed.requiresTilledSoil && soil && soil.IsTilled(worldPos))
        {
            var cell = soil.WorldToCell(worldPos);
            if (soil.IsCellTilled(cell))
            {
                tilledCellToClear = cell;
                worldPos = soil.CellToWorld(cell);
            }
        }

        if (Physics2D.OverlapCircle(worldPos, seed.blockCheckRadius, seed.blockMask)) return false;

        var go = Instantiate(plantRootPrefab, worldPos, Quaternion.identity);
        plant = go.GetComponent<PlantGrowth>() ?? go.AddComponent<PlantGrowth>();
        plant.Init(seed);

        if (tilledCellToClear.HasValue && soil)
        {
            soil.TryClearCell(tilledCellToClear.Value);
        }
        else if (seed.requiresTilledSoil && soil)
        {
            var cell = soil.WorldToCell(worldPos);
            soil.SetCellHasPlant(cell, true);
            if (soil.IsCellWet(cell))
            {
                plant.Water();
            }
        }

        return true;
    }

    public bool CanPlantAt(Vector2 mouseWorld, Vector2 playerPos, float rangeTiles,
                           SeedSO seed, out Vector2 snapped, out bool blocked, out bool tooFar)
    {
        float s = Mathf.Max(0.01f, seed.gridSize);
        snapped = seed.snapToGrid
            ? new Vector2(Mathf.Floor(mouseWorld.x / s) * s + 0.5f * s,
                          Mathf.Floor(mouseWorld.y / s) * s + 0.5f * s)
            : mouseWorld;

        SoilManager soil = null;
        if (seed.requiresTilledSoil)
        {
            soil = GetSoilManager();
            if (soil)
            {
                snapped = soil.SnapToGrid(snapped);
            }
        }

        blocked = Physics2D.OverlapCircle(snapped, seed.blockCheckRadius, seed.blockMask);

        if (seed.requiresTilledSoil)
        {
            bool tilledOk = soil && soil.IsTilled(snapped);
            blocked = blocked || !tilledOk;
        }

        if (!IsSeedAllowedInCurrentSeason(seed))
        {
            blocked = true;
        }

        float grid = seed.requiresTilledSoil && soil ? soil.GridSize : s;
        int ix = Mathf.FloorToInt(snapped.x / grid),  iy = Mathf.FloorToInt(snapped.y / grid);
        int px = Mathf.FloorToInt(playerPos.x / grid), py = Mathf.FloorToInt(playerPos.y / grid);
        int r  = Mathf.Max(0, Mathf.RoundToInt(rangeTiles));
        bool inRange = Mathf.Max(Mathf.Abs(ix - px), Mathf.Abs(iy - py)) <= r;

        tooFar = !inRange;
        return inRange && !blocked;
    }

    void RestorePlantsFromSave()
    {
        if (!plantRootPrefab) return;
        var scene = gameObject.scene.IsValid() ? gameObject.scene.name : null;
        if (string.IsNullOrEmpty(scene)) return;

        List<SaveStore.PlantState> states = new List<SaveStore.PlantState>(SaveStore.GetPlantsInScene(scene));
        foreach (var state in states)
        {
            if (string.IsNullOrEmpty(state.seedId)) continue;
            var seed = SeedSO.Find(state.seedId);
            if (!seed)
            {
                Debug.LogWarning($"PlantSystem: Không tìm thấy SeedSO với id '{state.seedId}' để khôi phục.");
                continue;
            }

            if (state.isStump)
            {
                var stumpPrefab = PlantGrowth.ResolveStumpPrefab(seed);
                if (!stumpPrefab)
                {
                    Debug.LogWarning($"PlantSystem: Seed '{state.seedId}' không có stumpPrefab để khôi phục gốc cây.");
                    if (!string.IsNullOrEmpty(state.id))
                    {
                        SaveStore.RemovePlantPending(scene, state.id);
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(state.id))
                {
                    Debug.LogWarning($"PlantSystem: Plant stump ở scene '{scene}' thiếu id, bỏ qua.");
                    continue;
                }

                var stumpPos = new Vector3(state.x, state.y, plantRootPrefab.transform.position.z);
                var stump = Instantiate(stumpPrefab, stumpPos, Quaternion.identity);
                var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
                tag.treeId = state.id;
                continue;
            }

            if (!IsSeedAllowedInCurrentSeason(seed))
            {
                if (!string.IsNullOrEmpty(state.id))
                {
                    SaveStore.RemovePlantPending(scene, state.id);
                }
                continue;
            }

            var prefabPos = plantRootPrefab.transform.position;
            var pos = new Vector3(state.x, state.y, prefabPos.z);
            if (seed.requiresTilledSoil)
            {
                var soil = GetSoilManager();
                if (soil)
                {
                    var cell = soil.WorldToCell(pos);
                    soil.EnsureCellTilledFromSave(cell);
                    var center = soil.CellToWorld(cell);
                    pos = new Vector3(center.x, center.y, prefabPos.z);
                }
            }
            var go = Instantiate(plantRootPrefab, pos, Quaternion.identity);
            var growth = go.GetComponent<PlantGrowth>() ?? go.AddComponent<PlantGrowth>();
            growth.Restore(seed, state);
            if (seed.requiresTilledSoil)
            {
                var soil = GetSoilManager();
                if (soil)
                {
                    var cell = soil.WorldToCell(pos);
                    soil.SetCellHasPlant(cell, true);
                }
            }
        }
    }
}
