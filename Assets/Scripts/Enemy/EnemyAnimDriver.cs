// EnemyAnimDriver.cs
using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimDriver : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] SpriteRenderer sr;
    bool attacking;

    [Header("Params")]
    public string pSpeed = "Speed";
    public string pMoveX = "MoveX";
    public string pMoveY = "MoveY";
    public string pIsMoving = "IsMoving";
    [SerializeField] string pAttack = "Attack";

    Vector2 prevPos;
    

    void Awake(){
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(); // lấy Animator ở child
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable(){ prevPos = rb.position; }

    public void SetAttacking(bool v) { attacking = v; }

    void FixedUpdate(){
        if (attacking) return;
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
    public void TriggerAttack()
    {
        if (animator) animator.SetTrigger(pAttack); // gọi trigger Attack
    }
}
