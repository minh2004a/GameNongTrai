// SoftPassThrough2D.cs — gắn cho Player/Enemy body (Dynamic)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class SoftPassThrough2D : MonoBehaviour
{
    [Header("Config")]
    public LayerMask characterMask;     // layer của Player/Enemy
    public float holdTime = 3f;         // phải tì liên tục 3s mới cho xuyên
    public float ghostDuration = 0.8f;  // thời gian cho xuyên

    Collider2D self;
    Rigidbody2D rb;
    readonly Dictionary<Collider2D, float> hold = new();
    readonly HashSet<Collider2D> ghosting = new();

    void Awake()
    {
        self = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        // Khuyên: Sleeping Mode = Never Sleep để OnCollisionStay2D ổn định. 
        // (nếu bị mất sự kiện khi đứng yên). :contentReference[oaicite:1]{index=1}
    }

    void OnCollisionStay2D(Collision2D c)
    {
        // chỉ tính với nhân vật
        if ((characterMask.value & (1 << c.collider.gameObject.layer)) == 0) return;

        // tăng bộ đếm
        hold.TryGetValue(c.collider, out float t);
        t += Time.fixedDeltaTime;               // đếm theo physics tick
        hold[c.collider] = t;

        if (t >= holdTime && !ghosting.Contains(c.collider))
        {
            // cho xuyên cặp này một lúc
            Physics2D.IgnoreCollision(self, c.collider, true);  // bật ignore cặp collider
            StartCoroutine(UnGhost(c.collider));
            ghosting.Add(c.collider);
        }
    }

    IEnumerator UnGhost(Collider2D other)
    {
        yield return new WaitForSeconds(ghostDuration);
        if (self && other) Physics2D.IgnoreCollision(self, other, false);   // tắt ignore
        ghosting.Remove(other);
        hold.Remove(other);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        hold.Remove(c.collider); // rời ra thì reset đếm
    }
}
