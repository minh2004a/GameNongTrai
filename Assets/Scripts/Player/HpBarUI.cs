using UnityEngine;
using UnityEngine.UI;
// Quản lý thanh HP hiển thị trên UI
public class HPBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    public void Set01(float v){
        if (fill) fill.fillAmount = Mathf.Clamp01(v);
    }
}
