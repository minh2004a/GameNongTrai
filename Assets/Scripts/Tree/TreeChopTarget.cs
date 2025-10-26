// TreeChopTarget.cs
using UnityEngine;

public class TreeChopTarget : MonoBehaviour, IToolTarget
{
    [Header("HP & Drop")]
    public int maxHp = 3;
    [Header("Prefabs")]
    public GameObject stumpPrefab;         // gốc cây
    [SerializeField] GameObject chopFxPrefab;   // nhận prefab FX của bạn

    int hp;

    void Awake(){ hp = maxHp; }
    void SpawnChopFX(Vector3 pos){
    if (!chopFxPrefab) return;
    var fx = Instantiate(chopFxPrefab, pos, Quaternion.identity);  // tạo bản sao runtime :contentReference[oaicite:0]{index=0}

    // Nếu có ParticleSystem → Play tất cả hệ hạt con
    var pss = fx.GetComponentsInChildren<ParticleSystem>(true);
    for (int i = 0; i < pss.Length; i++) pss[i].Play();            // chạy hạt ngay lập tức :contentReference[oaicite:1]{index=1}

    // Auto-hủy nếu FX của bạn CHƯA bật Stop Action = Destroy
    if (pss.Length > 0) StartCoroutine(CoAutoDestroy(pss, fx));
}
    public void Hit(ToolType tool, int damage, Vector2 pushDir)
    {
        if (tool != ToolType.Axe) return;

        // FX chặt
        if (chopFxPrefab)
        {
            var fx = Instantiate(chopFxPrefab, transform.position, Quaternion.identity);                    // one-shot rồi tự hủy nếu prefab đã set Stop Action = Destroy
        }                                   // Instantiate + Play là workflow chuẩn. :contentReference[oaicite:1]{index=1}
        // trừ máu
        hp = Mathf.Max(0, hp - Mathf.Max(1, damage));
        if (hp > 0) return;
        SpawnChopFX(transform.position);
        // thay bằng gốc
        if (stumpPrefab)
            Instantiate(stumpPrefab, transform.position, transform.rotation);
        GetComponent<DropLootOnDeath>()?.Drop();
        Destroy(gameObject); // xoá thân cây

    }
    System.Collections.IEnumerator CoAutoDestroy(ParticleSystem[] pss, GameObject fx){
    // nếu bạn đã bật Stop Action = Destroy thì đoạn này không cần :contentReference[oaicite:2]{index=2}
    bool AnyAlive(){
        for (int i = 0; i < pss.Length; i++) if (pss[i] && pss[i].IsAlive(true)) return true;
        return false;
    }
    while (AnyAlive()) yield return null;
    Destroy(fx);
}
}
