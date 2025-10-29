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
}
