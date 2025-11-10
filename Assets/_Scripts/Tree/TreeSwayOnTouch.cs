using System.Collections;
using UnityEngine;

/// <summary>
/// Khi người chơi đi qua sẽ làm cây lắc sang trái/phải giống Stardew Valley.
/// Đính kèm script này lên cây, thêm Collider2D (nên là trigger) để phát hiện player.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TreeSwayOnTouch : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform sẽ bị xoay. Để trống sẽ dùng chính transform hiện tại.")]
    public Transform swayRoot;

    [Header("Settings")]
    public string playerTag = "Player";
    [Tooltip("Độ nghiêng tối đa (độ).")]
    public float maxAngle = 6f;
    [Tooltip("Thời gian hoàn thành một lần lắc (giây).")]
    public float swayDuration = 0.6f;
    [Tooltip("Khoảng thời gian phải chờ trước khi có thể lắc lần nữa.")]
    public float cooldown = 0.2f;

    Transform Target => swayRoot ? swayRoot : transform;

    Coroutine swayCo;
    float lastSwayTime;
    Quaternion baseRotation;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        if (!swayRoot) swayRoot = transform;
        baseRotation = Target ? Target.localRotation : Quaternion.identity;
    }

    void OnEnable()
    {
        baseRotation = Target ? Target.localRotation : Quaternion.identity;
        ApplyAngle(0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Vector2 dir = (other.transform.position - Target.position);
        TriggerSway(Mathf.Sign(dir.x));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag(playerTag)) return;
        Vector2 dir = (collision.transform.position - Target.position);
        TriggerSway(Mathf.Sign(dir.x));
    }

    void TriggerSway(float horizontalSign)
    {
        float time = Time.time;
        if (time - lastSwayTime < cooldown) return;
        lastSwayTime = time;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ApplyAngle(0f);
            return;
        }

        float dir = Mathf.Approximately(horizontalSign, 0f) ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(horizontalSign);

        if (swayCo != null) StopCoroutine(swayCo);
        swayCo = StartCoroutine(SwayRoutine(dir));
    }

    IEnumerator SwayRoutine(float direction)
    {
        float dur = Mathf.Max(0.01f, swayDuration);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float angle = Mathf.Sin(t * Mathf.PI) * maxAngle * direction;
            ApplyAngle(angle);
            yield return null;
        }

        ApplyAngle(0f);
        swayCo = null;
    }

    void ApplyAngle(float zAngle)
    {
        if (!Target) return;
        Target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, zAngle);
    }

    void OnDisable()
    {
        if (swayCo != null) StopCoroutine(swayCo);
        swayCo = null;
        ApplyAngle(0f);
    }
}
