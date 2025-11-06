// PickupItem2D.cs
using UnityEngine;
// Quản lý vật phẩm có thể nhặt được trong thế giới 2D
public class PickupItem2D : MonoBehaviour
{
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] ItemSO item;
    [SerializeField] int count = 1;

    void Reset(){ iconRenderer = GetComponentInChildren<SpriteRenderer>(); }
    public void Set(ItemSO i, int c){ item = i; count = Mathf.Max(1,c); if (iconRenderer) iconRenderer.sprite = i ? i.icon : null; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var inv = other.GetComponent<PlayerInventory>(); if (!inv) return;

        var result = inv.AddItemDetailed(item, count);
        if (result.remaining <= 0) Destroy(gameObject);
        else count = result.remaining; // kho còn dư chỗ → giữ lại phần chưa nhét được
    }
}