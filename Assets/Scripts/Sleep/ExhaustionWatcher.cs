// ExhaustionWatcher.cs
using UnityEngine;
//// <summary>
/// Giám sát trạng thái kiệt sức của nhân vật để kích hoạt ngủ.
public class ExhaustionWatcher : MonoBehaviour
{
    [SerializeField] PlayerStamina stamina;
    [SerializeField] SleepManager sleep;
    bool handled;

    void Update()
    {
        if (!handled && stamina.IsFainted)
        {
            handled = true;
            sleep.FaintNow();
        }
    }
    public void ResetHandled() => handled = false;
}
