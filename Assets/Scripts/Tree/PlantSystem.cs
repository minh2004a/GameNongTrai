// PlantSystem.cs
using UnityEngine;
using UnityEngine.EventSystems;
// Hệ thống trồng cây từ hạt giống
// PlantSystem.cs
public class PlantSystem : MonoBehaviour
{
    [Header("Prefab gốc chứa PlantGrowth")]
    public GameObject plantRootPrefab;

    public bool TryPlantAt(Vector2 worldPos, SeedSO seed, out PlantGrowth plant)
    {
        plant = null; if (!seed || !plantRootPrefab) return false;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false; // tránh click UI

        if (seed.snapToGrid){
            float s = Mathf.Max(0.01f, seed.gridSize);
            worldPos = new Vector2(
                Mathf.Floor(worldPos.x / s) * s + 0.5f * s,
                Mathf.Floor(worldPos.y / s) * s + 0.5f * s
            );
        }

        if (Physics2D.OverlapCircle(worldPos, seed.blockCheckRadius, seed.blockMask)) return false;

        var go = Instantiate(plantRootPrefab, worldPos, Quaternion.identity);
        plant = go.GetComponent<PlantGrowth>() ?? go.AddComponent<PlantGrowth>();
        plant.Init(seed);
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

        blocked = Physics2D.OverlapCircle(snapped, seed.blockCheckRadius, seed.blockMask);

        int ix = Mathf.FloorToInt(snapped.x / s),  iy = Mathf.FloorToInt(snapped.y / s);
        int px = Mathf.FloorToInt(playerPos.x / s), py = Mathf.FloorToInt(playerPos.y / s);
        int r  = Mathf.Max(0, Mathf.RoundToInt(rangeTiles));
        bool inRange = Mathf.Max(Mathf.Abs(ix - px), Mathf.Abs(iy - py)) <= r;

        tooFar = !inRange;
        return inRange && !blocked;
    }
}
