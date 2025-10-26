// using UnityEngine;

// [RequireComponent(typeof(Collider2D))]
// public class TriggerProxy : MonoBehaviour
// {
//     public string playerTag = "Player";
//     public TreeFader fader;

//     void Awake()
//     {
//         GetComponent<Collider2D>().isTrigger = true;
//         if (!fader) fader = GetComponentInParent<TreeFader>();
//     }

//     void OnTriggerEnter2D(Collider2D other)
// {
//     Debug.Log($"ENTER {other.name} tag={other.tag}");
//     if (other.CompareTag(playerTag)) fader?.Enter();
// }
// void OnTriggerExit2D(Collider2D other)
// {
//     Debug.Log($"EXIT {other.name} tag={other.tag}");
//     if (other.CompareTag(playerTag)) fader?.Exit();
// }
//     }

