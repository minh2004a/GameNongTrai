// EnemyHealth.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("HP")]
   [SerializeField] int maxHp = 5;
   [SerializeField] int hp;

    [Header("Refs")]
    private Animator animator;           // lấy từ child cũng được
    private EnemyAnimDriver animDrv;     // để TriggerHit + Knockback
    private EnemyAI ai;                  // tắt khi chết
    private Rigidbody2D rb;
    private Collider2D[] colliders;      // tắt va chạm lúc chết

    [Header("Animator Params")]
    private string pDie = "Die";         // trigger "Die"
    private string pDeadBool = "Dead";   // tuỳ chọn: bool giữ state chết

    [Header("Death")]
    public float destroyDelay = 1.0f;             // thêm thời gian trễ sau khi anim xong
    private bool disableAIOnDeath = true;
    private bool disableCollidersOnDeath = true;
    private bool kinematicOnDeath = true;          // đổi Rigidbody2D sang Kinematic cho gọn

    [Header("Events")]
    private UnityEvent onDamaged;
    private UnityEvent onDied;

    void Awake(){
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animDrv) animDrv = GetComponent<EnemyAnimDriver>();
        if (!ai) ai = GetComponent<EnemyAI>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider2D>();
        hp = Mathf.Max(1, maxHp);
    }

    public void TakeDamage(int dmg, Vector2 from){
    hp -= Mathf.Max(0, dmg);
    if (hp <= 0) { Die(); return; }   // chết thì dừng ở đây

        // Hiệu ứng bị đánh + hất lùi
        if (animDrv){
        animDrv.TriggerHit(from);
        animDrv.ApplyKnockback(from);
    }

    
    }
    
    public void TakeHit(int dmg)
    {
        var p = FindObjectOfType<PlayerCombat>(); // lấy vị trí player làm hướng knockback
        Vector2 from = p ? (Vector2)p.transform.position : (Vector2)transform.position;
        TakeDamage(dmg, from);
    }

    public void Heal(int amount){
        if (hp <= 0) return;
        hp = Mathf.Min(maxHp, hp + Mathf.Max(0, amount));
    }

    public void Kill() { if (hp > 0) Die(); }

    void Die(){
    hp = 0;

    animDrv?.CancelKnockback();   // cắt hitstun/force
    animator.ResetTrigger("Hit");                // dọn cờ hit
    animator.CrossFade("Base Layer.Dead", 0.05f, 0, 0f); // ép qua Dead ngay
    StartCoroutine(WaitDeathAnimThenDestroy());

    if (ai) ai.enabled = false;
    if (rb){ rb.velocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
    }

    IEnumerator WaitDeathAnimThenDestroy(){
        // Chờ tới khi state hiện tại chạy xong (normalizedTime >= 1 và không transition)
        int layer = 0;
        float safety = 3f; // fallback chống kẹt
        float t = 0f;
        while (t < safety){
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (!animator.IsInTransition(layer) && info.normalizedTime >= 1f) break; // 1 = hết clip. :contentReference[oaicite:4]{index=4}
            t += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}

