using UnityEngine;
// Quản lý việc mở/đóng sách kho đồ
public class BookToggle : MonoBehaviour
{
    [SerializeField] GameObject bookPanel; // BookInventoryPanel

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool active = bookPanel.activeSelf;
            bookPanel.SetActive(!active);

            // Optional: pause game khi mở sách
            // Time.timeScale = !active ? 0f : 1f;
        }
    }
}
