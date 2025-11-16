using UnityEngine;

// Xử lý chặt cây: trừ HP, spawn FX, tạo gốc cây, và lưu trạng thái đã chặt
public class TreeChopTarget : MonoBehaviour, IDamageable
{
    [Header("HP & Drop")]
    public int maxHp = 3;

    [Header("Prefabs")]
    public GameObject stumpPrefab;                 // gốc cây

    [SerializeField] GameObject chopFxPrefab;      // FX chặt mặc định

    [Header("FX theo mùa (0: Spring, 1: Summer, 2: Fall, 3: Winter)")]
    [SerializeField] GameObject[] seasonalChopFxPrefabs = new GameObject[4];

    SeasonManager seasonManager;   // cache cho đỡ Find hoài
    int hp;
    SpriteRenderer sr;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponentInChildren<SpriteRenderer>();
        seasonManager = FindFirstObjectByType<SeasonManager>();
    }
    public void TakeHit(int damage)
    {
        ApplyDamage(damage, Vector2.zero);
    }

    public void ApplyDamage(int damage, Vector2 pushDir)
    {
        if (hp <= 0) return;

        hp = Mathf.Max(0, hp - Mathf.Max(1, damage));

        // FX mỗi lần bị đánh
        SpawnChopFX(transform.position);

        if (hp > 0) return;

        Vector2 scatterDir = pushDir.sqrMagnitude > 0.001f ? pushDir.normalized : Vector2.zero;

        // Hết HP: tạo gốc cây
        var drop = GetComponent<DropLootOnDeath>();
        var sTag = GetComponent<StumpOfTree>();
        if (sTag)
        {
            SaveStore.MarkStumpClearedPending(gameObject.scene.name, sTag.treeId);
            if (scatterDir != Vector2.zero) drop?.SetScatterDirection(scatterDir);
            drop?.Drop();
            Destroy(gameObject);
            return;
        }

        // Lưu đã chặt (nếu có hệ Save)
        var uid = GetComponent<UniqueId>();
        var plant = GetComponentInParent<PlantGrowth>();
        if (uid) SaveStore.MarkTreeChoppedPending(gameObject.scene.name, uid.Id);

        if (scatterDir != Vector2.zero) drop?.SetScatterDirection(scatterDir);

        if (stumpPrefab)
        {
            var parent = plant ? plant.transform.parent : transform.parent;
            var stump = Instantiate(stumpPrefab, transform.position, transform.rotation, parent);
            var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
            if (uid) tag.treeId = uid.Id;
        }

        drop?.Drop();

        if (plant)
        {
            plant.RemoveFromSave();
            if (plant.gameObject != gameObject)
                Destroy(plant.gameObject);
            else
                Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SpawnChopFX(Vector3 pos)
    {
        var fxPrefab = GetChopFxPrefabForCurrentSeason();
        if (!fxPrefab) return;

        var fx = Instantiate(fxPrefab, pos, Quaternion.identity);

        int layerId = SortingLayer.NameToID("FX_Back");        // dưới Characters
        foreach (var r in fx.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerID = layerId;
            r.sortingOrder = 0;                               // không cần cao
            if (r is ParticleSystemRenderer psr)
                psr.sortingFudge = +10f;                      // đẩy hạt VỀ SAU
        }
    }
     public GameObject GetSeasonalStumpPrefab(SeasonManager.Season season = SeasonManager.Season.Spring)
        {
            // hiện tại mình không cần phân biệt mùa cho prefab gốc,
            // gốc đổi skin bằng SeasonalSprite, nên chỉ trả về stumpPrefab
            return stumpPrefab;
        }
    
        GameObject GetChopFxPrefabForCurrentSeason()
        {
            // Mặc định dùng FX chung
            GameObject fx = chopFxPrefab;

            if (!seasonManager)
                seasonManager = FindFirstObjectByType<SeasonManager>();

            if (seasonManager)
            {
                // enum Season { Spring = 0, Summer = 1, Fall = 2, Winter = 3 }
                int idx = (int)seasonManager.CurrentSeason;

                if (seasonalChopFxPrefabs != null &&
                    idx >= 0 && idx < seasonalChopFxPrefabs.Length &&
                    seasonalChopFxPrefabs[idx] != null)
                {
                    fx = seasonalChopFxPrefabs[idx];
                }
            }

            return fx;
        }


}
