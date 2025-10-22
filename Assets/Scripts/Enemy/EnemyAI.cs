
// EnemyAI.cs  (chase + leash + return)
using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Refs")]
    private Rigidbody2D rb;
    [SerializeField] Transform player;
    private EnemyAnimDriver animDrv;
    public LayerMask enemyMask;
    private Collider2D col;   // kéo Collider2D của Enemy vào
    [Header("Khu vực tuần tra")]
    public Transform center;
    public float radius = 5f;
    private float edgeBuffer = 0.3f;
    private float edgeSteer;

    [Header("Di chuyển")]
    public float speed = 2f;
    private Vector2 changeDirEvery = new Vector2(0.8f, 1.6f);

    [Header("Wander")]
    public Vector2 waypointTime = new Vector2(1.2f, 5f); // thời gian sẽ "đổi ý" sớm
    public float waypointReach = 0.25f;                   // coi như tới đích
    private float turnAccel = 4f;                          // độ nhạy đổi hướng
    private float wanderJitter = 0.5f;                     // nhiễu đường đi
    private float avoidProbe = 0.8f;                       // tầm dò tránh vật cản
    private float avoidSideOffset = 0.35f;                 // đẩy đầu dò ra trước
    Vector2 waypoint;
    float nextReplan;

    [Header("Phát hiện & Đuổi theo")]
    public float detectRadius = 3f;
    public LayerMask playerMask;
    private bool requireLOS = true;
    public LayerMask obstacleMask;   // tường
    private float chaseSpeedMul = 1.6f;
    private float loseSightTime = 0.6f;

    [Header("Attack")]
    public float attackRange = 0.8f;
    public int attackDamage = 5;
    public float attackCooldown = 0.6f;
    float lastAttackTime = -999f;

    // Thêm biến
    [Header("Leash/Return")]
    public float leashOut = 8f;   // ra quá => Return
    public float leashIn = 7.4f; // vào tới đây mới thôi Return (nhỏ hơn leashOut)
    private float leashCooldown = 0.3f;
    private float returnSpeedMul = 1.2f; // thêm

    [Header("Retreat / Standoff")]
    public float standoffRadius = 1.6f;   // khoảng đứng vờn quanh player
    public float retreatSpeedMul = 1.3f;  // tốc độ khi lùi
    public float retreatMinTime = 0.3f;   // giữ lùi tối thiểu
    public float strafeBlend = 0.25f;     // 0..1: tỉ lệ liếc vòng
    int strafeDir;                        // +1 hoặc -1
    float retreatUntil;                   // Time.time tới khi cho phép vào lại
    float leashLockUntil;
    bool attacking;
    public void SetAttacking(bool v) { attacking = v; }

    enum State { Wander, Chase, Return, Retreat } // + Retreat
    State state = State.Wander;

    Vector2 dir, centerPos, prevPos;
    float tChange, loseTimer;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();
        if (!animDrv) animDrv = GetComponent<EnemyAnimDriver>();
    }

    void Start()
    {
        centerPos = center ? (Vector2)center.position : (Vector2)transform.position;
        dir = Random.insideUnitCircle.normalized;
        tChange = Random.Range(changeDirEvery.x, changeDirEvery.y);
        prevPos = rb.position;
        PickWaypoint();
    }

    void Update()
    {
        switch (state)
        {
            case State.Wander:
                {
                    // đổi sớm theo timer hoặc khi đã chạm waypoint
                    if (Time.time >= nextReplan || 
                        (rb.position - waypoint).sqrMagnitude <= waypointReach * waypointReach)
                        PickWaypoint();

                    if (SeePlayer()) { state = State.Chase; loseTimer = 0f; }
                    break;
                }

            case State.Chase:
                if (Vector2.Distance(rb.position, centerPos) >= leashOut)
                {
                    state = State.Return;
                    leashLockUntil = Time.time + leashCooldown;
                }
                            
               
                if (!SeePlayer()) loseTimer += Time.deltaTime; else loseTimer = 0f;
                if (loseTimer >= loseSightTime) state = State.Return;
                if (state == State.Chase && Time.time >= lastAttackTime + attackCooldown)
                {
                    var hit = Physics2D.OverlapCircle(rb.position, attackRange, playerMask);
                    if (hit && hit.transform == player)
                    {
                        if (animDrv) animDrv.TriggerAttack();
                        var hp = hit.GetComponent<PlayerHealth>();
                        if (hp) hp.TakeDamage(attackDamage);
                        lastAttackTime = Time.time;

                        // → vào RETREAT
                        state = State.Retreat;
                        strafeDir = Random.value < 0.5f ? -1 : 1;
                        retreatUntil = Time.time + retreatMinTime;
                    }
                }
                break;

            case State.Retreat:
                {
                    bool cdReady = Time.time >= lastAttackTime + attackCooldown;
                    float d = player ? Vector2.Distance(rb.position, player.position) : 999f;

                    if (Vector2.Distance(rb.position, centerPos) > leashOut)
                        state = State.Return; // lùi quá dây → quay về
                    else if (player && cdReady && d <= leashOut && SeePlayer() && Time.time >= retreatUntil)
                        state = State.Chase;  // hết CD + còn thấy mục tiêu → vào lại chase
                    break;
                }

            case State.Return:
                float dc = Vector2.Distance(rb.position, centerPos);
                if (dc <= leashIn && Time.time >= leashLockUntil) state = State.Wander;
                else if (dc <= leashOut && SeePlayer() && Time.time >= leashLockUntil) state = State.Chase;
                break;

            
        }
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            var hit = Physics2D.OverlapCircle(rb.position, attackRange, playerMask);
            if (hit && hit.transform == player)        // đúng player trong layer mask
            {
                if (animDrv) animDrv.TriggerAttack();
                var hp = hit.GetComponent<PlayerHealth>();
                if (hp) hp.TakeDamage(attackDamage);
                Debug.Log($"Enemy hit {player.name} for {attackDamage}");
                lastAttackTime = Time.time;
            }
        }
    }

    void FixedUpdate()
    {
        if (attacking || (animDrv && animDrv.IsHitstun)) { rb.velocity = Vector2.zero; return; }
        Vector2 next = rb.position;
        switch (state)
        {
            case State.Wander:
                {
                    // hướng mong muốn: tới waypoint, hơi kéo về tâm khi gần mép
                    Vector2 toWP = waypoint - rb.position;
                    Vector2 desired = toWP;
                    Vector2 toC = centerPos - rb.position;
                    float distC = toC.magnitude;
                    if (distC > radius - edgeBuffer) desired += toC.normalized * 2f;

                    Vector2 desiredDir = desired.sqrMagnitude > 1e-4f ? desired.normalized : dir;

                    // tránh vật cản bằng CircleCast phía trước
                    float colRad = 0.18f;
                    if (col) colRad = Mathf.Max(0.1f, Mathf.Min(col.bounds.extents.x, col.bounds.extents.y));
                    var hit = Physics2D.CircleCast(
                        rb.position + desiredDir * avoidSideOffset,
                        colRad, desiredDir, avoidProbe, obstacleMask);
                    if (hit.collider)
                    {
                        // phản xạ theo normal để quẹo né
                        desiredDir = Vector2.Reflect(desiredDir, hit.normal).normalized;
                    }

                    // thêm chút nhiễu để đường đi "tự nhiên" và đổi hướng mượt
                    desiredDir = (desiredDir + Random.insideUnitCircle * (wanderJitter * Time.fixedDeltaTime)).normalized;
                    dir = Vector2.Lerp(dir, desiredDir, 1f - Mathf.Exp(-turnAccel * Time.fixedDeltaTime));

                    next = rb.position + dir * speed * Time.fixedDeltaTime;
                    break;
                }

            case State.Chase:
                {
                    if (player)
                    {
                        Vector2 d = ((Vector2)player.position - rb.position).normalized;
                        next = rb.position + d * (speed * chaseSpeedMul) * Time.fixedDeltaTime;
                    }
                    break;
                }
            case State.Retreat:
                {
                    if (player)
                    {
                        Vector2 away = (rb.position - (Vector2)player.position).normalized;
                        Vector2 tangent = new Vector2(-away.y, away.x) * strafeDir;
                        float dist = Vector2.Distance(rb.position, player.position);

                        // ép lùi ra đủ xa rồi mới lượn ngang
                        float wAway = dist < standoffRadius ? 0.9f : (1f - strafeBlend);
                        Vector2 d = (away * wAway + tangent * (1f - wAway)).normalized;

                        next = rb.position + d * (speed * retreatSpeedMul) * Time.fixedDeltaTime;
                    }
                    break;
                }
            case State.Return:
                {
                    Vector2 d = (centerPos - rb.position).normalized;
                    next = rb.position + d * (speed * returnSpeedMul) * Time.fixedDeltaTime;
                    break;
                }
        }
        rb.MovePosition(next); // move đúng theo physics tick

    }

    bool SeePlayer()
    {
        if (!player) return false;
        // kiểm tra trong bán kính
        var hit = Physics2D.OverlapCircle(rb.position, detectRadius, playerMask);
        if (!hit || hit.transform != player) return false;

        if (!requireLOS) return true;

        // kiểm tra line of sight với tường
        var hitWall = Physics2D.Linecast(rb.position, player.position, obstacleMask);
        return hitWall.collider == null;
    }

    void PickWaypoint()
    {
        // chọn 1 điểm bất kỳ trong vùng, trừ mép
        float r = Mathf.Max(0f, radius - edgeBuffer);
        waypoint = centerPos + (Random.insideUnitCircle * r);
        nextReplan = Time.time + Random.Range(waypointTime.x, waypointTime.y);
    }
   
    
    void OnDrawGizmosSelected()
    {
        Vector3 c = center ? center.position : transform.position;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(c, radius);      // vùng patrol
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(c, leashOut); // leash
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectRadius); // detect
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange); // vẽ tầm đánh
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(player ? player.position : transform.position, standoffRadius);
    }
}
