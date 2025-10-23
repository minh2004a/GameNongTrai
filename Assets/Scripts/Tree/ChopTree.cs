// ChopTree.cs
using UnityEngine;

public class ChopTree : MonoBehaviour, IToolTarget
{
    public int hp = 3;

    public void Hit(ToolType tool, int power, Vector2 hitDir){
        if (tool != ToolType.Axe) return;
        hp -= power;
        if (hp <= 0) Destroy(gameObject); // sau này thay bằng rơi loot
    }
}
