#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

[InitializeOnLoad]
static class ItemDBAutoUpdater
{
    static bool _pending;
    static bool _isSyncing;

    static ItemDBAutoUpdater()
    {
        RequestSync();
    }

    static void RequestSync()
    {
        if (_pending) return;
        _pending = true;
        EditorApplication.delayCall += PerformSync;
    }

    static void PerformSync()
    {
        EditorApplication.delayCall -= PerformSync;
        _pending = false;
        SyncAllDatabases();
    }

    static void SyncAllDatabases()
    {
        if (_isSyncing) return;
        _isSyncing = true;
        try
        {
            var itemGuids = AssetDatabase.FindAssets("t:ItemSO");
            var allItems = new List<ItemSO>(itemGuids.Length);
            for (int i = 0; i < itemGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(itemGuids[i]);
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if (item)
                {
                    allItems.Add(item);
                }
            }

            var itemSet = new HashSet<ItemSO>(allItems);

            var dbGuids = AssetDatabase.FindAssets("t:ItemDB");
            for (int i = 0; i < dbGuids.Length; i++)
            {
                var dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[i]);
                var db = AssetDatabase.LoadAssetAtPath<ItemDB>(dbPath);
                if (!db) continue;

                var ordered = new List<ItemSO>();
                var seen = new HashSet<ItemSO>();
                bool changed = false;

                if (db.items != null)
                {
                    for (int j = 0; j < db.items.Length; j++)
                    {
                        var existing = db.items[j];
                        if (existing && itemSet.Contains(existing) && seen.Add(existing))
                        {
                            ordered.Add(existing);
                        }
                        else
                        {
                            changed = true;
                        }
                    }
                }

                for (int j = 0; j < allItems.Count; j++)
                {
                    var item = allItems[j];
                    if (seen.Add(item))
                    {
                        ordered.Add(item);
                        changed = true;
                    }
                }

                if (changed)
                {
                    db.items = ordered.ToArray();
                    EditorUtility.SetDirty(db);
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    class ItemAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            RequestSync();
        }
    }
}
#endif