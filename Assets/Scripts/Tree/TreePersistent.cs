using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class TreePersistent : MonoBehaviour
{
    [Header("Spawn gốc khi cây đã bị chặt")]
    public GameObject stumpPrefab;
    public bool spawnStumpOnRemoved = true;

    void Awake()
    {
        var uid = GetComponent<UniqueId>(); if (!uid) return;
        string scn = gameObject.scene.name; string id = uid.Id;

        if (SaveStore.IsStumpClearedInSession(scn, id)) { Destroy(gameObject); return; }

        if (SaveStore.IsTreeChoppedInSession(scn, id))
        {
            if (spawnStumpOnRemoved && stumpPrefab)
            {
                var stump = Instantiate(stumpPrefab, transform.position, transform.rotation, transform.parent);
                var tag = stump.GetComponent<StumpOfTree>() ?? stump.AddComponent<StumpOfTree>();
                tag.treeId = id;
            }
            Destroy(gameObject);
        }
    }
}
