// ItemMagnet.cs (gắn lên Player)
using UnityEngine;
// Hút các PickupItem2D gần đó về phía người chơi
public class ItemMagnet : MonoBehaviour
{
    [SerializeField] float radius = 2.5f;
    [SerializeField] float pullForce = 25f;   // lực hút
    [SerializeField] float maxSpeed = 8f;     // tốc độ tối đa của pickup
    [SerializeField] LayerMask pickupMask;    // layer của PickupItem2D
    PlayerInventory inv;
    readonly Collider2D[] hits = new Collider2D[32];
    
    void Awake(){
        inv = GetComponent<PlayerInventory>() ?? GetComponentInParent<PlayerInventory>();
    }
    void FixedUpdate()
    {
        int n = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hits, pickupMask);
        for (int i = 0; i < n; i++)
        {
            var rb = hits[i]?.attachedRigidbody;
            if (!rb) continue;

            Vector2 toPlayer = (Vector2)transform.position - rb.position;
            float dist = toPlayer.magnitude + 0.001f;
            // hút mạnh hơn khi gần
            Vector2 force = toPlayer.normalized * (pullForce / dist);
            rb.AddForce(force, ForceMode2D.Force);

            // giới hạn tốc độ bay của pickup
            if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
