// PlantSystem.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class PlantSystem : MonoBehaviour
{
    [Header("Prefab gốc chứa PlantGrowth")]
    public GameObject plantRootPrefab;

    public bool TryPlantAt(Vector2 worldPos, SeedSO seed, out PlantGrowth plant)
    {
        plant = null; if (!seed || !plantRootPrefab) return false;

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false; // tránh click UI

        // Snap lưới nếu bật
        if (seed.snapToGrid){
            float s = Mathf.Max(0.01f, seed.gridSize);
            worldPos = new Vector2(Mathf.Round(worldPos.x / s) * s, Mathf.Round(worldPos.y / s) * s);
        }

        // Chặn trồng đè vật cản / cây khác
        if (Physics2D.OverlapCircle(worldPos, seed.blockCheckRadius, seed.blockMask)) return false; // kiểm tra vùng tròn nhanh gọn. :contentReference[oaicite:1]{index=1}

        var go = Instantiate(plantRootPrefab, worldPos, Quaternion.identity); // sinh prefab tại vị trí. :contentReference[oaicite:2]{index=2}
        plant = go.GetComponent<PlantGrowth>() ?? go.AddComponent<PlantGrowth>();
        plant.Init(seed);
        return true;
    }
}
