// CameraBounds2D.cs
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class CameraBounds2D : MonoBehaviour
{
    public PolygonCollider2D Collider { get; private set; }
    void Awake() => Collider = GetComponent<PolygonCollider2D>();
}
