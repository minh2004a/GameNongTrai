using UnityEngine;
using UnityEngine.InputSystem;

public interface IDamageable { void TakeHit(int dmg); }
public interface IChoppable { void Chop(int power); }

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerUseItem : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask harvestMask;
    [SerializeField] float overlapRadius = 0.35f; // vùng công cụ
    [SerializeField] float moveSpeed = 4f;

    public int swordDamage = 1;       // gán trong Inspector hoặc lấy từ WeaponSO
    public float attackRange = 1.0f;  // tầm chém

    Rigidbody2D rb;
    Vector2 moveInput, lastFacing = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (!inv) inv = GetComponent<PlayerInventory>();
    }

    public void OnMove(InputValue v)
    {
        moveInput = v.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            lastFacing = (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
                ? new Vector2(Mathf.Sign(moveInput.x), 0f)
                : new Vector2(0f, Mathf.Sign(moveInput.y));
        }
    }

    //public void OnUse(UnityEngine.InputSystem.InputValue v)
    //{
    //    if (!v.isPressed) return;

    //    Vector2 dir = (moveInput.sqrMagnitude > 0.001f) ? moveInput.normalized : lastFacing;
    //    Vector2 origin = rb.position + dir * 0.15f;           // đẩy tia ra khỏi người chơi

    //    // Bắn một tia, trúng collider gần nhất theo enemyMask
    //    var hit = Physics2D.Raycast(origin, dir, attackRange, enemyMask);
    //    Debug.DrawRay(origin, dir * attackRange, Color.red, 0.2f);

    //    if (!hit.collider) { Debug.Log("Use: no hit"); return; }

    //    // Collider ở child → lấy component trên parent/root
    //    var target = hit.collider.GetComponentInParent<IDamageable>();
    //    if (target != null)
    //        target.TakeHit(swordDamage);
    //    else
    //        Debug.LogWarning($"Hit {hit.collider.name} nhưng không có IDamageable");
    //}


    void FixedUpdate() { rb.velocity = moveInput.normalized * moveSpeed; }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Vector2 origin = rb.position;
        Vector2 dir = (moveInput.sqrMagnitude > 0.0001f) ? moveInput.normalized : lastFacing;
        Gizmos.DrawLine(origin, origin + dir * 1.0f);
        Gizmos.DrawWireSphere(origin + dir * 0.6f, overlapRadius);
    }
}
