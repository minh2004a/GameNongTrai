



// SeedSO.cs
using UnityEngine;

public enum GrowthMode { FixedDays, RandomChance, RandomRange }
public enum HarvestMethod { None, Hand, Tool }

[CreateAssetMenu(menuName = "Plants/Seed")]
public class SeedSO : ScriptableObject
{
    [System.Serializable]
    public struct SeasonalStagePrefabSet
    {
        public SeasonManager.Season season;
        public GameObject[] stagePrefabs;
    }

    static readonly System.Collections.Generic.Dictionary<string, SeedSO> byId =
        new System.Collections.Generic.Dictionary<string, SeedSO>();
    public string seedId;

    [Tooltip("Prefab cho từng giai đoạn, cuối cùng là cây trưởng thành")]
    public GameObject[] stagePrefabs;

    [Tooltip("Tùy chọn: prefab theo mùa cho từng giai đoạn. Bỏ trống để dùng prefab mặc định.")]
    public SeasonalStagePrefabSet[] seasonalStagePrefabs;

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
    [Tooltip("Chỉ có thể trồng trên ô đất đã được xới")]
    public bool requiresTilledSoil = false;
    [Tooltip("Phải tưới nước mỗi ngày mới phát triển")]
    public bool requiresWatering = false;

    [Header("Thu hoạch")]
    [Tooltip("Phương thức thu hoạch khi cây trưởng thành")]
    public HarvestMethod harvestMethod = HarvestMethod.None;
    [Tooltip("Vật phẩm thu được khi thu hoạch")]
    public ItemSO harvestItem;
    [Min(1), Tooltip("Số lượng vật phẩm thu được khi thu hoạch")]
    public int harvestItemCount = 1;
    [Tooltip("Xóa cây sau khi thu hoạch thành công")]
    public bool destroyOnHarvest = true;

    [Header("Mùa vụ")]
    [Tooltip("Danh sách mùa mà hạt giống có thể trồng và tồn tại. Để trống để cho phép quanh năm." )]
    public SeasonManager.Season[] allowedSeasons;

    public bool HasSeasonRestrictions => allowedSeasons != null && allowedSeasons.Length > 0;

    public bool IsSeasonAllowed(SeasonManager.Season season)
    {
        if (allowedSeasons == null || allowedSeasons.Length == 0) return true;
        for (int i = 0; i < allowedSeasons.Length; i++)
        {
            if (allowedSeasons[i] == season) return true;
        }
        return false;
    }

    public GameObject GetStagePrefabForSeason(int stageIndex, SeasonManager.Season season)
    {
        if (stageIndex < 0)
        {
            stageIndex = 0;
        }

        var seasonal = GetSeasonalStageArray(season);
        if (seasonal != null && stageIndex < seasonal.Length)
        {
            var prefab = seasonal[stageIndex];
            if (prefab) return prefab;
        }

        return GetDefaultStagePrefab(stageIndex);
    }

    GameObject[] GetSeasonalStageArray(SeasonManager.Season season)
    {
        if (seasonalStagePrefabs == null) return null;
        for (int i = 0; i < seasonalStagePrefabs.Length; i++)
        {
            if (seasonalStagePrefabs[i].season == season)
            {
                return seasonalStagePrefabs[i].stagePrefabs;
            }
        }
        return null;
    }

    public GameObject GetDefaultStagePrefab(int stageIndex)
    {
        if (stagePrefabs == null || stagePrefabs.Length == 0) return null;
        if (stageIndex < stagePrefabs.Length)
        {
            return stagePrefabs[Mathf.Clamp(stageIndex, 0, stagePrefabs.Length - 1)];
        }
        return stagePrefabs[stagePrefabs.Length - 1];
    }

    public System.Collections.Generic.IEnumerable<GameObject> EnumerateAllStagePrefabs()
    {
        if (stagePrefabs != null)
        {
            for (int i = 0; i < stagePrefabs.Length; i++)
            {
                var prefab = stagePrefabs[i];
                if (prefab) yield return prefab;
            }
        }

        if (seasonalStagePrefabs != null)
        {
            for (int i = 0; i < seasonalStagePrefabs.Length; i++)
            {
                var set = seasonalStagePrefabs[i];
                if (set.stagePrefabs == null) continue;
                for (int j = 0; j < set.stagePrefabs.Length; j++)
                {
                    var prefab = set.stagePrefabs[j];
                    if (prefab) yield return prefab;
                }
            }
        }
    }
    void OnEnable(){
        if (!string.IsNullOrEmpty(seedId)) byId[seedId] = this;
    }
    void OnDisable(){
        if (!string.IsNullOrEmpty(seedId) && byId.TryGetValue(seedId, out var current) && current == this)
            byId.Remove(seedId);
    }
    public static SeedSO Find(string id){
        if (string.IsNullOrEmpty(id)) return null;
        if (byId.TryGetValue(id, out var seed) && seed) return seed;
        foreach (var s in Resources.FindObjectsOfTypeAll<SeedSO>())
        {
            if (string.IsNullOrEmpty(s.seedId)) continue;
            byId[s.seedId] = s;
        }
        return byId.TryGetValue(id, out seed) ? seed : null;
    }
#if UNITY_EDITOR
void OnValidate(){
    gridSize = Mathf.Max(0.01f, gridSize);
    harvestItemCount = Mathf.Max(1, harvestItemCount);
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
    if (seasonalStagePrefabs != null)
    {
        for (int i = 0; i < seasonalStagePrefabs.Length; i++)
        {
            var entry = seasonalStagePrefabs[i];
            if (entry.stagePrefabs == null)
            {
                entry.stagePrefabs = new GameObject[n];
                seasonalStagePrefabs[i] = entry;
                continue;
            }

            if (entry.stagePrefabs.Length != n)
            {
                System.Array.Resize(ref entry.stagePrefabs, n);
                seasonalStagePrefabs[i] = entry;
            }
        }
    }

    if (!string.IsNullOrEmpty(seedId)) byId[seedId] = this;
}
#endif
}
