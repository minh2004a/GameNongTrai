// PlayerPlanting.cs
using UnityEngine;
// Quản lý việc trồng cây của người chơi từ hạt giống
[RequireComponent(typeof(PlayerInventory))]
public class PlayerPlanting : MonoBehaviour
{
    public PlantSystem plantSystem;
    Camera cam;
    PlayerInventory inv;
    [SerializeField] LayerMask harvestMask = ~0;
    [SerializeField, Min(0f)] float harvestRange = 1.5f;

    void Awake(){
        inv = GetComponent<PlayerInventory>();
        cam = Camera.main;
        if (!plantSystem) plantSystem = FindFirstObjectByType<PlantSystem>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!cam) cam = Camera.main;
            Vector3 wp3 = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 wp = new Vector2(wp3.x, wp3.y);

            if (TryHarvestAt(wp)) return;
            TryPlantFromSelected(wp);
        }
    }

    // PlayerPlanting.cs — sửa TryPlantFromSelected()
    void TryPlantFromSelected(Vector2 worldPos)
    {
        var it = inv.CurrentItem; var seed = it ? it.seedData : null;
        if (!seed) return;

        if (!plantSystem.CanPlantAt(worldPos, transform.position, seed.gridSize * 1f,
            seed, out var snapped, out var blocked, out var tooFar)) return;

        if (plantSystem.TryPlantAt(snapped, seed, out _)) inv.ConsumeSelected(1);
    }

    bool TryHarvestAt(Vector2 worldPos)
    {
        int mask = harvestMask.value;
        if (mask == 0) mask = Physics2D.AllLayers;

        float radius = Mathf.Max(0.01f, GetHarvestGridSize()) * 0.5f;
        var hits = Physics2D.OverlapCircleAll(worldPos, radius, mask);
        if (hits == null || hits.Length == 0) return false;

        PlantGrowth best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            var plant = h.GetComponentInParent<PlantGrowth>();
            if (!plant || !plant.CanHarvestByHand) continue;
            float distToCursor = ((Vector2)plant.transform.position - worldPos).sqrMagnitude;
            if (distToCursor < bestDist)
            {
                best = plant;
                bestDist = distToCursor;
            }
        }

        if (!best) return false;
        if (!IsWithinHarvestGrid(best.transform.position)) return false;
        if (Vector2.Distance(transform.position, best.transform.position) > harvestRange) return false;

        return best.TryHarvestByHand(inv);
    }

    bool IsWithinHarvestGrid(Vector2 targetPos)
    {
        float grid = Mathf.Max(0.01f, GetHarvestGridSize());
        Vector2 playerPos = transform.position;

        int px = Mathf.FloorToInt(playerPos.x / grid);
        int py = Mathf.FloorToInt(playerPos.y / grid);
        int tx = Mathf.FloorToInt(targetPos.x / grid);
        int ty = Mathf.FloorToInt(targetPos.y / grid);

        const int radiusInCells = 1; // 3x3 ô quanh người chơi
        return Mathf.Abs(tx - px) <= radiusInCells && Mathf.Abs(ty - py) <= radiusInCells;
    }

    float GetHarvestGridSize()
    {
        if (plantSystem && plantSystem.soilManager)
        {
            return Mathf.Max(0.01f, plantSystem.soilManager.GridSize);
        }

        return 1f;
    }

}
