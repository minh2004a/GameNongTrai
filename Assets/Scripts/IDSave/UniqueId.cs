using UnityEngine;
/// <summary>
/// Gán ID duy nhất cho mỗi GameObject để lưu/truy xuất trạng thái.
[DisallowMultipleComponent]
public class UniqueId : MonoBehaviour
{
    [SerializeField] string id;
    public string Id => id;

    void Awake(){
        if (string.IsNullOrEmpty(id)) id = System.Guid.NewGuid().ToString();
    }
    #if UNITY_EDITOR
    void OnValidate(){
        if (!gameObject.scene.IsValid()) return; // đang ở Prefab Mode => không sinh ID
        if (string.IsNullOrEmpty(id)) id = System.Guid.NewGuid().ToString();
    }
    #endif

}
