// PlayerHealth.cs
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    void Awake() { currentHP = maxHP; }

    public void TakeDamage(int dmg)
    {
        currentHP = Mathf.Max(0, currentHP - dmg);
        Debug.Log($"Player HP: {currentHP}");
        // TODO: thêm flash/knockback/ chết...
    }
}
