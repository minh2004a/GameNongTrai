
using UnityEngine;

public class WildGrassManager : MonoBehaviour
{
    [Header("Prefab cỏ dại")]
    [SerializeField] ReapableGrass grassPrefab;
    [Header("Vùng spawn (ưu tiên BoxCollider2D)")]
    [SerializeField] BoxCollider2D area;
    [SerializeField] Transform grassParent;
    [Header("Thiết lập spawn")]
    [SerializeField, Min(0)] int targetCount = 12;
    [SerializeField, Min(0)] int maxSpawnPerDay = 3;
    [SerializeField, Min(0.1f)] float baseSpacing = 1f;
    [SerializeField, Range(0.05f, 1f)] float blockCheckRadiusMultiplier = 0.45f;
    [SerializeField] LayerMask blockMask;
    [SerializeField] bool snapToSoilGrid = true;
    [SerializeField] bool avoidTilledSoil = true;
    [SerializeField] SoilManager soilManager;

    readonly Collider2D[] blockCheckResults = new Collider2D[8];
    TimeManager time;
    string sceneName;

    void Awake()
    {
        sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : null;
        if (!grassParent) grassParent = transform;
        RestoreFromSave();
    }

    void Start()
    {
        AttachTimeManager(time ?? FindFirstObjectByType<TimeManager>());
        SpawnUpToTarget(maxSpawnPerDay);
    }

    void OnEnable()
    {
        AttachTimeManager(time ?? FindFirstObjectByType<TimeManager>());
    }

    void OnDisable()
    {
        if (time) time.OnNewDay -= HandleNewDay;
    }

    void AttachTimeManager(TimeManager tm)
    {
        if (!tm) return;
        if (time) time.OnNewDay -= HandleNewDay;
        time = tm;
        if (isActiveAndEnabled) time.OnNewDay += HandleNewDay;
    }

    void HandleNewDay()
    {
        SpawnUpToTarget(maxSpawnPerDay);
    }

    void RestoreFromSave()
    {
        if (string.IsNullOrEmpty(sceneName) || !grassPrefab) return;

        foreach (var state in SaveStore.GetGrassInstancesInScene(sceneName))
        {
            var pos = new Vector3(state.x, state.y, grassPrefab.transform.position.z);
            var grass = Instantiate(grassPrefab, pos, Quaternion.identity, grassParent);
            var uid = grass.GetComponent<UniqueId>() ?? grass.gameObject.AddComponent<UniqueId>();
            uid.ForceId(state.id);
        }
    }

    void SpawnUpToTarget(int maxNew)
    {
        if (!grassPrefab || string.IsNullOrEmpty(sceneName)) return;

        int existing = CountLivingGrass();
        int desired = Mathf.Max(0, targetCount - existing);
        int toSpawn = Mathf.Min(Mathf.Max(0, maxNew), desired);
        if (toSpawn <= 0) return;

        int attempts = Mathf.Max(toSpawn * 8, 8);
        int spawned = 0;
        for (int i = 0; i < attempts && spawned < toSpawn; ++i)
        {
            if (!TryPickPosition(out var pos)) continue;
            var grass = Instantiate(grassPrefab, pos, Quaternion.identity, grassParent);
            var uid = grass.GetComponent<UniqueId>() ?? grass.gameObject.AddComponent<UniqueId>();
            var state = new SaveStore.GrassInstanceState { id = uid.Id, x = pos.x, y = pos.y };
            SaveStore.SetGrassInstancePending(sceneName, state);
            spawned++;
        }
    }

    int CountLivingGrass()
    {
        if (!grassParent) return 0;
        int count = 0;
        var bounds = SpawnBounds;
        var grasses = grassParent.GetComponentsInChildren<ReapableGrass>(includeInactive: false);
        foreach (var g in grasses)
        {
            if (!g) continue;
            if (bounds.Contains(g.transform.position)) count++;
        }
        return count;
    }

    Bounds SpawnBounds
    {
        get
        {
            if (area) return area.bounds;
            float size = Mathf.Max(1f, targetCount * 0.5f * baseSpacing);
            return new Bounds(transform.position, new Vector3(size, size, 1f));
        }
    }

    bool TryPickPosition(out Vector2 pos)
    {
        var bounds = SpawnBounds;
        for (int i = 0; i < 4; ++i)
        {
            pos = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));

            if (snapToSoilGrid && soilManager)
            {
                var cell = soilManager.WorldToCell(pos);
                if (avoidTilledSoil && soilManager.IsCellTilled(cell)) continue;
                pos = soilManager.CellToWorld(cell);
                if (!bounds.Contains(pos)) continue;
            }

            if (IsBlocked(pos)) continue;
            return true;
        }
        pos = default;
        return false;
    }

    bool IsBlocked(Vector2 pos)
    {
        float radius = Mathf.Max(0.05f, baseSpacing) * Mathf.Clamp(blockCheckRadiusMultiplier, 0.05f, 1f);
        var filter = new ContactFilter2D
        {
            useLayerMask = blockMask.value != 0,
            layerMask = blockMask,
            useTriggers = true
        };

        int hits = Physics2D.OverlapCircle(pos, radius, filter, blockCheckResults);
        for (int i = 0; i < hits; ++i)
        {
            var col = blockCheckResults[i];
            blockCheckResults[i] = null;
            if (!col) continue;
            if (grassParent && col.transform.IsChildOf(grassParent)) continue;
            return true;
        }
        return false;
    }
}
