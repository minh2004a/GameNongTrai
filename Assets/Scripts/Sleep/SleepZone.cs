using UnityEngine;

[RequireComponent(typeof(Collider2D))]
/// <summary>
/// Vùng giường ngủ, hiển thị panel xác nhận ngủ khi nhân vật vào.
public class SleepZone : MonoBehaviour
{
    [SerializeField] SleepPanel panel;         // cho phép để trống
    [SerializeField] LayerMask playerMask;     // layer của Player
    [SerializeField] Collider2D trigger;       // chính collider của vùng giường

    SleepManager sleep;
    bool suppressUntilExit;

    void Awake(){
        sleep = FindObjectOfType<SleepManager>(true);
        if (!trigger) trigger = GetComponent<Collider2D>();
        if (!panel) panel = FindObjectOfType<SleepPanel>(true);    // tìm cross-scene, gồm cả inactive
    }

    void Start(){
        // Nếu spawn đang đứng trong vùng -> chặn đến khi ra ngoài
        if (!trigger) return;
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerMask, useTriggers = true };
        var hits = new Collider2D[2];
        if (trigger.OverlapCollider(filter, hits) > 0) suppressUntilExit = true;
    }

    void OnTriggerEnter2D(Collider2D other){
        if (!other.CompareTag("Player")) return;

        if (sleep && sleep.suppressBedPromptOnce){
            sleep.suppressBedPromptOnce = false;   // chặn 1 lần (spawn ở giường, hoặc ngất về)
            suppressUntilExit = true;
            return;
        }
        if (suppressUntilExit) return;

        panel?.Show(true);
    }

    void OnTriggerExit2D(Collider2D other){
        if (!other.CompareTag("Player")) return;
        suppressUntilExit = false;
        panel?.Show(false);
    }
}
