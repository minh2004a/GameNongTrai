// SleepManager.cs  (đặt ở Persistent)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Quản lý quá trình ngủ của nhân vật, bao gồm chuyển scene, tua thời gian
public class SleepManager : MonoBehaviour
{
    [SerializeField] TimeManager timeMgr;
    [SerializeField] PlayerHealth hp;
    [SerializeField] PlayerStamina stamina;
    [SerializeField] GameObject player;
    [SerializeField] CanvasGroup fade;
    
    [SerializeField] MonoBehaviour[] toDisable;
    public bool suppressBedPromptOnce;
    Transform bedSpawn;  // resolve theo scene House
    public System.Action OnSaveRequested;

    void Awake()
    {
        suppressBedPromptOnce = true;   // không bật panel khi spawn ở giường
    }    void OnEnable(){
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable(){
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene s, LoadSceneMode m){
        if (s.name == "House") ResolveBedSpawn();
    }

    void ResolveBedSpawn(){
        var anchor = FindObjectOfType<BedSpawnAnchor>(true);
        bedSpawn = anchor ? anchor.transform : null;
    }
    bool _transitioning;
    public void SleepNow()
    {
        if (_transitioning) return;
        _transitioning = true;
        StopAllCoroutines();
        StartCoroutine(CoSleep(forceToHouse: false));
    }
    
    public void FaintNow()
    {
        if (_transitioning) return;
        _transitioning = true;
        StopAllCoroutines();
        StartCoroutine(CoSleep(forceToHouse: true));
    }
    static readonly HashSet<string> kKeep = new HashSet<string>{
    "Persistent", "DontDestroyOnLoad", "House"  // các scene giữ lại khi ngất
};
    static class SceneUtils
    {
        public static IEnumerator EnsureSingleScene(string target)
        {
            // 1) liệt kê mọi scene đang mở
            var loaded = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                loaded.Add(SceneManager.GetSceneAt(i));                        // :contentReference[oaicite:2]{index=2}

            // 2) gỡ mọi scene không phải Persistent/DontDestroyOnLoad/target
            foreach (var s in loaded)
            {
                if (s.name != "Persistent" && s.name != "DontDestroyOnLoad" && s.name != target)
                {
                    yield return SceneManager.UnloadSceneAsync(s);             // :contentReference[oaicite:3]{index=3}
                }
            }

            // 3) gom các bản scene đích
            var targets = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name == target) targets.Add(s);
            }

            // 4) nếu >1 bản -> giữ bản đầu, gỡ các bản còn lại
            for (int i = 1; i < targets.Count; i++)
                yield return SceneManager.UnloadSceneAsync(targets[i]);        // tên trùng chỉ gỡ bản chỉ định, không gỡ hết. :contentReference[oaicite:4]{index=4}

            // 5) nếu chưa có bản nào -> tải Additive
            Scene active;
            if (targets.Count == 0)
            {
                var op = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);  // không unload cái khác. :contentReference[oaicite:5]{index=5}
                while (!op.isDone) yield return null;
                active = SceneManager.GetSceneByName(target);   // lưu ý: khi trùng tên, API trả bản đầu tiên. :contentReference[oaicite:6]{index=6}
            }
            else
            {
                active = targets[0];
            }

            // 6) đóng dấu ActiveScene
            SceneManager.SetActiveScene(active);                                                // :contentReference[oaicite:7]{index=7}
        }
    }
    IEnumerator CoSleep(bool forceToHouse)
    {
        // 1) Khoá điều khiển và fade lên đen
        foreach (var m in toDisable) if (m) m.enabled = false;
        yield return Fade(1f, 0.35f); // <-- LÊN ĐEN
        suppressBedPromptOnce = true;
        // 2) Gỡ mọi scene không phải Persistent/House
        var toUnload = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            if (s.name != "Persistent" && s.name != "House") toUnload.Add(s);
        }
        foreach (var s in toUnload)
            yield return SceneManager.UnloadSceneAsync(s); // Additive không tự dọn. :contentReference[oaicite:2]{index=2}

        // 3) Đảm bảo House đã được nạp và đặt Active
        var house = SceneManager.GetSceneByName("House"); // trả về bản KHỚP ĐẦU TIÊN theo tên. :contentReference[oaicite:3]{index=3}
        if (!house.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync("House", LoadSceneMode.Additive);
            while (!op.isDone) yield return null;          // chờ nạp xong. :contentReference[oaicite:4]{index=4}
            house = SceneManager.GetSceneByName("House");
        }
        SceneManager.SetActiveScene(house);                // đổi scene đích làm Active. :contentReference[oaicite:5]{index=5}
        // 4) Teleport về giường, tua thời gian, save
        ResolveBedSpawn();
        if (player && bedSpawn) player.transform.position = bedSpawn.position;
        timeMgr.SleepToNextMorningRecover(hp, stamina, fullSleepMin: 480);
        OnSaveRequested?.Invoke();

        // 5) Fade xuống trong và MỞ KHÓA ở CUỐI
        yield return Fade(0f, 0.35f); // <-- XUỐNG TRONG           // thêm
        foreach (var m in toDisable) if (m) m.enabled = true;
        var watcher = player.GetComponent<ExhaustionWatcher>();
        watcher?.ResetHandled();
        _transitioning = false;
    }
    IEnumerator Fade(float target, float dur){
        if (!fade) yield break;
        fade.gameObject.SetActive(true);
        float a0 = fade.alpha, t = 0f;
        while (t < dur){ t += Time.unscaledDeltaTime; fade.alpha = Mathf.Lerp(a0, target, t/dur); yield return null; }
        fade.alpha = target;
        if (Mathf.Approximately(target, 0f)) fade.gameObject.SetActive(false);
    }
}
