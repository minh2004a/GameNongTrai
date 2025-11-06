
// ItemDB.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="DB/ItemDB")]
public class ItemDB : ScriptableObject
{
    public ItemSO[] items;
    Dictionary<string, ItemSO> byKey;
    Dictionary<string, ItemSO> byId;
    Dictionary<ItemSO, string> keyOf;
    int lastAugmentCount;

    void OnEnable()
    {
        BuildIndex();
    }

    void BuildIndex()
    {
        byKey = new Dictionary<string, ItemSO>();
        byId = new Dictionary<string, ItemSO>();
        keyOf = new Dictionary<ItemSO, string>();

        lastAugmentCount = 0;

        if (items != null)
        {
            foreach (var it in items)
            {
                RegisterItem(it);
            }
        }

        AugmentWithLoadedAssets(force: true);
    }

    void EnsureIndex()
    {
        if (byKey == null || byId == null || keyOf == null)
        {
            BuildIndex();
        }
    }

    void EnsureOnDemandEntry(ItemSO it)
    {
        if (!it) return;

        EnsureIndex();

        RegisterItem(it);
    }

    void RegisterItem(ItemSO it)
    {
        if (!it) return;

        var assetKey = it.name; // khóa = tên asset, giữ cố định
        if (!string.IsNullOrEmpty(assetKey))
        {
            if (!byKey.ContainsKey(assetKey))
            {
                byKey[assetKey] = it;
            }
            keyOf[it] = assetKey;
        }

        if (!string.IsNullOrEmpty(it.id) && !byId.ContainsKey(it.id))
        {
            byId[it.id] = it;
        }
    }

    void AugmentWithLoadedAssets(bool force = false)
    {
        var loaded = Resources.FindObjectsOfTypeAll<ItemSO>();
        if (!force && loaded.Length == lastAugmentCount) return;
        lastAugmentCount = loaded.Length;
        foreach (var it in loaded)
        {
            RegisterItem(it);
        }
    }

    public string GetKey(ItemSO it)
    {
        if (!it) return null;

        EnsureOnDemandEntry(it);

        if (keyOf.TryGetValue(it, out var key) && !string.IsNullOrEmpty(key))
        {
            return key;
        }

        if (!string.IsNullOrEmpty(it.id))
        {
            byId[it.id] = it;
            return it.id;
        }

        return it.name;
    }

    public ItemSO Find(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        EnsureIndex();

        if (byKey.TryGetValue(key, out var itByAsset))
        {
            return itByAsset;
        }

        if (byId.TryGetValue(key, out var itById))
        {
            return itById;
        }

        // fallback: try to discover more assets (e.g. items không có trong mảng items)
        AugmentWithLoadedAssets();

        if (byKey.TryGetValue(key, out itByAsset))
        {
            return itByAsset;
        }

        if (byId.TryGetValue(key, out itById))
        {
            return itById;
        }

        return null;
    }
}
