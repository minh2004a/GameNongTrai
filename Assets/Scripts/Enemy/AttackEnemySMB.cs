using UnityEngine;
using System.Collections.Generic;

public class AttackEnemySMB : StateMachineBehaviour
{
    EnemyAI ai; Rigidbody2D rb; Collider2D selfCol;
    RigidbodyType2D savedType;
    readonly List<Collider2D> ignored = new();

    // set trên Inspector của EnemyAI
    LayerMask enemyMask;

    public override void OnStateEnter(Animator a, AnimatorStateInfo s, int l){
        ai ??= a.GetComponentInParent<EnemyAI>();
        rb ??= ai.GetComponent<Rigidbody2D>();
        selfCol ??= ai.GetComponent<Collider2D>();
        enemyMask = ai.enemyMask;

        ai.SetAttacking(true);                   // FixedUpdate của bạn sẽ return sớm
        rb.velocity = Vector2.zero; rb.angularVelocity = 0;
        savedType = rb.bodyType;
        rb.bodyType = RigidbodyType2D.Kinematic; // không bị đẩy khi chém  :contentReference[oaicite:3]{index=3}

        // tùy chọn: cắt va chạm với các Enemy lân cận để khỏi “chen lấn”
        ignored.Clear();
        foreach (var h in Physics2D.OverlapCircleAll(rb.position, 1.2f, enemyMask)){
            if (h != selfCol){ Physics2D.IgnoreCollision(selfCol, h, true); ignored.Add(h); } // :contentReference[oaicite:4]{index=4}
        }
    }

    public override void OnStateExit(Animator a, AnimatorStateInfo s, int l){
        // khôi phục
        foreach (var h in ignored) if (h) Physics2D.IgnoreCollision(selfCol, h, false); // :contentReference[oaicite:5]{index=5}
        ignored.Clear();
        rb.bodyType = savedType;                 // trả Dynamic nếu trước đó là Dynamic  :contentReference[oaicite:6]{index=6}
        ai.SetAttacking(false);
    }
}
