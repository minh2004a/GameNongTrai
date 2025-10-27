// DropLootOnDeath.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable] public class DropEntry{ public ItemSO item; public Vector2Int countRange = new(1,1); [Range(0,1)] public float chance = 1f; }

public class DropLootOnDeath : MonoBehaviour
{
    public bool spawnSingles = true;      // rơi tách từng cục
    public int maxSinglesPerDrop = 12;    // giới hạn số cục sinh ra
    public bool fullCircle = true;        // văng 360°
    public PickupItem2D pickupPrefab;
    public List<DropEntry> drops = new();
    public Vector2 scatterSpeed = new(0.8f, 1.4f);
    public float scatterAngle = 35f;

    public void Drop()
{
    Vector2 pos = transform.position;
    foreach (var d in drops)
    {
        if (!d.item) continue;
        if (Random.value > d.chance) continue;

        int cnt = Random.Range(d.countRange.x, d.countRange.y + 1);

        if (spawnSingles)
        {
            int pieces = Mathf.Min(cnt, maxSinglesPerDrop);
            for (int k = 0; k < pieces; k++)
                Spawn(d.item, 1, pos);        // mỗi pickup = 1 cục

            int rest = cnt - pieces;          // phần dư thì gộp 1 cục
            if (rest > 0) Spawn(d.item, rest, pos);
        }
        else
        {
            Spawn(d.item, cnt, pos);          // kiểu cũ: một pickup to
        }
    }
}

void Spawn(ItemSO item, int count, Vector2 pos)
{
    var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
    go.Set(item, count);

    var rb = go.GetComponent<Rigidbody2D>();
    if (rb)
    {
        float angDeg = fullCircle ? Random.Range(0f, 360f)
                                  : Random.Range(-scatterAngle, scatterAngle);
        float ang = angDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
        rb.velocity = dir * Random.Range(scatterSpeed.x, scatterSpeed.y);
    }
}

}
