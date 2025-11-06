using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
// Quản lý việc chọn ô trên thanh công cụ (hotbar) của người chơi
public class HotbarSelect : MonoBehaviour
{
    [SerializeField] PlayerInventory inv;

    void Awake(){ if (!inv) inv = GetComponent<PlayerInventory>(); }

    void Update()
    {
        var k = Keyboard.current;
        if (Pressed(k.digit1Key) || Pressed(k.numpad1Key)) inv.SelectSlot(0);
        else if (Pressed(k.digit2Key) || Pressed(k.numpad2Key)) inv.SelectSlot(1);
        else if (Pressed(k.digit3Key) || Pressed(k.numpad3Key)) inv.SelectSlot(2);
        else if (Pressed(k.digit4Key) || Pressed(k.numpad4Key)) inv.SelectSlot(3);
        else if (Pressed(k.digit5Key) || Pressed(k.numpad5Key)) inv.SelectSlot(4);
        else if (Pressed(k.digit6Key) || Pressed(k.numpad6Key)) inv.SelectSlot(5);
        else if (Pressed(k.digit7Key) || Pressed(k.numpad7Key)) inv.SelectSlot(6);
        else if (Pressed(k.digit8Key) || Pressed(k.numpad8Key)) inv.SelectSlot(7);

        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (scroll > 0.01f) inv.CycleSlot(-1);
        else if (scroll < -0.01f) inv.CycleSlot(+1);
    }

    bool Pressed(KeyControl key) => key != null && key.wasPressedThisFrame;
}
