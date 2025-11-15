using System.Collections;
using UnityEngine;

/// <summary>
/// Tạo hiệu ứng phóng vật phẩm rơi kiểu Stardew Valley.
/// Có thể dùng chung cho mọi loại ItemSO vì sau khi rơi xong
/// sẽ trả object về prefab PickupItem2D ban đầu.
/// </summary>
/// <remarks>
/// <para><b>Cách sử dụng nhanh</b></para>
/// <list type="number">
/// <item>
/// <description>Tạo một GameObject trống trong scene (ví dụ "LootThrower"), gắn component <see cref="LootThrower2D"/>.</description>
/// </item>
/// <item>
/// <description>Gán prefab <see cref="PickupItem2D"/> mặc định vào trường <c>Default Pickup Prefab</c> của component.</description>
/// </item>
/// <item>
/// <description>(Tuỳ chọn) Điều chỉnh các tham số tốc độ, trọng lực, thời gian giữ yên để hợp với game của bạn.</description>
/// </item>
/// <item>
/// <description>Kéo thả tham chiếu <c>LootThrower2D</c> đó vào trường <c>Loot Thrower</c> của các script sinh đồ rơi, ví dụ <see cref="PlantGrowth"/>.</description>
/// </item>
/// <item>
/// <description>Khi muốn quăng vật phẩm bằng code, gọi <c>lootThrower.Throw(item, count)</c> hoặc truyền thêm vị trí/prefab tuỳ ý.</description>
/// </item>
/// </list>
/// </remarks>
public class LootThrower2D : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] PickupItem2D defaultPickupPrefab;

    [Header("Launch velocity")]
    [SerializeField] Vector2 launchVelocityXRange = new(-2f, 2f);
    [SerializeField] Vector2 launchVelocityYRange = new(3f, 5f);
    [SerializeField] float randomAngularVelocity = 180f;

    [Header("Physics during flight")]
    [SerializeField] float launchGravityScale = 1f;
    [SerializeField] PhysicsMaterial2D launchMaterial;

    [Header("Settle detection")]
    [SerializeField] float settleSpeedThreshold = 0.15f;
    [SerializeField] float settleHoldDuration = 0.25f;

    /// <summary>
    /// Quăng vật phẩm tại vị trí của GameObject chứa component này.
    /// </summary>
    public void Throw(ItemSO item, int count)
    {
        Throw(item, count, transform.position, null);
    }

    /// <summary>
    /// Quăng vật phẩm tại vị trí chỉ định. Có thể truyền prefab override nếu muốn.
    /// </summary>
    public void Throw(ItemSO item, int count, Vector3 position, PickupItem2D prefabOverride = null)
    {
        if (!item || count <= 0)
        {
            Debug.LogWarning("LootThrower2D.Throw: Thiếu dữ liệu item hoặc số lượng không hợp lệ.");
            return;
        }

        var prefab = prefabOverride ? prefabOverride : defaultPickupPrefab;
        if (!prefab)
        {
            Debug.LogWarning("LootThrower2D.Throw: Chưa gán prefab PickupItem2D.");
            return;
        }

        var pickup = Instantiate(prefab, position, Quaternion.identity);
        pickup.Set(item, count);

        PrepareLaunch(pickup);
    }

    void PrepareLaunch(PickupItem2D pickup)
    {
        if (!pickup) return;

        var body = pickup.GetComponent<Rigidbody2D>();
        if (!body) body = pickup.gameObject.AddComponent<Rigidbody2D>();

        var collider = pickup.GetComponent<Collider2D>();
        if (!collider) collider = pickup.gameObject.AddComponent<CircleCollider2D>();

        var state = new LaunchState
        {
            body = body,
            collider = collider,
            originalBodyType = body.bodyType,
            originalSimulated = body.simulated,
            originalGravityScale = body.gravityScale,
            originalMaterial = collider ? collider.sharedMaterial : null,
            originalIsTrigger = collider && collider.isTrigger
        };

        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.gravityScale = Mathf.Max(0.01f, launchGravityScale);
        body.velocity = new Vector2(Random.Range(launchVelocityXRange.x, launchVelocityXRange.y),
                                     Random.Range(launchVelocityYRange.x, launchVelocityYRange.y));
        if (Mathf.Abs(randomAngularVelocity) > 0f)
        {
            body.angularVelocity = Random.Range(-randomAngularVelocity, randomAngularVelocity);
        }

        if (collider)
        {
            collider.isTrigger = false;
            if (launchMaterial) collider.sharedMaterial = launchMaterial;
        }

        StartCoroutine(SettleRoutine(state));
    }

    IEnumerator SettleRoutine(LaunchState state)
    {
        float stillTimer = 0f;
        float thresholdSqr = settleSpeedThreshold * settleSpeedThreshold;

        while (state.body)
        {
            if (state.body.velocity.sqrMagnitude <= thresholdSqr)
            {
                stillTimer += Time.deltaTime;
                if (stillTimer >= settleHoldDuration) break;
            }
            else
            {
                stillTimer = 0f;
            }

            yield return null;
        }

        if (!state.body) yield break;

        state.body.velocity = Vector2.zero;
        state.body.angularVelocity = 0f;
        state.body.gravityScale = state.originalGravityScale;
        state.body.bodyType = state.originalBodyType;
        state.body.simulated = state.originalSimulated;

        if (state.collider)
        {
            state.collider.isTrigger = state.originalIsTrigger;
            state.collider.sharedMaterial = state.originalMaterial;
        }
    }

    struct LaunchState
    {
        public Rigidbody2D body;
        public Collider2D collider;
        public RigidbodyType2D originalBodyType;
        public bool originalSimulated;
        public float originalGravityScale;
        public bool originalIsTrigger;
        public PhysicsMaterial2D originalMaterial;
    }
}
