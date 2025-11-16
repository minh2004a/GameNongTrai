using UnityEngine;

public class ReapableGrass : MonoBehaviour, IReapable
{
    [SerializeField] PickupItem2D pickupPrefab;
    [SerializeField] ItemSO dropItem;
    [SerializeField, Min(0)] int minDrop = 0;
    [SerializeField, Min(0)] int maxDrop = 1;

    public void Reap(int damage, Vector2 hitDir, PlayerInventory inv)
    {
        int count = Random.Range(minDrop, maxDrop + 1);

        // Nếu 2 cái này đang None thì đoạn này sẽ SKIP, chỉ Destroy cỏ thôi
        if (dropItem && count > 0 && pickupPrefab)
        {
            var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            pickup.Set(dropItem, count);
        }

        Destroy(gameObject);
    }
}
