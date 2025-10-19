using UnityEngine;

[System.Serializable] public struct ItemStack { public ItemSO item; public int count; }

public class PlayerInventory : MonoBehaviour
{
    public ItemStack[] hotbar = new ItemStack[8];
    public int selected;
    public ItemSO CurrentItem => (selected >= 0 && selected < hotbar.Length) ? hotbar[selected].item : null;
}
