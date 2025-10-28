// StaminaBarUI.cs
using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    public void Set01(float t){
        if (fill) fill.fillAmount = Mathf.Clamp01(t);
    }
}
