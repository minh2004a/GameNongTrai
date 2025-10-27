// PlayerPlanting.cs
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerPlanting : MonoBehaviour
{
    public PlantSystem plantSystem;
    Camera cam;
    PlayerInventory inv;

    void Awake(){
        inv = GetComponent<PlayerInventory>();
        cam = Camera.main;
        if (!plantSystem) plantSystem = FindFirstObjectByType<PlantSystem>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryPlantFromSelected();
    }

    void TryPlantFromSelected()
    {
        var item = inv.CurrentItem;
        if (!item || !item.seedData) return;                 // chỉ xử lý item là hạt
        Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(wp.x, wp.y);

        if (plantSystem && plantSystem.TryPlantAt(pos, item.seedData, out _))
        {
            inv.ConsumeSelected(1);                          // trừ 1 hạt sau khi trồng
        }
    }
}
