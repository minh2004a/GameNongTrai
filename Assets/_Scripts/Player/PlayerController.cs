﻿using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float exhaustedSpeedMult = 0.5f;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
    public bool canMove = true;
    [SerializeField] PlayerStamina stamina; 
    float currentSpeed; // magnitude of velocity
    Rigidbody2D rb;
    Vector2 moveInput;
    Vector2 lastFacing = Vector2.right;
    Vector2 appliedAnimFacing = Vector2.right;
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
        // 1) Lưu hướng cuối cùng khi có input (ưu tiên trục ngang/dọc)
        if (moveInput.sqrMagnitude > 0.0001f)
            lastFacing = TopDownAnimatorUtility.UpdateFacing(moveInput, lastFacing);

        // 2) Nuôi tham số Speed của Animator bằng tốc độ thật
        TopDownAnimatorUtility.ApplySpeed(anim, currentSpeed);

        // 3) Chỉ ghi hướng khi không bị lock và có input
        if (!MoveLocked && moveInput.sqrMagnitude > 0.0001f)
        {
            if (appliedAnimFacing != lastFacing)
            {
                TopDownAnimatorUtility.ApplyFacing(anim, sprite, lastFacing);
                appliedAnimFacing = lastFacing;
            }
        }
    }
    void FixedUpdate()
    {
        float eff = (stamina && stamina.IsExhausted) ? exhaustedSpeedMult : 1f;
        Vector2 dir = moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : Vector2.zero;
        rb.velocity = (canMove && !MoveLocked) ? dir * (moveSpeed * eff) : Vector2.zero;

        currentSpeed = rb.velocity.magnitude; // nuôi speedWorld cho Update
    }
    // helper
    public Vector2 PendingFacing4()
    {
        var v = pendingMoveInput;
        if (v.sqrMagnitude <= 0.0001f) return Vector2.zero;
        return TopDownAnimatorUtility.SnapToCardinal(v);
    }
    void ApplyFacing(Vector2 f)
    {
        TopDownAnimatorUtility.ApplyFacing(anim, sprite, f);
        appliedAnimFacing = f;
    }

    public void ApplyPendingMove()
    {
        if (pendingMoveInput.sqrMagnitude > 1e-4f)
        {
            moveInput = pendingMoveInput;
            lastFacing = TopDownAnimatorUtility.UpdateFacing(moveInput, lastFacing);
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
        lastFacing = TopDownAnimatorUtility.UpdateFacing(moveInput, lastFacing);
    }

    public void ForceFace(Vector2 dir)
    {
        if (dir.sqrMagnitude < 1e-4f) return;
        // snap 4 hướng (đổi sang 8 nếu cần)
        Vector2 f = TopDownAnimatorUtility.SnapToCardinal(dir);

        lastFacing = f;
        TopDownAnimatorUtility.ApplyFacing(anim, sprite, f);
        appliedAnimFacing = f;
    }

}
