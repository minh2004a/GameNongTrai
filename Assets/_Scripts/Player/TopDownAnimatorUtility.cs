using UnityEngine;

/// <summary>
/// Helper utilities for working with 2D top-down character animations.
/// Centralises animator parameter hashes and common math so we do not
/// duplicate logic across movement, tool usage or harvesting scripts.
/// </summary>
public static class TopDownAnimatorUtility
{
    public static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    public static readonly int VerticalHash = Animator.StringToHash("Vertical");
    public static readonly int SpeedHash = Animator.StringToHash("Speed");

    const float kEpsilon = 0.0001f;

    /// <summary>
    /// Snap an arbitrary direction to the four cardinal axes used by the
    /// character animations. Returns <see cref="Vector2.zero"/> when the
    /// input is almost zero.
    /// </summary>
    public static Vector2 SnapToCardinal(Vector2 vector)
    {
        if (vector.sqrMagnitude <= kEpsilon)
            return Vector2.zero;

        return Mathf.Abs(vector.x) >= Mathf.Abs(vector.y)
            ? new Vector2(Mathf.Sign(vector.x), 0f)
            : new Vector2(0f, Mathf.Sign(vector.y));
    }

    /// <summary>
    /// Updates the facing direction, preserving the previous facing if the
    /// supplied input is almost zero. This mirrors the behaviour expected in
    /// classic top-down games where the avatar keeps looking in the last
    /// valid direction.
    /// </summary>
    public static Vector2 UpdateFacing(Vector2 input, Vector2 previousFacing)
    {
        var snapped = SnapToCardinal(input);
        return snapped.sqrMagnitude > kEpsilon ? snapped : previousFacing;
    }

    /// <summary>
    /// Apply the facing direction to an animator using cached parameter
    /// hashes. Optionally flips the supplied sprite renderer.
    /// </summary>
    public static void ApplyFacing(Animator animator, SpriteRenderer sprite, Vector2 facing, bool flipSprite = true)
    {
        if (!animator)
            return;

        animator.SetFloat(HorizontalHash, facing.x);
        animator.SetFloat(VerticalHash, facing.y);

        if (flipSprite && sprite)
            sprite.flipX = facing.x < 0f;
    }

    /// <summary>
    /// Apply the facing direction without touching any sprite renderer.
    /// </summary>
    public static void ApplyFacing(Animator animator, Vector2 facing)
    {
        ApplyFacing(animator, null, facing, false);
    }

    /// <summary>
    /// Applies the world-space movement speed to the animator using the
    /// cached hash, avoiding repeated string lookups per frame.
    /// </summary>
    public static void ApplySpeed(Animator animator, float speed)
    {
        if (animator)
            animator.SetFloat(SpeedHash, speed);
    }
}
