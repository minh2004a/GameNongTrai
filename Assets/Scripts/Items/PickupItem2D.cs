// PickupItem2D.cs
using UnityEngine;

public class PickupItem2D : MonoBehaviour
{
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] ItemSO item;
    [SerializeField] int count = 1;

    void Reset(){ iconRenderer = GetComponentInChildren<SpriteRenderer>(); }
    public void Set(ItemSO i, int c){ item = i; count = Mathf.Max(1,c); if (iconRenderer) iconRenderer.sprite = i ? i.icon : null; }

    void OnTriggerEnter2D(Collider2D other){
        var inv = other.GetComponent<PlayerInventory>();
        if (!inv || !item) return;
        if (InventoryUtil.TryAddToHotbar(inv, item, count)) Destroy(gameObject);
    }
}
