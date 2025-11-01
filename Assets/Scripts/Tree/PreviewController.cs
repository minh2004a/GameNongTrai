// PlantPreviewController.cs
using UnityEngine;
using UnityEngine.EventSystems;
// Hiển thị preview vị trí trồng cây dựa trên item hạt giống đang chọn
public class PlantPreviewController : MonoBehaviour
{
    public Sprite okSprite;          // Extras_1
    public Sprite blockedSprite;     // Extras_0
    public float rangeTiles = 1f;

    public PlayerInventory inv;
    public PlantSystem plantSystem;
    public Transform player;
    
    Camera cam;
    SpriteRenderer sr;

    void Awake(){
        cam = Camera.main;
        sr  = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        sr.color = new Color(1,1,1,0.8f);
    }

    void Update(){
        var it = inv ? inv.CurrentItem : null;
        var seed = it ? it.seedData : null;
        if (!seed || EventSystem.current && EventSystem.current.IsPointerOverGameObject()){
            sr.enabled = false; return;
        }

        Vector3 wp3 = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 wp  = new Vector2(wp3.x, wp3.y);

        bool ok = plantSystem.CanPlantAt(
        wp, player.position, rangeTiles,   // <-- không nhân gridSize ở đây
        seed, out var snapped, out var blocked, out var tooFar);
        transform.position = snapped;
        sr.sprite  = ok ? okSprite : blockedSprite;
        sr.enabled = true;
        
    }
}
