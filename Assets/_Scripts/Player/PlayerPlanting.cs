// PlayerPlanting.cs
using UnityEngine;
// Quản lý việc trồng cây của người chơi từ hạt giống
[RequireComponent(typeof(PlayerInventory))]
public class PlayerPlanting : MonoBehaviour
{
    public PlantSystem plantSystem;
    [SerializeField] PlayerController controller;
    Camera cam;
    PlayerInventory inv;
    [SerializeField] LayerMask harvestMask = ~0;
    [SerializeField, Min(0f)] float harvestClickRadius = 0.25f;
    [SerializeField, Min(0f)] float harvestRange = 1.5f;
    [SerializeField, Range(-1f, 1f)] float harvestFacingDotThreshold = 0f;

    void Awake(){
        inv = GetComponent<PlayerInventory>();
        if (!controller) controller = GetComponent<PlayerController>();
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
        if (inv && inv.CurrentItem)
            return false; // phải tay không mới được thu hoạch

        int mask = harvestMask.value;
        if (mask == 0) mask = Physics2D.AllLayers;

        float radius = Mathf.Max(0.01f, harvestClickRadius);
        var hits = Physics2D.OverlapCircleAll(worldPos, radius, mask);
        if (hits == null || hits.Length == 0) return false;

        var facing = GetFacingDirection();
        PlantGrowth best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            var plant = h.GetComponentInParent<PlantGrowth>();
            if (!plant || !plant.CanHarvestByHand) continue;
            if (!IsInFrontOfPlayer(plant, facing)) continue;
            float distToCursor = ((Vector2)plant.transform.position - worldPos).sqrMagnitude;
            if (distToCursor < bestDist)
            {
                best = plant;
                bestDist = distToCursor;
            }
        }

        if (!best) return false;
        if (Vector2.Distance(transform.position, best.transform.position) > harvestRange) return false;

        return best.TryHarvestByHand(inv);
    }

    Vector2 GetFacingDirection()
    {
        if (controller)
        {
            var dir = controller.Facing4;
            if (dir.sqrMagnitude > 0.0001f)
                return dir;
        }

        // mặc định nhìn sang phải nếu không lấy được hướng
        return Vector2.right;
    }

    bool IsInFrontOfPlayer(PlantGrowth plant, Vector2 facing)
    {
        var toPlant = ((Vector2)plant.transform.position - (Vector2)transform.position);
        if (toPlant.sqrMagnitude <= 0.0001f)
            return true; // nằm cùng vị trí, cho phép

        toPlant.Normalize();
        return Vector2.Dot(facing, toPlant) >= harvestFacingDotThreshold;
    }
}
