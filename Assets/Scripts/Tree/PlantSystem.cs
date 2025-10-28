// PlantSystem.cs
using UnityEngine;
using UnityEngine.EventSystems;
// Hệ thống trồng cây từ hạt giống
public class PlantSystem : MonoBehaviour
{
    [Header("Prefab gốc chứa PlantGrowth")]
    public GameObject plantRootPrefab;

    public bool TryPlantAt(Vector2 worldPos, SeedSO seed, out PlantGrowth plant)
    {
        plant = null; if (!seed || !plantRootPrefab) return false;

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false; // tránh click UI

        // Snap lưới nếu bật
        // bên trong TryPlantAt, phần snap:
        if (seed.snapToGrid)
        {
            float s = Mathf.Max(0.01f, seed.gridSize);
            worldPos = new Vector2(
                Mathf.Floor(worldPos.x / s) * s + 0.5f * s,
                Mathf.Floor(worldPos.y / s) * s + 0.5f * s
            );
        }

if (seed.snapToGrid)
{
    float s = Mathf.Max(0.01f, seed.gridSize);
    worldPos = new Vector2(
        Mathf.Floor(worldPos.x / s) * s + 0.5f * s,
        Mathf.Floor(worldPos.y / s) * s + 0.5f * s
    );
}

        // Chặn trồng đè vật cản / cây khác
        if (Physics2D.OverlapCircle(worldPos, seed.blockCheckRadius, seed.blockMask)) return false; // kiểm tra vùng tròn nhanh gọn. :contentReference[oaicite:1]{index=1}

        var go = Instantiate(plantRootPrefab, worldPos, Quaternion.identity); // sinh prefab tại vị trí. :contentReference[oaicite:2]{index=2}
        plant = go.GetComponent<PlantGrowth>() ?? go.AddComponent<PlantGrowth>();
        plant.Init(seed);
        return true;
    }
    public bool CanPlantAt(Vector2 mouseWorld, Vector2 playerPos, float _,
                        SeedSO seed, out Vector2 snapped, out bool blocked, out bool tooFar)
    {
        // SNAP: tâm ô kích thước seed.gridSize
        float s = Mathf.Max(0.01f, seed.gridSize);
        if (seed.snapToGrid)
        {
            snapped = new Vector2(
                Mathf.Floor(mouseWorld.x / s) * s + 0.5f * s,
                Mathf.Floor(mouseWorld.y / s) * s + 0.5f * s
            );
        }
        else snapped = mouseWorld;

        // Vật cản
        blocked = Physics2D.OverlapCircle(snapped, seed.blockCheckRadius, seed.blockMask);

        // Chebyshev: max(|dx|,|dy|) <= 1  ⇒ “1 ô quanh player”
        int ix = Mathf.FloorToInt(snapped.x / s);
        int iy = Mathf.FloorToInt(snapped.y / s);
        int px = Mathf.FloorToInt(playerPos.x / s);
        int py = Mathf.FloorToInt(playerPos.y / s);
        bool inRange = Mathf.Max(Mathf.Abs(ix - px), Mathf.Abs(iy - py)) <= 1;

        tooFar = !inRange;
        return inRange && !blocked;
    }

}
