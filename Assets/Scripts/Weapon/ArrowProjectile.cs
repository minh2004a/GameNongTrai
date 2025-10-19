using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour {
    int dmg; Vector2 dir; float speed; LayerMask mask;
    float maxDistSqr; Vector2 startPos;
    GameObject hitVFX;

    public void Init(int damage, Vector2 direction, float spd, LayerMask enemyMask,
                     float life = 2f, float maxDist = 8f, GameObject hitVFXPrefab = null){
        dmg = damage; dir = direction.normalized; speed = spd; mask = enemyMask;
        startPos = transform.position; maxDistSqr = maxDist * maxDist; hitVFX = hitVFXPrefab;

        var rb = GetComponent<Rigidbody2D>(); rb.isKinematic = true; rb.gravityScale = 0f;
        // Có thể dùng CapsuleCollider2D/CircleCollider2D đặt IsTrigger = true trên prefab
        Destroy(gameObject, life); // dự phòng
    }

    void Update(){
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
        if (((Vector2)transform.position - startPos).sqrMagnitude >= maxDistSqr){
            Impact(transform.position); // hết tầm → hủy
        }
    }

    void OnTriggerEnter2D(Collider2D other){
        if (((1 << other.gameObject.layer) & mask) == 0) return;
        // Lấy điểm va chạm gần nhất trên collider 2D
        Vector2 p = other is Collider2D c ? c.ClosestPoint(transform.position) : (Vector2)transform.position;
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
