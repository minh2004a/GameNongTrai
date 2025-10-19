using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
    public bool canMove = true;

    Rigidbody2D rb;
    Vector2 moveInput;
    Vector2 lastFacing = Vector2.right;
    Vector2 pendingMoveInput;

    public bool MoveLocked { get; private set; }
    public void SetMoveLock(bool locked)
    {
        MoveLocked = locked;
        if (locked) rb.velocity = Vector2.zero;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    // helper
    void UpdateFacingFrom(Vector2 v)
    {
        if (v.sqrMagnitude > 0.0001f)
            lastFacing = (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                ? new Vector2(Mathf.Sign(v.x), 0)
                : new Vector2(0, Mathf.Sign(v.y));

    }

    public Vector2 PendingFacing4()
    {
        var v = pendingMoveInput;
        if (v.sqrMagnitude <= 0.0001f) return Vector2.zero;
        return (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            ? new Vector2(Mathf.Sign(v.x), 0)
            : new Vector2(0, Mathf.Sign(v.y));
    }


    public void ApplyPendingMove(){
            if (pendingMoveInput.sqrMagnitude > 0.0001f){
            moveInput = pendingMoveInput;
            UpdateFacingFrom(moveInput);
        }
        pendingMoveInput = Vector2.zero;
    }

    public void OnMove(InputValue v)
    {
       var input = v.Get<Vector2>();
    if (!canMove){ pendingMoveInput = input; return; } // chỉ lưu, không áp
    moveInput = input;
    UpdateFacingFrom(moveInput);
    }


    void Update()
    {
        if (sprite) sprite.flipX = lastFacing.x < 0f;
        if (anim)
        {
            anim.SetFloat("Horizontal", Mathf.Abs(lastFacing.x));
            anim.SetFloat("Vertical",   lastFacing.y);
            anim.SetFloat("Speed",      MoveLocked ? 0f : moveInput.sqrMagnitude);
        }
    }

   void FixedUpdate(){
    rb.velocity = canMove ? moveInput.normalized * moveSpeed : Vector2.zero;
}
}
