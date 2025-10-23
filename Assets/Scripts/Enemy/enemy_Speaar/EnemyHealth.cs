// EnemyHealth.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    public bool IsDead { get; private set; }
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
    // private string pDie = "Die";         // trigger "Die"
    private string pDeadBool;   // tuỳ chọn: bool giữ state chết

    [Header("Death")]
    public float destroyDelay = 1.0f;             // thêm thời gian trễ sau khi anim xong
    private bool disableAIOnDeath;
    private bool disableCollidersOnDeath;
    private bool kinematicOnDeath ;          // đổi Rigidbody2D sang Kinematic cho gọn

    [Header("Events")]
    private UnityEvent onDamaged;
    private UnityEvent onDied;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animDrv) animDrv = GetComponent<EnemyAnimDriver>();
        if (!ai) ai = GetComponent<EnemyAI>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider2D>();
        hp = Mathf.Max(1, maxHp);
    }

    public void TakeDamage(int dmg, Vector2 from)
    {
        if (IsDead) return;                 // không nhận hit nữa
        hp -= Mathf.Max(0, dmg);
        if (hp <= 0){ Die(); return; }
        animDrv?.TriggerHit(from);
        animDrv?.ApplyKnockback(from);
    }

    public void TakeHit(int dmg)
    {
        var p = FindObjectOfType<PlayerUse>(); // lấy vị trí player làm hướng knockback
        Vector2 from = p ? (Vector2)p.transform.position : (Vector2)transform.position;
        TakeDamage(dmg, from);
    }

    public void Heal(int amount)
    {
        if (hp <= 0) return;
        hp = Mathf.Min(maxHp, hp + Mathf.Max(0, amount));
    }
    public void SetMaxHp(int v){
    int nv = Mathf.Max(1, v);
    // nếu đã khởi tạo rồi thì giữ % máu hiện tại khi đổi data trong Editor
    if (hp > 0 && maxHp > 0){
        float ratio = hp / (float)maxHp;
        maxHp = nv;
        hp = Mathf.Clamp(Mathf.RoundToInt(maxHp * ratio), 1, maxHp);
    }else{
        maxHp = nv;
        hp = maxHp;
    }
}


    public void Kill() { if (hp > 0) Die(); }

    void Die()
    {
        hp = 0;
        if (IsDead) return;
        IsDead = true;
        animDrv?.CancelKnockback();   
        animator.ResetTrigger("Hit");
        animator.SetBool("Dead", true);                    
        animator.CrossFade("Base Layer.Dead", 0.05f, 0, 0f);
        StartCoroutine(WaitDeathAnimThenDestroy());

        if (ai) ai.enabled = false;
        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static; // đứng yên như tường
            rb.simulated = false;                 // tắt xử lý va chạm ngay
        }
        if (colliders != null)                   // hoặc: chỉ tắt collider
            foreach (var c in colliders) if (c) c.enabled = false;
        StartCoroutine(WaitDeathAnimThenDestroy());
        IEnumerator WaitDeathAnimThenDestroy()
        {
            // Chờ tới khi state hiện tại chạy xong (normalizedTime >= 1 và không transition)
            int layer = 0;
            float safety = 3f; // fallback chống kẹt
            float t = 0f;
            while (t < safety)
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                if (!animator.IsInTransition(layer) && info.normalizedTime >= 1f) break; // 1 = hết clip. :contentReference[oaicite:4]{index=4}
                t += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }

}