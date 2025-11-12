using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Đánh dấu vùng nước có thể câu cá và cung cấp bảng loot cho khu vực đó.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FishingSpot : MonoBehaviour
{
    [System.Serializable]
    public struct LootEntry
    {
        public ItemSO item;
        [Min(0f)] public float weight;
        [Min(1)] public int minCount;
        [Min(1)] public int maxCount;

        public int RollCount()
        {
            int min = Mathf.Max(1, minCount);
            int max = Mathf.Max(min, maxCount);
            return Random.Range(min, max + 1);
        }
    }

    static readonly List<FishingSpot> AllSpots = new();

    [Header("Area")]
    [SerializeField] Collider2D fishingArea;
    [SerializeField] bool requirePointInside = true;

    [Header("Catch Timing")]
    [SerializeField, Min(0f)] float minBiteDelay = 0.8f;
    [SerializeField, Min(0f)] float maxBiteDelay = 2.2f;

    [Header("Loot Table")]
    [SerializeField] LootEntry[] lootTable;

    [Header("Rơi ra ngoài nếu kho đầy")]
    [SerializeField] PickupItem2D pickupPrefab;
    [SerializeField] Vector2 dropImpulse = new Vector2(0.5f, 1.2f);

    void Reset()
    {
        fishingArea = GetComponent<Collider2D>();
        if (fishingArea) fishingArea.isTrigger = true;
    }

    void Awake()
    {
        if (!fishingArea) fishingArea = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        if (!AllSpots.Contains(this)) AllSpots.Add(this);
    }

    void OnDisable()
    {
        AllSpots.Remove(this);
    }

    public static bool TryGetSpot(Vector2 worldPoint, out FishingSpot spot)
    {
        for (int i = 0; i < AllSpots.Count; i++)
        {
            var candidate = AllSpots[i];
            if (!candidate || !candidate.isActiveAndEnabled) continue;
            if (candidate.ContainsPoint(worldPoint))
            {
                spot = candidate;
                return true;
            }
        }

        spot = null;
        return false;
    }

    public static bool TryGetSpot(Vector2 worldPoint, float searchRadius, out FishingSpot spot, out Vector2 resolvedPoint)
    {
        if (TryGetSpot(worldPoint, out spot))
        {
            resolvedPoint = spot ? spot.ClampCastPoint(worldPoint) : worldPoint;
            return true;
        }

        float bestSqr = searchRadius * searchRadius;
        resolvedPoint = worldPoint;
        spot = null;

        for (int i = 0; i < AllSpots.Count; i++)
        {
            var candidate = AllSpots[i];
            if (!candidate || !candidate.isActiveAndEnabled) continue;

            Vector2 clamped = candidate.ClampCastPoint(worldPoint);
            float sqr = (clamped - worldPoint).sqrMagnitude;
            if (sqr > bestSqr) continue;

            spot = candidate;
            resolvedPoint = clamped;
            bestSqr = sqr;
        }

        return spot != null;
    }

    public bool ContainsPoint(Vector2 point)
    {
        if (!fishingArea) return false;
        if (requirePointInside) return fishingArea.OverlapPoint(point);

        Vector2 closest = fishingArea.ClosestPoint(point);
        return (closest - point).sqrMagnitude <= 0.04f;
    }

    public Vector2 ClampCastPoint(Vector2 requestedPoint)
    {
        if (!fishingArea) return requestedPoint;
        Vector2 clamped = fishingArea.ClosestPoint(requestedPoint);
        if (requirePointInside && !fishingArea.OverlapPoint(clamped))
        {
            // Đẩy nhẹ về phía trong để tránh trả về đúng biên
            Vector2 center = fishingArea.bounds.center;
            Vector2 dir = (clamped - center).sqrMagnitude > 0.0001f ? (clamped - center).normalized : Vector2.up;
            clamped -= dir * 0.05f;
        }
        return clamped;
    }

    public float RandomBiteDelay()
    {
        float a = Mathf.Max(0f, minBiteDelay);
        float b = Mathf.Max(0f, maxBiteDelay);
        if (b < a)
        {
            float tmp = a;
            a = b;
            b = tmp;
        }
        if (Mathf.Approximately(a, b)) return a;
        return Random.Range(a, b);
    }

    public bool TryRollCatch(out ItemSO item, out int count)
    {
        item = null;
        count = 0;

        float totalWeight = 0f;
        for (int i = 0; i < lootTable.Length; i++)
        {
            if (lootTable[i].weight <= 0f) continue;
            totalWeight += lootTable[i].weight;
        }

        if (totalWeight <= 0f) return false;

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < lootTable.Length; i++)
        {
            var entry = lootTable[i];
            if (entry.weight <= 0f) continue;
            roll -= entry.weight;
            if (roll > 0f) continue;

            item = entry.item;
            count = entry.RollCount();
            return item;
        }

        return false;
    }

    public InventoryAddResult GiveCatch(PlayerInventory inventory, ItemSO item, int count, Vector2 dropOrigin)
    {
        var result = new InventoryAddResult
        {
            requested = Mathf.Max(0, count),
            remaining = Mathf.Max(0, count)
        };

        if (!inventory || !item || count <= 0) return result;

        result = inventory.AddItemDetailed(item, count);
        if (result.remaining > 0)
        {
            DropRemainder(item, result.remaining, dropOrigin);
        }
        return result;
    }

    void DropRemainder(ItemSO item, int count, Vector2 origin)
    {
        if (!pickupPrefab || !item || count <= 0) return;

        var pickup = Instantiate(pickupPrefab, origin, Quaternion.identity);
        pickup.Set(item, count);

        var body = pickup.GetComponent<Rigidbody2D>();
        if (!body) return;

        Vector2 impulse = new Vector2(Random.Range(-dropImpulse.x, dropImpulse.x), Random.Range(0.1f, dropImpulse.y));
        body.AddForce(impulse, ForceMode2D.Impulse);
    }
}
