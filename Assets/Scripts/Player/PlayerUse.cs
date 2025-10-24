
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUse : MonoBehaviour
{
    [SerializeField] Transform arrowMuzzle;            
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerController controller;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] LayerMask enemyMask;
    // [SerializeField] bool bowAimWithMouse = true;
    private float bowFailSafe = 10f; // thời gian khóa tối đa 1 phát
    
    bool bowLocked;
    Vector2 bowFacing;
    Vector2 bowDirLocked;
    private float swordFailSafe = 10f;
    bool swordLocked; float swordTimer;
    Vector2 swordFacing;
    float bowTimer;
    [SerializeField] float defaultHitRadius = 0.35f; // giữ bán kính mặc định
    [SerializeField] float minCooldown = 0.15f;
    Rigidbody2D rb; Vector2 move, lastFacing = Vector2.down; float cd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(); 
        if (!inv) inv = GetComponent<PlayerInventory>();
    }
    void Update(){
    if (cd > 0) cd -= Time.deltaTime;
   
    if (bowLocked){ bowTimer -= Time.deltaTime; if (bowTimer <= 0f) BowEnd(); }
    if (swordLocked){ swordTimer -= Time.deltaTime; if (swordTimer <= 0f) AttackEnd(); }

    // ép hướng cho Animator khi đang bắn/chém
    var face = bowLocked ? bowFacing : (swordLocked ? swordFacing : lastFacing);
    anim?.SetFloat("Horizontal", face.x);
    anim?.SetFloat("Vertical",   face.y);
    anim?.SetFloat("Speed",      (bowLocked || swordLocked) ? 0f : move.sqrMagnitude);
    if (sprite) sprite.flipX = face.x < 0f;
    }


    public void OnMove(InputValue v){
    move = v.Get<Vector2>();
    if (bowLocked || swordLocked) return;
    if (move.sqrMagnitude > 0.0001f)
        lastFacing = (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
            ? new Vector2(Mathf.Sign(move.x), 0) : new Vector2(0, Mathf.Sign(move.y));
    }

    Vector2 Facing4()
    {
        return (Mathf.Abs(lastFacing.x) >= Mathf.Abs(lastFacing.y))
            ? new Vector2(Mathf.Sign(lastFacing.x), 0)
            : new Vector2(0, Mathf.Sign(lastFacing.y));
    }

    Vector2 MouseFacing4()
    {
        if (!Camera.main || Mouse.current == null) return Facing4();
        Vector3 wp = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 v = (Vector2)wp - rb.position;
        if (v.sqrMagnitude < 1e-4f) return Facing4();
        return (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            ? new Vector2(Mathf.Sign(v.x), 0)
            : new Vector2(0, Mathf.Sign(v.y));
    }
    Vector2 Facing4FromDir(Vector2 d){
    if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x >= 0 ? Vector2.right : Vector2.left;
    return d.y >= 0 ? Vector2.up : Vector2.down;
    }
    Vector2 AimDirToMouse(){
    Vector3 mp = Mouse.current.position.ReadValue();
    Vector3 mw = Camera.main.ScreenToWorldPoint(mp);
    mw.z = 0f;
    return ((Vector2)mw - (Vector2)arrowMuzzle.position).normalized;
    }

    public void OnUse(InputValue v){
    if (!v.isPressed || cd>0 || swordLocked || bowLocked) return;
    var it = inv?.CurrentItem; if (it==null || it.category!=ItemCategory.Weapon) return;

   if (it.weaponType == WeaponType.Bow){
    var face = MouseFacing4();          // lấy hướng theo chuột, 4 góc 90°
    lastFacing = face;                   // quay mặt trước
    bowFacing  = face;                   // lưu hướng khóa
    ApplyFacingAndFlip(face);            // cập nhật Animator + flip
    anim?.ResetTrigger("Shoot");
    anim?.SetTrigger("Shoot");
    cd = Mathf.Max(minCooldown, it.cooldown);
    return;
}
        if (it.weaponType == WeaponType.Sword)
        {
            anim?.ResetTrigger("Attack"); anim?.SetTrigger("Attack");
            cd = Mathf.Max(minCooldown, it.cooldown);
            return;
        }
    // Rìu
}

    void ApplyFacingAndFlip(Vector2 face)
    {
        anim?.SetFloat("Horizontal", face.x);     // nếu dùng 1 clip side, thay = Mathf.Abs(face.x)
        anim?.SetFloat("Vertical", face.y);
        anim?.SetFloat("Speed", 0f);
        if (sprite) sprite.flipX = face.x < 0f;   // chỉ lật render, không đổi collider :contentReference[oaicite:0]{index=0}
    }

    public void ShootArrow(){
    var it = inv?.CurrentItem; 
    if (it == null || it.weaponType != WeaponType.Bow) return;

    Vector2 dir = bowLocked ? bowDirLocked : AimDirToMouse();   // <— SỬA
    // KHÔNG cập nhật lại bowFacing theo chuột ở đây

    Vector2 spawn = arrowMuzzle ? (Vector2)arrowMuzzle.position : (Vector2)transform.position;
    var go = Instantiate(it.projectilePrefab, spawn, Quaternion.identity);
    go.transform.right = dir;

    var proj = go.GetComponent<ArrowProjectile>() ?? go.AddComponent<ArrowProjectile>();
    proj.Init(it.Dame, dir, it.projectileSpeed, enemyMask, life: 3f,
              maxDist: it.projectileMaxDistance, hitVFXPrefab: it.projectileHitVFX);

    var sr = go.GetComponent<SpriteRenderer>();
    if (sr && sprite){
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder   = sprite.sortingOrder + (dir.y < 0 ? +1 : -1);
    }
}

    // Animation Events trên clip Attack:
    public void AttackStart(){
        swordTimer = swordFailSafe;
        swordLocked = true; swordFacing = Facing4();
        LockMove(true);
        }
    public void BowStart(){
        bowTimer = bowFailSafe;
        bowLocked = true;
        bowFacing = MouseFacing4();        // chốt 4 hướng cho animator
        bowDirLocked = AimDirToMouse();    // chốt vector bay của mũi tên 1 lần  <— THÊM
        ApplyFacingAndFlip(bowFacing);
        LockMove(true);
    }

    public void AttackHit() // gọi bằng Animation Event
    {
        var it = inv?.CurrentItem; if (it == null) return;

        float dist = it.range > 0f ? it.range : 0.6f;      // lấy từ ItemSO
        float rad = defaultHitRadius;

        Vector2 origin = rb.position;
        Vector2 dir = swordLocked ? swordFacing : Facing4();
        Vector2 center = rb.position + dir * (it.range > 0f ? it.range : 0.6f);
        var hits = Physics2D.OverlapCircleAll(center, defaultHitRadius, enemyMask);
        foreach (var c in hits) c.GetComponentInParent<IDamageable>()?.TakeHit(it.Dame);
    }
    void OnDrawGizmosSelected(){
        if (!Application.isPlaying) return;
        var it = inv?.CurrentItem; if (it == null) return;
        float dist = it.range > 0 ? it.range : 0.5f;
        float rad  = 0.5f;
        Vector2 origin = (Vector2)transform.position;
        Vector2 dir = (Mathf.Abs(lastFacing.x)>=Mathf.Abs(lastFacing.y)) ? new Vector2(Mathf.Sign(lastFacing.x),0) : new Vector2(0,Mathf.Sign(lastFacing.y));
        Vector2 center = origin + dir * dist;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, rad);
    }

    public void AttackEnd()
    {
       // nếu người chơi vừa nhấn ngược chiều khi đang bị khóa → lấy hướng đó
        var pf = controller ? controller.PendingFacing4() : Vector2.zero;
        if (pf != Vector2.zero) lastFacing = pf;
        ApplyFacingAndFlip(lastFacing);   // FLIP NGAY lúc end
        swordLocked = false;
        controller?.ApplyPendingMove();   // hoặc sau LockMove(false) đều được
        LockMove(false);
    
    }

    public void BowEnd(){
        lastFacing = bowFacing;            // giữ hướng vừa aim
        ApplyFacingAndFlip(lastFacing);
        bowLocked = false;
        LockMove(false);
        controller?.ApplyPendingMove();    // áp input sau khi mở khoá
    }

    void LockMove(bool on)
    {
        if (controller)
        {
            controller.canMove = !on;
            controller.SetMoveLock(on);   // cập nhật MoveLocked cho Animator.Speed
        }
        rb.velocity = Vector2.zero;
    }

    

    
}
