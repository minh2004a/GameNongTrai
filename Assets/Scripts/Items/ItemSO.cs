using UnityEngine;

public enum ItemCategory { Tool, Weapon }
public enum ToolType { None, Axe, Hoe, Sickle, Shovel }
public enum WeaponType { None, Sword, Bow }

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public Sprite icon;
    public ItemCategory category;
    public ToolType toolType;           // dùng khi category = Tool
    public WeaponType weaponType;       // dùng khi category = Weapon

    public int power = 1;
    public float range = 1f;
    public float cooldown = 0.2f;

    // cho Bow (để null với vũ khí/công cụ khác)
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    public float projectileMaxDistance = 8f;   // tầm bắn
    public GameObject projectileHitVFX;        // prefab hiệu ứng trúng
}
