using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    public void Set01(float v){
        if (fill) fill.fillAmount = Mathf.Clamp01(v);
    }
}
