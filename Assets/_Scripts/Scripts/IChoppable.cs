// IChoppable.cs
using UnityEngine;

public interface IChoppable {
    void Chop(int power, Vector2 hitDir);
    bool IsDead { get; }
}
