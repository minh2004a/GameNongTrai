using UnityEngine;
// Dữ liệu ScriptableObject cho một loại vật phẩm trong trò chơi
public enum ItemCategory { Tool, Weapon, Resource, Consumable, Minerals } // thêm loại
public enum ToolType { None, Axe, Hoe, Sickle, Shovel, WateringCan }
public enum WeaponType { None, Sword, Bow }
public interface IToolTarget
{
    void Hit(ToolType tool, int power, Vector2 hitDir);
}
public interface IDamageable { void TakeHit(int dmg); }
[CreateAssetMenu(menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public Sprite icon;
    public SeedSO seedData; // dùng khi type == Seed
    public ItemCategory category;
    public ToolType toolType;
    public bool stackable = true; 
    public WeaponType weaponType;  
    public int maxStack = 999;   
    [Header("Hitbox Tuning")]
    [Tooltip("Nhân với range để phóng to/thu nhỏ hitbox")]
    public float hitboxScale = 1f;

    [Tooltip("Dịch hitbox lên/xuống theo trục Y thế giới (+Y là lên)")]
    public float hitboxYOffset = 0f;

    [Tooltip("Khoảng cách từ người chơi tới TÂM hitbox; -1 = dùng mặc định của vũ khí/công cụ")]
    public float hitboxForward = -1f;   // NEW
           // dùng khi category = Tool
           // dùng khi category = Weapon
    public int Dame = 1;
    public float range = 1f;
    // public float radius = 0.35f;
    public float cooldown = 0.2f;

    // cho Bow (để null với vũ khí/công cụ khác)
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    public float projectileMaxDistance = 8f;   // tầm bắn
    public GameObject projectileHitVFX;        // prefab hiệu ứng trúng
}
