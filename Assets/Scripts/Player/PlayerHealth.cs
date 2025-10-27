using UnityEngine;
using UnityEngine.Events;
// Quản lý sức khỏe của người chơi
public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int hp;

    public UnityEvent<float> OnHpPercent; // giá trị 0..1

    void Awake(){ hp = maxHP; OnHpPercent?.Invoke(1f); }

    public void TakeDamage(int dmg){
        hp = Mathf.Max(0, hp - dmg);
        OnHpPercent?.Invoke((float)hp / maxHP);
    }

    public void Heal(int v){
        hp = Mathf.Min(maxHP, hp + v);
        OnHpPercent?.Invoke((float)hp / maxHP);
    }
}
