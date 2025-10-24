using UnityEngine;

public class ChopTree : MonoBehaviour, IToolTarget
{
    public int hp = 3;
    public DropLootOnDeath dropper; // kéo từ Inspector

    void Reset(){ dropper = GetComponent<DropLootOnDeath>(); }

    public void Hit(ToolType tool, int power, Vector2 hitDir)
    {
        if (tool != ToolType.Axe) return;
        hp -= Mathf.Max(1, power);
        if (hp <= 0)
        {
            if (dropper) dropper.Drop(); // rơi gỗ
            Destroy(gameObject);
        }
    }
}
