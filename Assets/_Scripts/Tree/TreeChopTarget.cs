

using UnityEngine;

// Xử lý chặt cây: trừ HP, spawn FX, tạo gốc cây, và lưu trạng thái đã chặt
public class TreeChopTarget : MonoBehaviour, IDamageable
{
    [Header("HP & Drop")]
    public int maxHp = 3;

    [Header("Prefabs")]
    public GameObject stumpPrefab;                 // gốc cây
    [SerializeField] GameObject chopFxPrefab;      // FX chặt

    [System.Serializable]
    public struct SeasonSpriteVariant
    {
        public SeasonManager.Season season;
        public Sprite sprite;
    }

    [Header("Seasonal visuals")]
    [SerializeField] SpriteRenderer seasonalSpriteRenderer;
    [SerializeField] Sprite defaultSeasonSprite;
    [SerializeField] SeasonSpriteVariant[] seasonalSprites;
    [Header("Seasonal chop FX")]
    [SerializeField] GameObject springChopFxPrefab;
    [SerializeField] GameObject summerChopFxPrefab;
    [SerializeField] GameObject fallChopFxPrefab;
    [SerializeField] GameObject winterChopFxPrefab;

    int hp;
    SpriteRenderer sr;
    SeasonManager seasonManager;
    bool seasonSubscribed;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (!seasonalSpriteRenderer) seasonalSpriteRenderer = sr;
        if (!defaultSeasonSprite && seasonalSpriteRenderer) defaultSeasonSprite = seasonalSpriteRenderer.sprite;
        EnsureSeasonSubscription();
        ApplySeasonalSprite();
    }

    void OnEnable()
    {
        EnsureSeasonSubscription();
        ApplySeasonalSprite();
    }

    void OnDisable()
    {
        if (seasonManager && seasonSubscribed)
        {
            seasonManager.OnSeasonChanged -= HandleSeasonChanged;
            seasonSubscribed = false;
        }
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
        // trong nhánh hp<=0 trước khi Destroy:
        var uid = GetComponent<UniqueId>();
        var plant = GetComponentInParent<PlantGrowth>();
        if (uid) SaveStore.MarkTreeChoppedPending(gameObject.scene.name, uid.Id);

        if (scatterDir != Vector2.zero) drop?.SetScatterDirection(scatterDir);

        var resolvedStumpPrefab = GetSeasonalStumpPrefab(GetCurrentSeason());

        if (plant)
        {
            drop?.Drop();
            plant.ReplaceWithStump(resolvedStumpPrefab);
            return;
        }

        if (resolvedStumpPrefab)
        {
            var stump = Instantiate(resolvedStumpPrefab, transform.position, transform.rotation, transform.parent);
            var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
            if (uid) tag.treeId = uid.Id;
        }

        drop?.Drop();

        Destroy(gameObject);

    }
    void SpawnChopFX(Vector3 pos)
    {
        var fxPrefab = ResolveChopFxPrefabForCurrentSeason();
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

    void EnsureSeasonSubscription()
    {
        var sm = FindActiveSeasonManager();
        if (sm != seasonManager)
        {
            if (seasonManager && seasonSubscribed)
            {
                seasonManager.OnSeasonChanged -= HandleSeasonChanged;
                seasonSubscribed = false;
            }
            seasonManager = sm;
        }

        if (seasonManager && !seasonSubscribed)
        {
            seasonManager.OnSeasonChanged += HandleSeasonChanged;
            seasonSubscribed = true;
        }
    }

    SeasonManager FindActiveSeasonManager()
    {
        if (seasonManager && seasonManager.isActiveAndEnabled) return seasonManager;
        return FindFirstObjectByType<SeasonManager>();
    }

    void HandleSeasonChanged(SeasonManager.Season season)
    {
        ApplySeasonalSprite();
    }

    void ApplySeasonalSprite()
    {
        var targetRenderer = seasonalSpriteRenderer ? seasonalSpriteRenderer : sr;
        if (!targetRenderer) return;

        var sprite = ResolveSpriteForSeason(GetCurrentSeason());
        if (sprite)
        {
            targetRenderer.sprite = sprite;
        }
        else if (defaultSeasonSprite)
        {
            targetRenderer.sprite = defaultSeasonSprite;
        }
    }

    Sprite ResolveSpriteForSeason(SeasonManager.Season season)
    {
        if (seasonalSprites != null)
        {
            for (int i = 0; i < seasonalSprites.Length; i++)
            {
                if (seasonalSprites[i].season == season && seasonalSprites[i].sprite)
                {
                    return seasonalSprites[i].sprite;
                }
            }
        }
        return defaultSeasonSprite;
    }

    SeasonManager.Season GetCurrentSeason()
    {
        EnsureSeasonSubscription();
        var sm = FindActiveSeasonManager();
        return sm ? sm.CurrentSeason : SeasonManager.Season.Spring;
    }

    public GameObject GetSeasonalStumpPrefab(SeasonManager.Season season)
    {
        return stumpPrefab;
    }

    GameObject ResolveChopFxPrefabForCurrentSeason()
    {
        var season = GetCurrentSeason();
        var prefab = GetSeasonalChopFxPrefab(season);
        if (prefab) return prefab;
        return chopFxPrefab;
    }

    GameObject GetSeasonalChopFxPrefab(SeasonManager.Season season)
    {
        switch (season)
        {
            case SeasonManager.Season.Spring:
                return springChopFxPrefab;
            case SeasonManager.Season.Summer:
                return summerChopFxPrefab;
            case SeasonManager.Season.Fall:
                return fallChopFxPrefab;
            case SeasonManager.Season.Winter:
                return winterChopFxPrefab;
        }
        return null;
    }
}
