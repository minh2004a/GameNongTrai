// ConfinerAutoBinder.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Collections;
using System.Reflection;
[DefaultExecutionOrder(-1000)]
public class ConfinerAutoBinder : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera vcam;
    CinemachineConfiner2D confiner;

    void Awake()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();
        confiner = vcam.GetComponent<CinemachineConfiner2D>();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void Start()
    {
        // bind khi scene đầu tiên đã sẵn sàng
        StartCoroutine(BindNextFrame());
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // đợi 1 frame cho scene mới spawn xong object
        StartCoroutine(BindNextFrame());
    }

    IEnumerator BindNextFrame()
    {
        yield return null;
        BindToSceneBounds();
    }

public void BindToSceneBounds()
{
    var marker = FindObjectOfType<CameraBounds2D>();
    if (marker == null || confiner == null)
    {
        Debug.LogWarning("[ConfinerAutoBinder] Không tìm thấy CameraBounds2D trong scene.");
        return;
    }

    // 1) Set BoundingShape2D (hoặc m_BoundingShape2D)
    var prop = typeof(CinemachineConfiner2D).GetProperty("BoundingShape2D",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (prop != null) prop.SetValue(confiner, marker.Collider);
    else
    {
        var field = typeof(CinemachineConfiner2D).GetField("m_BoundingShape2D",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(confiner, marker.Collider);
    }

    // 2) Gọi InvalidatePathCache() hoặc InvalidateCache()
    var invPath = typeof(CinemachineConfiner2D).GetMethod("InvalidatePathCache",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    var inv = invPath ?? typeof(CinemachineConfiner2D).GetMethod("InvalidateCache",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    inv?.Invoke(confiner, null);
}

}
