using UnityEngine;
using UnityEngine.InputSystem;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Control de cámara con el mouse: el cuerpo gira en horizontal (yaw)
    /// y la cámara en vertical (pitch), con límite para no dar la vuelta.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Tooltip("Objeto que contiene la cámara (se inclina arriba/abajo).")]
        [SerializeField] private Transform cameraRoot;

        [Tooltip("Grados por píxel de movimiento del mouse.")]
        [SerializeField] private float sensitivity = 0.08f;

        [SerializeField] private float maxPitch = 85f;

        private float pitch;

        /// <summary>Permite bloquear la cámara (menús, game over, victoria).</summary>
        public bool CanLook { get; set; } = true;

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Si el cursor se soltó (por ejemplo al cambiar de ventana), un clic lo recaptura
            if (CanLook && Time.timeScale > 0f && Cursor.lockState != CursorLockMode.Locked &&
                mouse.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }

            if (!CanLook || Cursor.lockState != CursorLockMode.Locked) return;

            // El delta del mouse ya viene por frame, no se multiplica por deltaTime
            Vector2 delta = mouse.delta.ReadValue() * sensitivity;

            transform.Rotate(0f, delta.x, 0f);

            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            if (cameraRoot != null)
            {
                cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
        }

        /// <summary>Bloquea y oculta el cursor, o lo libera y lo muestra.</summary>
        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
