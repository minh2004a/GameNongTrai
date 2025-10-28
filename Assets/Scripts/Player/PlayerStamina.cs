// PlayerStamina.cs
using UnityEngine;
using UnityEngine.Events;

public class PlayerStamina : MonoBehaviour
{
    [Header("Chỉ số")]
    public float max = 100f;
    [SerializeField] float current = 100f;

    [Header("Chi phí")]
    public int bowCost = 12;
    public int swordCost = 8;
    public int toolCost = 6;

    [Header("Tiêu hao/Hồi phục")]
    public float moveDrainPerSecond = 0f;
    public float regenPerSecond = 0.1f;
    public float regenDelay = 3f;

    public UnityEvent<float> OnStamina01; // 0..1 cho UI
    float regenTimer;

    void Awake(){
        current = Mathf.Clamp(current, 0f, max);
        OnStamina01?.Invoke(current / max);
    }

    void Update(){
        if (regenTimer > 0f){ regenTimer -= Time.deltaTime; return; }
        if (current < max){
            current = Mathf.Min(max, current + regenPerSecond * Time.deltaTime);
            OnStamina01?.Invoke(current / max);
        }
    }

    public void DrainMove(float dt){
        if (moveDrainPerSecond <= 0f) return;
        current = Mathf.Max(0f, current - moveDrainPerSecond * dt);
        regenTimer = regenDelay;
        OnStamina01?.Invoke(current / max);
    }

    public bool TrySpend(int cost){
        if (current < cost) return false;
        current -= cost;
        regenTimer = regenDelay;
        OnStamina01?.Invoke(current / max);
        return true;
    }

    public float Ratio => max <= 0 ? 0 : current / max;
}
