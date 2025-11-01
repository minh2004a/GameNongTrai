// SleepPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class SleepPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] Button okBtn, cancelBtn;
    [SerializeField] SleepManager sleep;

    void Awake(){
        Show(false);
        okBtn.onClick.AddListener(() => { Show(false); sleep.SleepNow(); });
        cancelBtn.onClick.AddListener(() => Show(false));
    }

    public void Show(bool v){
        if (!group) { gameObject.SetActive(v); return; }
        group.alpha = v ? 1 : 0;
        group.interactable = v;
        group.blocksRaycasts = v;
        gameObject.SetActive(v);
    }
}
