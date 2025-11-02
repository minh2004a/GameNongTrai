// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using System.Collections;

// public class MenuController : MonoBehaviour
// {
//     [Header("Scenes")]
//     [SerializeField] string persistentScene = "Persistent";
//     [SerializeField] string startMapScene   = "House";

//     [Header("UI")]
//     [SerializeField] Button newGameButton;
//     [SerializeField] Button continueButton;
//     [SerializeField] Button quitButton;
//     [SerializeField] GameObject confirmWipePanel;

//     void Awake(){
//         // Đủ để bật/tắt nút Chơi tiếp
//         if (continueButton) continueButton.interactable = SaveStore.HasAnySave();
//         Time.timeScale = 1f;
//     }

//     public void OnClickNewGame(){
//         // Nếu có panel xác nhận và đã có save -> hỏi xoá.
//         if (SaveStore.HasAnySave() && confirmWipePanel){
//             confirmWipePanel.SetActive(true);
//             return;
//         }
//         StartCoroutine(StartNewGameRoutine());
//     }
//     public void OnConfirmWipe(){
//         // Gọi NewGame sẽ ghi file mới luôn
//         StartCoroutine(StartNewGameRoutine());
//     }
//     public void OnCancelWipe(){ if (confirmWipePanel) confirmWipePanel.SetActive(false); }

//     IEnumerator StartNewGameRoutine(){
//         // Tạo save rỗng + meta
//         SaveStore.NewGame(startMapScene);
//         // Vào Persistent rồi additively load map bắt đầu
//         yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);
//         yield return SceneManager.LoadSceneAsync(startMapScene, LoadSceneMode.Additive);
//         SceneManager.SetActiveScene(SceneManager.GetSceneByName(startMapScene));
//     }

//     public void OnClickContinue(){
//         if (!SaveStore.HasAnySave()) return;
//         StartCoroutine(ContinueRoutine());
//     }
//     IEnumerator ContinueRoutine(){
//         yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);
//         var scene = SaveStore.GetLastScene();
//         yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
//         SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
//     }

//     public void OnClickQuit(){
// #if UNITY_EDITOR
//         UnityEditor.EditorApplication.isPlaying = false;
// #else
//         Application.Quit();
// #endif
//     }
// }
