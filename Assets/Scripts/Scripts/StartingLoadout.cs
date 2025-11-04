using UnityEngine;

[CreateAssetMenu(fileName = "StartingLoadout", menuName = "Game/Starting Loadout")]
public class StartingLoadout : ScriptableObject
{
    [System.Serializable]
    public struct Entry {
        public ItemSO item;       // để trống nếu dùng key
        public string itemKey;    // để trống nếu kéo thả item
        public int amount;        // số lượng
        public bool equip;        // có auto trang bị hay không
        public int hotbarSlot;    // -1 nếu không gán hotbar
    }
    public Entry[] items;
}
