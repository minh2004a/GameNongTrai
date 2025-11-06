using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string id;
#if UNITY_EDITOR
    void OnDrawGizmos(){
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
#endif
}
