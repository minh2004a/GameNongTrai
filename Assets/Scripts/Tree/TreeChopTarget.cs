using UnityEngine;

// Xử lý chặt cây: trừ HP, spawn FX, tạo gốc cây, và lưu trạng thái đã chặt
public class TreeChopTarget : MonoBehaviour, IToolTarget
{
    [Header("HP & Drop")]
    public int maxHp = 3;

    [Header("Prefabs")]
    public GameObject stumpPrefab;                 // gốc cây
    [SerializeField] GameObject chopFxPrefab;      // FX chặt

    int hp;
    SpriteRenderer sr;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Hit(ToolType tool, int damage, Vector2 pushDir)
    {
        if (tool != ToolType.Axe) return;

        hp = Mathf.Max(0, hp - Mathf.Max(1, damage));

        // FX mỗi nhát chém
        SpawnChopFX(transform.position);

        if (hp > 0) return;

        // Hết HP: tạo gốc cây
       var sTag = GetComponent<StumpOfTree>();
        if (sTag)
        {
            SaveStore.MarkStumpClearedPending(gameObject.scene.name, sTag.treeId);
            GetComponent<DropLootOnDeath>()?.Drop();
            Destroy(gameObject);
            return;
        }


        // Lưu đã chặt (nếu có hệ Save)
        // trong nhánh hp<=0 trước khi Destroy:
        var uid = GetComponent<UniqueId>();
        if (uid) SaveStore.MarkTreeChoppedPending(gameObject.scene.name, uid.Id);

        if (stumpPrefab){
            var stump = Instantiate(stumpPrefab, transform.position, transform.rotation, transform.parent);
            var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
            if (uid) tag.treeId = uid.Id;
        }

        GetComponent<DropLootOnDeath>()?.Drop();
        Destroy(gameObject);

    }
    void SpawnChopFX(Vector3 pos)
{
    if (!chopFxPrefab) return;

    var fx = Instantiate(chopFxPrefab, pos, Quaternion.identity);

    int layerId = SortingLayer.NameToID("FX_Back");        // dưới Characters
    foreach (var r in fx.GetComponentsInChildren<Renderer>(true))
    {
        r.sortingLayerID = layerId;
        r.sortingOrder   = 0;                               // không cần cao
        if (r is ParticleSystemRenderer psr)
            psr.sortingFudge = +10f;                       // đẩy hạt VỀ SAU
    }
}
}
