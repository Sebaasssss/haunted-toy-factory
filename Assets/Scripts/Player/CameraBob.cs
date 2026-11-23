using UnityEngine;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Balanceo suave de la cámara al caminar y correr (sensación de pasos).
    /// Va en la cámara, hija de CameraRoot, para no interferir con PlayerLook.
    /// </summary>
    public class CameraBob : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private float walkFrequency = 9f;
        [SerializeField] private float runFrequency = 14f;
        [SerializeField] private float walkAmplitude = 0.035f;
        [SerializeField] private float runAmplitude = 0.065f;
        [SerializeField] private float returnSpeed = 6f;

        private Vector3 startLocalPos;
        private float phase;

        private void Start()
        {
            startLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (movement == null) return;

            Vector3 target = startLocalPos;
            if (movement.IsMoving && movement.CanMove)
            {
                bool running = movement.IsRunning;
                phase += Time.deltaTime * (running ? runFrequency : walkFrequency);
                float amp = running ? runAmplitude : walkAmplitude;
                target += new Vector3(Mathf.Cos(phase * 0.5f) * amp * 0.5f, Mathf.Abs(Mathf.Sin(phase)) * amp, 0f);
            }
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, returnSpeed * Time.deltaTime);
        }
    }
}
