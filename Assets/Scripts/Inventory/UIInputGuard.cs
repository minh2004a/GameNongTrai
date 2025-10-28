using UnityEngine;
using UnityEngine.EventSystems;

public static class UIInputGuard
{
    static int clickedUIFrame = -1;
    public static void MarkClick() => clickedUIFrame = Time.frameCount;

    public static bool BlockInputNow()
    {
        if (clickedUIFrame == Time.frameCount) return true;
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
