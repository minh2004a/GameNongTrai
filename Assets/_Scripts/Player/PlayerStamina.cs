
// PlayerStamina.cs
using UnityEngine;
using UnityEngine.Events;

public class PlayerStamina : MonoBehaviour
{
    [Header("Chỉ số")]
    public float max = 100f;
    [SerializeField] float current = 100f;

    [Header("Chi phí")]
    public float bowCost = 12f, swordCost = 8f, hoeCost = 4f;

    [Header("Tiêu hao/Hồi phục")]
    public float moveDrainPerSecond = 0f;
    public float regenPerSecond = 0.1f;
    public float regenDelay = 3f;

    [Header("Kiệt sức")]
    public float faintThreshold = -15f;            // ngất khi ≤ -15
    [HideInInspector] public bool exhaustedSinceLastSleep;

    public UnityEvent<float> OnStamina01;
    float regenTimer;

    void Awake(){
        current = Mathf.Clamp(current, -999f, max);
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
    }
    void Update(){
        if (regenTimer > 0f){ regenTimer -= Time.deltaTime; return; }
        if (current < max){
            current = Mathf.Min(max, current + regenPerSecond * Time.deltaTime);
            OnStamina01?.Invoke(Mathf.Clamp01(current / max));      // clamp 0..1
        }
    }
    public void DrainMove(float dt){
        if (moveDrainPerSecond <= 0f) return;
        current -= moveDrainPerSecond * dt;
        regenTimer = regenDelay;
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
    }

    // Chi tiêu cũ: vẫn giữ nguyên nếu nơi khác cần chặn âm
    public bool TrySpend(float cost){
        if (current < cost) return false;
        current -= cost;
        regenTimer = regenDelay;
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
        return true;
    }

    // Chi tiêu cho cơ chế mới: cho phép âm tới ngưỡng ngất
    public enum SpendResult { Spent, Exhausted, Fainted }
    public SpendResult SpendExhaustible(float cost){
        current -= cost;
        regenTimer = regenDelay;

        if (current <= faintThreshold){
            exhaustedSinceLastSleep = true;
            OnStamina01?.Invoke(Mathf.Clamp01(current / max));
            return SpendResult.Fainted;
        }
        if (current <= 0f){
            exhaustedSinceLastSleep = true;
            OnStamina01?.Invoke(Mathf.Clamp01(current / max));
            return SpendResult.Exhausted;
        }
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
        return SpendResult.Spent;
    }

    public float Ratio => max <= 0 ? 0 : Mathf.Clamp01(current / max);
    public bool IsFainted   => current <= faintThreshold;
    public bool IsExhausted => current <= 0f;
    public void SetPercent(float p){ p = Mathf.Clamp01(p); current = max * p; OnStamina01?.Invoke(p); }
    public void RecoverMissingPercent(float p){ p = Mathf.Clamp01(p); current = Mathf.Min(max, current + (max - current)*p); OnStamina01?.Invoke(Mathf.Clamp01(current/max)); }
    public void ClearExhaustionFlag() => exhaustedSinceLastSleep = false;
}
