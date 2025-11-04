using Cinemachine;
using UnityEngine;

public class HouseCameraSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera vcamHouse;   // có thể để trống, sẽ tự tìm
    public CinemachineVirtualCamera vcamPlayer;  // kéo từ Persistent
    public string houseCamName = "VCam_House";   // đổi theo tên object
    public string playerTag = "Player";
    public int housePrio = 20;
    public int playerPrio = 10;

    void OnEnable() { TryResolveHouseCam(); }

    void OnTriggerEnter2D(Collider2D other){
        if (!other.CompareTag(playerTag)) return;
        TryResolveHouseCam();
        if (vcamHouse && vcamPlayer){
            vcamHouse.Priority  = housePrio;   // khóa khung trong nhà
            vcamPlayer.Priority = playerPrio;
        }
    }

    void OnTriggerExit2D(Collider2D other){
        if (!other.CompareTag(playerTag)) return;
        if (vcamHouse && vcamPlayer){
            vcamHouse.Priority  = playerPrio;  // trả về camera theo player
            vcamPlayer.Priority = housePrio;
        }
    }

    void TryResolveHouseCam(){
        if (vcamHouse) return;
        var byTag = GameObject.FindWithTag("HouseCam");
        if (byTag) { vcamHouse = byTag.GetComponent<CinemachineVirtualCamera>(); if (vcamHouse) return; }
        var go = GameObject.Find(houseCamName);
        if (go) vcamHouse = go.GetComponent<CinemachineVirtualCamera>();
    }
}
