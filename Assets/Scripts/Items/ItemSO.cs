using UnityEngine;

public enum ItemCategory { Tool, Weapon, Resource, Consumable, Minerals } // thêm loại
public enum ToolType { None, Axe, Hoe, Sickle, Shovel }
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
    public ItemCategory category;
    public bool stackable = true;   // thêm
    public int maxStack = 999;      // thêm

    public ToolType toolType;           // dùng khi category = Tool
    public WeaponType weaponType;       // dùng khi category = Weapon

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
