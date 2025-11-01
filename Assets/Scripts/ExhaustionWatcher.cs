// ExhaustionWatcher.cs
using UnityEngine;

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
