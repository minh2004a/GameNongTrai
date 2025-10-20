// EnemyAnimDriver.cs
using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimDriver : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    bool attacking;
    Coroutine knockCo;
   
    Vector2 lastHitDir; bool hitstun;
    public Vector2 LastHitDir => lastHitDir;
    public Vector2 BasePosAtHit { get; set; }
    public void SetHitstun(bool v){ hitstun = v; }
    public void TriggerHit(Vector2 from){
        lastHitDir = ((Vector2)rb.position - from).normalized;
        animator.SetTrigger(pHit); // vào state Hit
    }

    [Header("Params")]
    private string pSpeed = "Speed";
    private string pMoveX = "MoveX";
    private string pMoveY = "MoveY";
    private string pIsMoving = "IsMoving";
    private string pAttack = "Attack";
    private string pHit = "Hit";

    [Header("Knockback")]
    public float knockForce = 6f;
    public float knockStun  = 0.18f;
    public LayerMask solidMask;      // World/Wall/Props
    public float skin = 0.01f;

    // khóa update anim khi đang bị hất
    RaycastHit2D[] _hits = new RaycastHit2D[8];
    ContactFilter2D _filter;
    Vector2 prevPos;
    void Awake(){
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(); // lấy Animator ở child
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        _filter = new ContactFilter2D{ useLayerMask = true, layerMask = solidMask, useTriggers = false };
    }

    void OnEnable(){ prevPos = rb.position; }

    public void SetAttacking(bool v) { attacking = v; }

    void FixedUpdate(){
        if (attacking || hitstun) return;
        Vector2 vel = (rb.position - prevPos) / Time.fixedDeltaTime;
        prevPos = rb.position;
       
        float spd = vel.magnitude;
        // dùng overload có damp để mượt
        animator.SetFloat(pSpeed, spd, 0.1f, Time.deltaTime);
        bool moving = spd > 0.01f;
        animator.SetBool(pIsMoving, moving);
        if (moving){
            Vector2 n = vel.normalized;
            animator.SetFloat(pMoveX, n.x);
            animator.SetFloat(pMoveY, n.y);
        }
        if (sr)
        {
            if (vel.x > 0.01f) sr.flipX = false;
            else if (vel.x < -0.01f) sr.flipX = true;
        }
       
    }

    public void ApplyKnockback(Vector2 fromPos, float? force = null, float? stun = null){
    if (knockCo != null) StopCoroutine(knockCo);
    float f = force ?? knockForce;   // dùng mặc định nếu null
    float s = stun  ?? knockStun;
    knockCo = StartCoroutine(_KnockCo(fromPos, f, s));
}

    public void CancelKnockback(){
    if (knockCo != null){ StopCoroutine(knockCo); knockCo = null; }
    hitstun = false;                 
    if (rb) rb.velocity = Vector2.zero;
    }   
    public bool IsHitstun => hitstun;

    IEnumerator _KnockCo(Vector2 fromPos, float force, float stun)
    {
        hitstun = true;
        Vector2 dir = ((Vector2)rb.position - fromPos).normalized;

        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(dir * force, ForceMode2D.Impulse);   // hất lùi
            float t = 0f; while (t < stun){ t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
        }
        else // Kinematic: tự đẩy thủ công, không xuyên tường
        {
            float t = 0f;
            while (t < stun)
            {
                float step = force * 6f * Time.fixedDeltaTime;
                int n = rb.Cast(dir, _filter, _hits, step + skin);   // dò chướng ngại
                if (n > 0) break;
                rb.MovePosition(rb.position + dir * step);
                t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate();
            }
        }
        hitstun = false;
    }

    public void TriggerAttack()
    {
        if (animator) animator.SetTrigger(pAttack); // gọi trigger Attack
    }
}
