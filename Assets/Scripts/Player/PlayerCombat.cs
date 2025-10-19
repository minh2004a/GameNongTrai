
﻿﻿// PlayerCombat.cs (tối giản cho kiếm)
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] Transform bowOrigin;                // drag BowOrigin vào đây
    [SerializeField] Vector2 offsetSide = new(0.25f, 0.08f); // phải/trái
    [SerializeField] Vector2 offsetUp   = new(0.00f, 0.30f); // bắn lên
    [SerializeField] Vector2 offsetDown = new(0.00f,-0.05f); // bắn xuống
    [SerializeField] PlayerInventory inv;
    [SerializeField] PlayerController controller;
    [SerializeField] Animator anim;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] LayerMask enemyMask;
    // [SerializeField] bool bowAimWithMouse = true;
    [SerializeField] float bowFailSafe = 0.6f; // thời gian khóa tối đa 1 phát
    bool bowLocked;
    Vector2 bowFacing;
    [SerializeField] float swordFailSafe = 0.5f;
    bool swordLocked; float swordTimer;
    Vector2 swordFacing;
    float bowTimer;
    [SerializeField] float defaultHitRadius = 0.35f; // giữ bán kính mặc định
    [SerializeField] float minCooldown = 0.15f;

    Rigidbody2D rb; Vector2 move, lastFacing = Vector2.down; float cd;

    void Awake() { rb = GetComponent<Rigidbody2D>(); if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(); }
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
    public void OnUse(InputValue v){
    if (!v.isPressed || cd>0 || swordLocked || bowLocked) return;
    var it = inv?.CurrentItem; if (it==null || it.category!=ItemCategory.Weapon) return;

    if (it.weaponType == WeaponType.Bow){
        anim?.ResetTrigger("Shoot"); anim?.SetTrigger("Shoot");
        cd = Mathf.Max(minCooldown, it.cooldown);
        return;
    }
    if (it.weaponType == WeaponType.Sword){
        anim?.ResetTrigger("Attack"); anim?.SetTrigger("Attack");
        cd = Mathf.Max(minCooldown, it.cooldown);
        return;
    }
}

    void ApplyFacingAndFlip(Vector2 face)
    {
        anim?.SetFloat("Horizontal", face.x);     // nếu dùng 1 clip side, thay = Mathf.Abs(face.x)
        anim?.SetFloat("Vertical", face.y);
        anim?.SetFloat("Speed", 0f);
        if (sprite) sprite.flipX = face.x < 0f;   // chỉ lật render, không đổi collider :contentReference[oaicite:0]{index=0}
    }

    public void ShootArrow()
{
    var it = inv?.CurrentItem; if (it == null || it.weaponType != WeaponType.Bow) return;
    Vector2 dir = bowFacing;                  // dùng hướng đã CHỐT
    Vector2 spawn = BowSpawnPos(dir);         // nếu có hàm offset điểm bắn
        var go = Instantiate(it.projectilePrefab, spawn, Quaternion.identity);
        go.transform.right = dir;
        var proj = go.GetComponent<ArrowProjectile>() ?? go.AddComponent<ArrowProjectile>();
        proj.Init(it.power, dir, it.projectileSpeed, enemyMask, life: 3f, 
        maxDist: it.projectileMaxDistance, hitVFXPrefab: it.projectileHitVFX);
        go.transform.right = dir;
    // tùy chọn: đồng bộ sorting
    var sr = go.GetComponent<SpriteRenderer>();
    if (sr && sprite){
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder   = sprite.sortingOrder + (dir.y < 0 ? +1 : -1);
    }
}

Vector2 BowSpawnPos(Vector2 dir)
{
    Vector2 basePos = bowOrigin ? (Vector2)bowOrigin.position : rb.position;
    Vector2 face = (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        ? new Vector2(Mathf.Sign(dir.x), 0)
        : new Vector2(0, Mathf.Sign(dir.y));

    if (face.x != 0) return basePos + new Vector2(Mathf.Sign(face.x) * Mathf.Abs(offsetSide.x), offsetSide.y);
    if (face.y > 0)  return basePos + offsetUp;
    return offsetDown + basePos;
}

    // Animation Events trên clip Attack:
    public void AttackStart(){
        swordTimer = swordFailSafe;
        swordLocked = true; swordFacing = Facing4();
        LockMove(true);
        }
    public void BowStart(){
        bowTimer = bowFailSafe;
        bowLocked = true; bowFacing = Facing4();
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
        foreach (var c in hits) c.GetComponentInParent<IDamageable>()?.TakeHit(it.power);

        Debug.Log($"Hit with {it.name} | range={it.range} | usedDist={dist} | hits={hits.Length}");
    }
    void OnDrawGizmosSelected(){
        if (!Application.isPlaying) return;
        var it = inv?.CurrentItem; if (it == null) return;
        float dist = it.range > 0 ? it.range : 0.6f;
        float rad  = 0.35f;
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

        controller?.ApplyPendingMove();   // khôi phục input đang giữ
        ApplyFacingAndFlip(lastFacing);   // FLIP NGAY lúc end
        swordLocked = false;
            LockMove(false);
    
    }

public void BowEnd(){
        var pf = controller ? controller.PendingFacing4() : Vector2.zero;
        if (pf != Vector2.zero) lastFacing = pf;

        controller?.ApplyPendingMove();
        ApplyFacingAndFlip(lastFacing);
        bowLocked = false;
        LockMove(false);
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
