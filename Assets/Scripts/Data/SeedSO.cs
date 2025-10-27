// SeedSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Plants/Seed")]
public class SeedSO : ScriptableObject
{
    public string seedId;
    [Tooltip("Prefab cho từng giai đoạn, cuối cùng là cây trưởng thành")]
    public GameObject[] stagePrefabs;
    [Tooltip("Số ngày cần cho mỗi stage, chiều dài bằng stagePrefabs")]
    public int[] stageDays;

    [Header("Ràng buộc trồng")]
    public float blockCheckRadius = 0.35f;
    public LayerMask blockMask;          // Ground, Tree, Rock…
    public bool snapToGrid = true;
    public float gridSize = 1f;          // 1 ô = 1 đơn vị
}
