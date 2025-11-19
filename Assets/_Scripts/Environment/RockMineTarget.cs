using UnityEngine;

/// <summary>
/// Đối tượng đá/khoáng sản có thể bị đập bằng cuốc chim.
/// Quản lý HP, FX và rơi loot khi bị phá.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueId))]
public class RockMineTarget : MonoBehaviour, IMineable
{
    [Header("HP")]
    [Min(1)] public int maxHp = 3;

    [Header("FX")]
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] GameObject breakFxPrefab;

    [Header("Drop")]
    [SerializeField] DropLootOnDeath dropOnBreak;

    int hp;
    UniqueId uid;
    string sceneName;

    void Awake()
    {
        hp = Mathf.Max(1, maxHp);
        if (!dropOnBreak) dropOnBreak = GetComponent<DropLootOnDeath>();

        uid = GetComponent<UniqueId>();
        sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : null;

        if (uid && !string.IsNullOrEmpty(sceneName) && SaveStore.IsRockMinedInSession(sceneName, uid.Id))
        {
            Destroy(gameObject);
            return;
        }
    }

    public bool IsDepleted => hp <= 0;

    public void Mine(int power, Vector2 hitDir)
    {
        if (IsDepleted) return;

        hp = Mathf.Max(0, hp - Mathf.Max(1, power));
        SpawnFx(hitFxPrefab);

        if (hp > 0) return;

        if (uid && !string.IsNullOrEmpty(sceneName))
        {
            SaveStore.MarkRockMinedPending(sceneName, uid.Id);
        }

        if (dropOnBreak)
        {
            if (hitDir.sqrMagnitude > 0.001f)
                dropOnBreak.SetScatterDirection(hitDir);
            dropOnBreak.Drop();
        }

        SpawnFx(breakFxPrefab);
        Destroy(gameObject);
    }

    void SpawnFx(GameObject prefab)
    {
        if (!prefab) return;
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}