using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ReapableGrass : MonoBehaviour, IReapable
{
    [SerializeField] PickupItem2D pickupPrefab;
    [SerializeField] ItemSO dropItem;
    [SerializeField, Min(0)] int minDrop = 0;
    [SerializeField, Min(0)] int maxDrop = 1;

    UniqueId uid;
    string sceneName;

    void Awake()
    {
        uid = GetComponent<UniqueId>();
        sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : null;
        if (!uid || string.IsNullOrEmpty(sceneName)) return;

        if (SaveStore.IsGrassReapedInSession(sceneName, uid.Id))
        {
            Destroy(gameObject);
        }
    }

    public void Reap(int damage, Vector2 hitDir, PlayerInventory inv)
    {
        int count = Random.Range(minDrop, maxDrop + 1);

        // Nếu 2 cái này đang None thì đoạn này sẽ SKIP, chỉ Destroy cỏ thôi
        if (dropItem && count > 0 && pickupPrefab)
        {
            var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            pickup.Set(dropItem, count);
        }

        if (uid && !string.IsNullOrEmpty(sceneName))
        {
            SaveStore.MarkGrassReapedPending(sceneName, uid.Id);
            SaveStore.RemoveGrassInstancePending(sceneName, uid.Id);
        }

        Destroy(gameObject);
    }
}
