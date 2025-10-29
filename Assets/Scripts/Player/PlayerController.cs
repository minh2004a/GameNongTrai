using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
    public bool canMove = true;
    [SerializeField] PlayerStamina stamina; 
    Rigidbody2D rb;
    Vector2 moveInput;
    Vector2 lastFacing = Vector2.right;
    Vector2 pendingMoveInput;
    public bool MoveLocked { get; private set; }
    public Vector2 Facing4 => lastFacing;
    public void SetMoveLock(bool locked)
    {
        MoveLocked = locked;
        canMove = !locked;                // CHỐT: tắt input khi lock
        if (locked)
        {
            rb.velocity = Vector2.zero;
            if (pendingMoveInput.sqrMagnitude <= 1e-4f) pendingMoveInput = moveInput;
            moveInput = Vector2.zero;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    void Update()
    {
        // Speed luôn cập nhật
        if (anim)
        {
            anim.SetFloat("Speed", moveInput.sqrMagnitude);
        }

        // Cách 3: chỉ cập nhật hướng khi KHÔNG lock và đang có input di chuyển
        if (!MoveLocked && moveInput.sqrMagnitude > 0.0001f)
        {
            if (anim)
            {
                anim.SetFloat("Horizontal", lastFacing.x);
                anim.SetFloat("Vertical", lastFacing.y);
            }
            if (sprite)
            {
                sprite.flipX = lastFacing.x < 0f;
            }
        }
    }
    void FixedUpdate()
    {
        rb.velocity = (canMove && !MoveLocked) ? moveInput.normalized * moveSpeed : Vector2.zero;

        if (stamina && canMove && !MoveLocked && rb.velocity.sqrMagnitude > 0.0001f)
            stamina.DrainMove(Time.fixedDeltaTime);
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
    void ApplyFacing(Vector2 f)
    {
        if (anim)
        {
            anim.SetFloat("Horizontal", f.x);
            anim.SetFloat("Vertical", f.y);
        }
        if (sprite) sprite.flipX = f.x < 0f;
    }
    
    public void ApplyPendingMove()
    {
        if (pendingMoveInput.sqrMagnitude > 1e-4f)
        {
            moveInput = pendingMoveInput;
            UpdateFacingFrom(moveInput);
        }
        else
        {
            moveInput = Vector2.zero;      // không còn “hướng cũ”
        }
        pendingMoveInput = Vector2.zero;
    }

    public void OnMove(InputValue v)
    {
        var input = v.Get<Vector2>();
        if (!canMove || MoveLocked)
        {          // giữ hướng, không áp vào rb
            pendingMoveInput = input;
            return;
        }
        moveInput = input;
        UpdateFacingFrom(moveInput);
    }

    public void ForceFace(Vector2 dir)
    {
        if (dir.sqrMagnitude < 1e-4f) return;
        // snap 4 hướng (đổi sang 8 nếu cần)
        Vector2 f = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0)
            : new Vector2(0, Mathf.Sign(dir.y));

        lastFacing = f;
        if (anim)
        {
            anim.SetFloat("Horizontal", f.x);
            anim.SetFloat("Vertical", f.y);
        }
        if (sprite) sprite.flipX = f.x < 0f;
        Debug.Log($"ForceFace -> {f}");
    }

}
