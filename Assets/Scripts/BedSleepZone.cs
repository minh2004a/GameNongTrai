// using UnityEngine;
// using UnityEngine.UI;

// [RequireComponent(typeof(Collider2D))]
// public class BedSleepZone : MonoBehaviour
// {
//     [Header("UI")]
//     [SerializeField] GameObject promptUI;   // Panel có TMP_Text + 2 nút
//     [SerializeField] Button okButton;       // nút ✓
//     [SerializeField] Button cancelButton;   // nút ✕
//     [SerializeField] string playerTag = "Player";
//     [SerializeField] PlayerHealth hp;
//     [SerializeField] PlayerStamina stamina;
//     TimeManager tm;
//     bool suppressNextEnter;                  // thêm

//     void Reset()
//     {
//         var col = GetComponent<Collider2D>();
//         col.isTrigger = true;
//     }
//     public void SuppressPromptOnce() => suppressNextEnter = true;   // thêm
//     void Awake()
//     {
//         tm = FindObjectOfType<TimeManager>();
//         if (promptUI) promptUI.SetActive(false);
//         if (okButton) okButton.onClick.AddListener(SleepNow);
//         if (cancelButton) cancelButton.onClick.AddListener(ClosePrompt);
//     }
//     void SleepNow()
//     {
//         if (tm) tm.SleepToNextMorningRecover(hp, stamina, 480);
//         ClosePrompt();
//     }
//     void OnTriggerEnter2D(Collider2D other)
//     {
//         if (!other.CompareTag(playerTag)) return;                   // dùng CompareTag
//         if (suppressNextEnter)
//         {                                     // chặn 1 lần sau teleport
//             suppressNextEnter = false;
//             return;
//         }
//         ShowPrompt();
//     }

//     void OnTriggerExit2D(Collider2D other)
//     {
//         if (other != null && other.CompareTag(playerTag)) ClosePrompt();
//     }

//     void ShowPrompt(){ if (promptUI) promptUI.SetActive(true); }
//     void ClosePrompt(){ if (promptUI) promptUI.SetActive(false); }

// }
