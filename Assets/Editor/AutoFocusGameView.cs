// #if UNITY_EDITOR
// using UnityEditor;

// [InitializeOnLoad]
// static class AutoFocusGameView
// {
//     static AutoFocusGameView()
//     {
//         EditorApplication.playModeStateChanged += s => {
//             if (s == PlayModeStateChange.EnteredPlayMode)
//                 EditorApplication.delayCall += FocusGameView;
//         };
//     }

//     static void FocusGameView()
//     {
//         var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
//         var w = EditorWindow.GetWindow(t);    // mở nếu chưa có
//         w.Focus();                            // đưa Game view lên và focus
//     }
// }
// #endif
