// ItemDB.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="DB/ItemDB")]
public class ItemDB : ScriptableObject
{
    public ItemSO[] items;
    Dictionary<string, ItemSO> byKey;
    Dictionary<ItemSO, string> keyOf;

    void OnEnable(){
        byKey = new Dictionary<string, ItemSO>();
        keyOf = new Dictionary<ItemSO, string>();
        foreach (var it in items){
            if (!it) continue;
            var k = it.name; // khóa = tên asset, giữ cố định
            if (!byKey.ContainsKey(k)) byKey[k] = it;
            if (!keyOf.ContainsKey(it)) keyOf[it] = k;
        }
    }
    public string GetKey(ItemSO it) => it && keyOf.TryGetValue(it, out var k) ? k : null;
    public ItemSO Find(string key) => key != null && byKey.TryGetValue(key, out var it) ? it : null;
}
