
using UnityEngine;

public interface IMineable
{
    void Mine(int power, Vector2 hitDir);
    bool IsDepleted { get; }
}
