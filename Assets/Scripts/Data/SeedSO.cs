// SeedSO.cs
using UnityEngine;

public enum GrowthMode { FixedDays, RandomChance, RandomRange }

[CreateAssetMenu(menuName = "Plants/Seed")]
public class SeedSO : ScriptableObject
{
    public string seedId;

    [Tooltip("Prefab cho từng giai đoạn, cuối cùng là cây trưởng thành")]
    public GameObject[] stagePrefabs;

    [Header("Cố định theo ngày (giữ nguyên)")]
    [Tooltip("Số ngày cần cho mỗi stage, dài bằng stagePrefabs")]
    public int[] stageDays;

    [Header("Chế độ tăng trưởng")]
    public GrowthMode growthMode = GrowthMode.FixedDays;

    [Header("Ngẫu nhiên theo % mỗi ngày (0..1)")]
    [Tooltip("Xác suất mỗi ngày để lên stage, dài bằng stagePrefabs")]
    [Range(0f, 1f)] public float[] stageAdvanceChance;

    [Header("Ngẫu nhiên theo khoảng ngày [min,max] (bao gồm max)")]
    [Tooltip("Khoảng ngày cần cho mỗi stage, dài bằng stagePrefabs")]
    public Vector2Int[] stageDayRange;

    [Header("Ràng buộc trồng")]
    public float blockCheckRadius = 0.35f;
    public LayerMask blockMask;
    public bool snapToGrid = true;
    public float gridSize = 1f;
    #if UNITY_EDITOR
void OnValidate(){
    gridSize = Mathf.Max(0.01f, gridSize);
    if (stagePrefabs == null) return;
    int n = stagePrefabs.Length;

    if (stageDays != null && stageDays.Length != n){
        System.Array.Resize(ref stageDays, n);
    }
    if (stageAdvanceChance != null && stageAdvanceChance.Length != n){
        System.Array.Resize(ref stageAdvanceChance, n);
    }
    if (stageDayRange != null && stageDayRange.Length != n){
        System.Array.Resize(ref stageDayRange, n);
    }
}
#endif
}
