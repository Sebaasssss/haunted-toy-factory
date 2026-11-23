using HauntedFactory.Core;
using UnityEngine;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Al ser atrapado, la cámara gira de golpe hacia la cara del juguete
    /// y hace zoom, antes de que aparezca la pantalla de derrota.
    /// Usa tiempo sin escalar porque el juego se congela al perder.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CaughtCamera : MonoBehaviour
    {
        [SerializeField] private float turnTime = 0.3f;
        [SerializeField] private float zoomFov = 45f;
        [SerializeField] private float headHeight = 1.5f;

        private Camera cam;
        private bool active;
        private float t;
        private Quaternion startRot;
        private Quaternion targetRot;
        private float startFov;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.GameLost += OnLost;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.GameLost -= OnLost;
        }

        private void OnLost()
        {
            Transform catcher = GameManager.Instance != null ? GameManager.Instance.Catcher : null;
            if (catcher == null) return;
            Vector3 head = catcher.position + Vector3.up * headHeight;
            startRot = transform.rotation;
            targetRot = Quaternion.LookRotation(head - transform.position);
            startFov = cam.fieldOfView;
            t = 0f;
            active = true;
        }

        private void Update()
        {
            if (!active) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / turnTime);
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            cam.fieldOfView = Mathf.Lerp(startFov, zoomFov, k);
            // Temblor mientras el juguete te tiene
            transform.rotation *= Quaternion.Euler(Random.Range(-1f, 1f) * (1f - k * 0.5f), Random.Range(-1f, 1f) * (1f - k * 0.5f), 0f);
        }
    }
}
