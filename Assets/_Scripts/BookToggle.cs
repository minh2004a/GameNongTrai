using UnityEngine;
// Quản lý việc mở/đóng sách kho đồ
public class BookToggle : MonoBehaviour
{
    [SerializeField] GameObject bookPanel; // BookInventoryPanel

    float previousTimeScale = 1f;
    bool pausedByBook = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool active = bookPanel.activeSelf;
            bookPanel.SetActive(!active);

            if (!active)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                pausedByBook = true;
            }
            else if (pausedByBook)
            {
                Time.timeScale = previousTimeScale;
                pausedByBook = false;
            }
        }
    }

    void OnDisable()
    {
        if (pausedByBook)
        {
            Time.timeScale = previousTimeScale;
            pausedByBook = false;
        }
    }
}
