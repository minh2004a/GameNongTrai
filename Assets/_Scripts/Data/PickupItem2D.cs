// PickupItem2D.cs
using UnityEngine;
// Quản lý vật phẩm có thể nhặt được trong thế giới 2D
public class PickupItem2D : MonoBehaviour
{
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] ItemSO item;
    [SerializeField] int count = 1;

    [Header("Drop settling")]
    [SerializeField, Range(0f, 1f)] float impactDamping = 0.35f;   // giảm lực bật lại sau va chạm
    [SerializeField] float settleDeceleration = 8f;                // lực ma sát khiến vật dừng lại
    [SerializeField] float stopSpeed = 0.05f;                      // tốc độ coi như đã đứng yên
    [SerializeField] int settleFrames = 12;                         // số frame áp lực ma sát sau khi chạm đất

    Rigidbody2D body;
    int settleTicksRemaining;

    void Reset()
    {
        iconRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (!iconRenderer) iconRenderer = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        settleTicksRemaining = 0;
    }

    public void Set(ItemSO i, int c)
    {
        item = i;
        count = Mathf.Max(1, c);
        if (iconRenderer) iconRenderer.sprite = i ? i.icon : null;
    }

    void FixedUpdate()
    {
        if (!body) return;
        if (settleTicksRemaining <= 0) return;

        settleTicksRemaining--;

        Vector2 v = body.velocity;
        if (v.sqrMagnitude <= stopSpeed * stopSpeed)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }

        float maxDelta = settleDeceleration * Time.fixedDeltaTime;
        body.velocity = Vector2.MoveTowards(v, Vector2.zero, maxDelta);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!body) return;
        if (collision.collider.isTrigger) return;

        settleTicksRemaining = Mathf.Max(settleTicksRemaining, settleFrames);

        if (impactDamping <= 0f || impactDamping >= 1f) return;
        Vector2 v = body.velocity;
        if (v.sqrMagnitude <= 0f) return;
        body.velocity = v * (1f - impactDamping);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var inv = other.GetComponent<PlayerInventory>(); if (!inv) return;

        var result = inv.AddItemDetailed(item, count);
        if (result.remaining <= 0) Destroy(gameObject);
        else count = result.remaining; // kho còn dư chỗ → giữ lại phần chưa nhét được
    }
}