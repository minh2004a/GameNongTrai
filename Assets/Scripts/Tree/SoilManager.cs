// SoilManager.cs
using System.Collections.Generic;
using UnityEngine;

// Quản lý trạng thái ô đất đã được xới, hỗ trợ lưu/khôi phục
public class SoilManager : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float gridSize = 1f;
    [SerializeField] GameObject tilledSoilPrefab;
    [SerializeField] Transform tilledParent;
    [SerializeField] LayerMask tillableMask;
    [SerializeField, Range(0.05f, 1f)] float maskCheckRadiusMultiplier = 0.45f;

    readonly HashSet<Vector2Int> tilledCells = new();
    readonly Dictionary<Vector2Int, GameObject> visuals = new();

    string sceneName;

    void Awake()
    {
        gridSize = Mathf.Max(0.01f, gridSize);
        if (!tilledParent) tilledParent = transform;
        sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : null;
        RestoreFromSave();
    }

    public float GridSize => gridSize;

    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        float s = Mathf.Max(0.01f, gridSize);
        int x = Mathf.FloorToInt(worldPos.x / s);
        int y = Mathf.FloorToInt(worldPos.y / s);
        return new Vector2Int(x, y);
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        float s = Mathf.Max(0.01f, gridSize);
        return new Vector2((cell.x + 0.5f) * s, (cell.y + 0.5f) * s);
    }

    public Vector2 SnapToGrid(Vector2 worldPos) => CellToWorld(WorldToCell(worldPos));

    public bool IsTilled(Vector2 worldPos) => IsCellTilled(WorldToCell(worldPos));

    public bool IsCellTilled(Vector2Int cell) => tilledCells.Contains(cell);

    public bool TryTillAt(Vector2 worldPos)
    {
        return TryTillCell(WorldToCell(worldPos));
    }

    public bool TryTillCell(Vector2Int cell)
    {
        if (!CanTillCell(cell)) return false;
        AddCell(cell, true);
        return true;
    }

    public void EnsureCellTilledFromSave(Vector2Int cell)
    {
        AddCell(cell, false);
    }

    bool CanTillCell(Vector2Int cell)
    {
        if (tilledCells.Contains(cell)) return false;
        if (tillableMask.value == 0) return true;
        Vector2 center = CellToWorld(cell);
        float radius = Mathf.Max(0.01f, gridSize) * Mathf.Clamp(maskCheckRadiusMultiplier, 0.05f, 1f);
        return Physics2D.OverlapCircle(center, radius, tillableMask);
    }

    void AddCell(Vector2Int cell, bool markPending)
    {
        if (!tilledCells.Add(cell)) return;
        SpawnVisual(cell);
        if (markPending && !string.IsNullOrEmpty(sceneName))
        {
            SaveStore.MarkSoilTilledPending(sceneName, cell);
        }
    }

    void SpawnVisual(Vector2Int cell)
    {
        if (!tilledSoilPrefab) return;
        if (visuals.TryGetValue(cell, out var existing) && existing)
        {
            existing.transform.position = CellToWorld(cell);
            return;
        }

        var parent = tilledParent ? tilledParent : transform;
        var inst = Instantiate(tilledSoilPrefab, CellToWorld(cell), Quaternion.identity, parent);
        visuals[cell] = inst;
    }

    void RestoreFromSave()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        foreach (var cell in SaveStore.GetTilledSoilInScene(sceneName))
        {
            AddCell(cell, false);
        }
    }
}
