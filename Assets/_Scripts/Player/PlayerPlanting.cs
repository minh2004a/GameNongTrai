// PlayerPlanting.cs
using UnityEngine;
// Quản lý việc trồng cây của người chơi từ hạt giống
[RequireComponent(typeof(PlayerInventory))]
public class PlayerPlanting : MonoBehaviour
{
    public PlantSystem plantSystem;
    Camera cam;
    PlayerInventory inv;
    [SerializeField, Tooltip("Animator điều khiển hoạt ảnh trồng và nhổ bằng tay.")] Animator animator;
    [SerializeField, Tooltip("SpriteRenderer của nhân vật dùng để xác định hướng quay.")] SpriteRenderer sprite;
    [SerializeField, Tooltip("Tham chiếu tới PlayerHeldItemDisplay để hiển thị vật phẩm vừa thu hoạch.")] PlayerHeldItemDisplay heldItemDisplay;
    static readonly int HandHarvestTrigger = Animator.StringToHash("HandHarvest");
    bool isHandHarvesting;
    ItemSO pendingHarvestItem;
    int pendingHarvestCount;
    Vector3 pendingHarvestWorldPos;
    [SerializeField, Tooltip("Lớp đối tượng được phép kiểm tra khi tìm cây có thể nhổ bằng tay.")] LayerMask harvestMask = ~0;
    [SerializeField, Min(0f), Tooltip("Khoảng cách tối đa người chơi có thể nhổ cây bằng tay.")] float harvestRange = 1.5f;
    [SerializeField, Min(0f), Tooltip("Thời gian icon thu hoạch được giữ trên đầu người chơi.")] float handHarvestDisplayDuration = 1.2f;
    [SerializeField, Min(0f), Tooltip("Thời gian chuyển động nâng icon từ gốc cây lên vị trí cầm.")] float handHarvestLiftDuration = 0.45f;

    void Awake(){
        inv = GetComponent<PlayerInventory>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sprite)
        {
            var ctrl = GetComponent<PlayerController>();
            sprite = ctrl ? ctrl.GetComponentInChildren<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }
        if (!heldItemDisplay)
        {
            heldItemDisplay = GetComponent<PlayerHeldItemDisplay>();
            if (!heldItemDisplay)
            {
                heldItemDisplay = GetComponentInChildren<PlayerHeldItemDisplay>();
            }
        }
        cam = Camera.main;
        if (!plantSystem) plantSystem = FindFirstObjectByType<PlantSystem>();
        pendingHarvestWorldPos = transform.position;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!cam) cam = Camera.main;
            Vector3 wp3 = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 wp = new Vector2(wp3.x, wp3.y);

            if (TryHarvestAt(wp)) return;
            TryPlantFromSelected(wp);
        }
    }

    // PlayerPlanting.cs — sửa TryPlantFromSelected()
    void TryPlantFromSelected(Vector2 worldPos)
    {
        var it = inv.CurrentItem; var seed = it ? it.seedData : null;
        if (!seed) return;

        if (!plantSystem.CanPlantAt(worldPos, transform.position, seed.gridSize * 1f,
            seed, out var snapped, out var blocked, out var tooFar)) return;

        if (plantSystem.TryPlantAt(snapped, seed, out _)) inv.ConsumeSelected(1);
    }

    bool TryHarvestAt(Vector2 worldPos)
    {
        if (isHandHarvesting) return false;

        int mask = harvestMask.value;
        if (mask == 0) mask = Physics2D.AllLayers;

        float radius = Mathf.Max(0.01f, GetHarvestGridSize()) * 0.5f;
        var hits = Physics2D.OverlapCircleAll(worldPos, radius, mask);
        if (hits == null || hits.Length == 0) return false;

        PlantGrowth best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            var plant = h.GetComponentInParent<PlantGrowth>();
            if (!plant || !plant.CanHarvestByHand) continue;
            float distToCursor = ((Vector2)plant.transform.position - worldPos).sqrMagnitude;
            if (distToCursor < bestDist)
            {
                best = plant;
                bestDist = distToCursor;
            }
        }

        if (!best) return false;
        if (!IsWithinHarvestGrid(best.transform.position)) return false;
        if (Vector2.Distance(transform.position, best.transform.position) > harvestRange) return false;

        if (!best.TryHarvestByHand(inv, out var harvestedItem, out var harvestedCount)) return false;

        CachePendingHarvest(harvestedItem, harvestedCount, best.transform.position);
        TriggerHandHarvestAnimation(best.transform.position);
        return true;
    }

    void TriggerHandHarvestAnimation(Vector3 targetPos)
    {
        if (!animator)
        {
            isHandHarvesting = false;
            FlushPendingHarvestDisplay();
            return;
        }

        Vector2 toTarget = (Vector2)targetPos - (Vector2)transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector2 facing = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.y)
                ? new Vector2(Mathf.Sign(toTarget.x), 0f)
                : new Vector2(0f, Mathf.Sign(toTarget.y));

            animator.SetFloat("Horizontal", facing.x);
            animator.SetFloat("Vertical", facing.y);
            if (sprite) sprite.flipX = facing.x < 0f;
        }

        isHandHarvesting = true;
        animator.ResetTrigger(HandHarvestTrigger);
        animator.SetTrigger(HandHarvestTrigger);
    }

    void CachePendingHarvest(ItemSO item, int count, Vector3 worldPosition)
    {
        if (item && count > 0)
        {
            pendingHarvestItem = item;
            pendingHarvestCount = count;
            pendingHarvestWorldPos = worldPosition;
        }
        else
        {
            pendingHarvestItem = null;
            pendingHarvestCount = 0;
            pendingHarvestWorldPos = transform.position;
        }
    }

    void ShowHarvestedItem(ItemSO item, int count)
    {
        if (!heldItemDisplay) return;
        if (!item || count <= 0) return;
        heldItemDisplay.ShowHandHarvestedItem(item, pendingHarvestWorldPos, handHarvestLiftDuration, handHarvestDisplayDuration);
    }

    public void BeginHandHarvestAnimation()
    {
        isHandHarvesting = true;
        FlushPendingHarvestDisplay();
    }

    public void EndHandHarvestAnimation()
    {
        isHandHarvesting = false;
        pendingHarvestItem = null;
        pendingHarvestCount = 0;
        pendingHarvestWorldPos = transform.position;
    }

    void FlushPendingHarvestDisplay()
    {
        if (!pendingHarvestItem || pendingHarvestCount <= 0) return;
        ShowHarvestedItem(pendingHarvestItem, pendingHarvestCount);
        pendingHarvestItem = null;
        pendingHarvestCount = 0;
        pendingHarvestWorldPos = transform.position;
    }

    bool IsWithinHarvestGrid(Vector2 targetPos)
    {
        float grid = Mathf.Max(0.01f, GetHarvestGridSize());
        Vector2 playerPos = transform.position;

        int px = Mathf.FloorToInt(playerPos.x / grid);
        int py = Mathf.FloorToInt(playerPos.y / grid);
        int tx = Mathf.FloorToInt(targetPos.x / grid);
        int ty = Mathf.FloorToInt(targetPos.y / grid);

        const int radiusInCells = 1; // 3x3 ô quanh người chơi
        return Mathf.Abs(tx - px) <= radiusInCells && Mathf.Abs(ty - py) <= radiusInCells;
    }

    float GetHarvestGridSize()
    {
        if (plantSystem && plantSystem.soilManager)
        {
            return Mathf.Max(0.01f, plantSystem.soilManager.GridSize);
        }

        return 1f;
    }

}