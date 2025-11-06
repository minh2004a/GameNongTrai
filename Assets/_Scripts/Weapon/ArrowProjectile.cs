using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour {
    int dmg; Vector2 dir; float speed;
    LayerMask damageMask, blockMask;    // NEW
    float maxDistSqr; Vector2 startPos;
    GameObject hitVFX;

    public void Init(int damage, Vector2 direction, float spd,
                     LayerMask enemyMask, LayerMask blockMask,   // CHANGED
                     float life = 2f, float maxDist = 8f, GameObject hitVFXPrefab = null){
        dmg = damage;
        dir = direction.normalized;
        speed = spd;
        damageMask = enemyMask;
        this.blockMask = blockMask;                         // NEW
        startPos = transform.position;
        maxDistSqr = maxDist * maxDist;
        hitVFX = hitVFXPrefab;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Destroy(gameObject, life);
    }

    void Update(){
        float step = speed * Time.deltaTime;

        // Chặn tường/vật cản không-trigger
        var hit = Physics2D.Raycast((Vector2)transform.position, dir, step, blockMask);
        if (hit.collider){
            Impact(hit.point);
            return;
        }

        transform.position += (Vector3)(dir * step);

        if (((Vector2)transform.position - startPos).sqrMagnitude >= maxDistSqr)
            Impact(transform.position);
    }

    void OnTriggerEnter2D(Collider2D other){
        // Gây sát thương cho layer địch (có thể là trigger)
        if (((1 << other.gameObject.layer) & damageMask) == 0) return;
        Vector2 p = other.ClosestPoint(transform.position);
        other.GetComponentInParent<IDamageable>()?.TakeHit(dmg);
        Impact(p);
    }

    void Impact(Vector2 pos){
        if (hitVFX){
            var rot = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0));
            Instantiate(hitVFX, pos, rot);
        }
        Destroy(gameObject);
    }
}
