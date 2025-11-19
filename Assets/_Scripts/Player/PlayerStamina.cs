

// PlayerStamina.cs
using UnityEngine;
using UnityEngine.Events;

public class PlayerStamina : MonoBehaviour
{
    [Header("Chỉ số")]
    [SerializeField] float baseMax = 100f;
    public float max = 100f;
    [SerializeField] float current = 100f;
    [SerializeField] float baseRegenPerGameHour = 6f;

    [Header("Chi phí")]
    public float bowCost = 12f, swordCost = 8f, hoeCost = 4f,
    wateringCost = 2f, axeCost = 6f, pickaxeCost = 6.5f, scytheCost = 3f;

    [Header("Tiêu hao/Hồi phục")]
    public float moveDrainPerSecond = 0f;
    public float regenPerGameHour = 6f;
    public float regenDelay = 3f;
    float regenPerSecond;
    float lastMinutesPerRealSecond = -1f;

    [Header("Thời gian game")]
    [SerializeField] TimeManager timeManager;

    [Header("Kiệt sức")]
    public float faintThreshold = -15f;            // ngất khi ≤ -15
    [HideInInspector] public bool exhaustedSinceLastSleep;

    public UnityEvent<float> OnStamina01;
    float regenTimer;

    void Awake(){
        if (!timeManager) timeManager = FindObjectOfType<TimeManager>(true);

        baseMax = Mathf.Max(1f, max);
        max = baseMax;
        baseRegenPerGameHour = Mathf.Max(0f, regenPerGameHour);
        regenPerGameHour = baseRegenPerGameHour;
        regenPerSecond = ConvertRegenToPerSecond(regenPerGameHour);
        lastMinutesPerRealSecond = GetMinutesPerRealSecond();

        current = Mathf.Clamp(current, -999f, max);
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
    }
    void Update(){
        float minutesPerRS = GetMinutesPerRealSecond();
        if (!Mathf.Approximately(minutesPerRS, lastMinutesPerRealSecond))
        {
            lastMinutesPerRealSecond = minutesPerRS;
            regenPerSecond = ConvertRegenToPerSecond(regenPerGameHour);
        }
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

    public void ApplyMaxBonus(float bonus)
    {
        float oldMax = Mathf.Max(1f, max);
        float newMax = Mathf.Max(1f, baseMax + Mathf.Max(0f, bonus));

        if (Mathf.Approximately(oldMax, newMax)) return;

        float percent = current / oldMax;
        max = newMax;
        current = Mathf.Clamp(percent * max, -999f, max);
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
    }

    public float Restore(float amount)
    {
        if (amount <= 0f) return 0f;
        float before = current;
        current = Mathf.Min(max, current + amount);
        regenTimer = 0f;
        OnStamina01?.Invoke(Mathf.Clamp01(current / max));
        return current - before;
    }

    public void ApplyRegenBonus(float bonus)
    {
        float oldRegen = Mathf.Max(0f, regenPerGameHour);
        float newRegen = Mathf.Max(0f, baseRegenPerGameHour + Mathf.Max(0f, bonus));

        if (Mathf.Approximately(oldRegen, newRegen)) return;

        regenPerGameHour = newRegen;
        regenPerSecond = ConvertRegenToPerSecond(regenPerGameHour);
    }

    float ConvertRegenToPerSecond(float regenPerHour)
    {
        float minutesPerRealSecond = Mathf.Max(0.001f, GetMinutesPerRealSecond());
        float secondsPerGameHour = 60f / minutesPerRealSecond;
        return regenPerHour / Mathf.Max(0.001f, secondsPerGameHour);
    }

    float GetMinutesPerRealSecond() => timeManager ? timeManager.minutesPerRealSecond : 1f;
}
