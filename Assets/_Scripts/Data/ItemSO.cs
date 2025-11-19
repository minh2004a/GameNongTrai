
using UnityEngine;
using UnityEngine.Serialization;
// Dữ liệu ScriptableObject cho một loại vật phẩm trong trò chơi
public enum ItemCategory { Tool, Weapon, Resource, Consumable, Minerals, Seed,Equipment } // thêm loại
public enum WeaponType { None, Sword, Bow }
public enum EquipSlotType
{
    None = 0,
    Hat = 1,
    Armor = 2,
    Boots = 3,
    Ring = 4,
    Pants = 5,
    Gloves = 6,
    Backpack = 7,
    Potion = 8
}
public enum ToolType
{
    None = 0,
    Axe = 1,
    Hoe = 2,
    Pickaxe = 3,
    Scythe = 4,
    WateringCan = 5
}
public interface IDamageable { void TakeHit(int dmg); }
public interface IReapable
{
    void Reap(int damage, Vector2 hitDir, PlayerInventory inv);
}

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public Sprite icon;
    public SeedSO seedData; // dùng khi type == Seed
    public ItemCategory category;
    [Header("Equipment Settings")]
    public EquipSlotType equipSlot = EquipSlotType.None;
    public bool stackable = true;
    public WeaponType weaponType;
    public ToolType toolType = ToolType.None;
    [Header("Consumable Settings")]
    [Tooltip("Lượng HP hồi lại khi sử dụng vật phẩm này.")]
    public int healthRestore;
    [Tooltip("Lượng thể lực hồi lại khi sử dụng vật phẩm này.")]
    public float staminaRestore;
    [Tooltip("Giá bán mỗi vật phẩm khi bán.")]
    public int sellPrice;
    [Header("Boots Settings")]
    [Tooltip("Tăng thể lực tối đa khi mang giày.")]
    public float staminaMaxBonus;
    [Tooltip("Tăng tốc độ hồi thể lực khi mang giày (đơn vị thể lực/giây).")]
    public float staminaRegenBonus;
    [Header("Backpack Settings")]
    [FormerlySerializedAs("bagSlotBonus")]
    [Tooltip("Số ô túi được mở thêm khi trang bị balo này.")]
    public int backpackSlotBonus;
    [Header("Tool Settings")]
    [Tooltip("Số ô tối đa tính từ người chơi đến mục tiêu khi dùng công cụ này")]
    [Min(1)] public int toolRangeTiles = 1;
    public int maxStack = 999;
    [Header("Hitbox Tuning")]
    [Tooltip("Nhân với range để phóng to/thu nhỏ hitbox")]
    public float hitboxScale = 1f;

    [Tooltip("Dịch hitbox lên/xuống theo trục Y thế giới (+Y là lên)")]
    public float hitboxYOffset = 0f;

    [Tooltip("Khoảng cách từ người chơi tới TÂM hitbox; -1 = dùng mặc định của vũ khí/công cụ")]
    public float hitboxForward = -1f;   // NEW
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
