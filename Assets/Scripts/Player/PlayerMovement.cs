using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Movimiento del elfo en primera persona.
    /// WASD para caminar, Shift para correr, Espacio para saltar.
    /// Usa CharacterController y aplica gravedad.
    /// Lee el teclado con el Input System nuevo (el proyecto tiene desactivado el Input Manager viejo).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Velocidades (m/s)")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6f;
        [Tooltip("Qué tan rápido se alcanza la velocidad objetivo (suaviza arranques y frenadas).")]
        [SerializeField] private float acceleration = 14f;

        [Header("Salto y gravedad")]
        [Tooltip("Altura máxima del salto en metros.")]
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool wasGrounded = true;
        private float airTime;

        /// <summary>True mientras el jugador se mueve.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>True mientras el jugador corre (Shift + movimiento). Útil para que el enemigo lo oiga.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Permite bloquear el movimiento (menús, game over, victoria).</summary>
        public bool CanMove { get; set; } = true;

        public bool IsGrounded => controller != null && controller.isGrounded;

        /// <summary>Se dispara al despegar del suelo con un salto.</summary>
        public event Action Jumped;
        /// <summary>Se dispara al tocar el suelo después de estar en el aire.</summary>
        public event Action Landed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Escondido (controlador desactivado): no moverse
            if (!controller.enabled) return;

            Vector2 input = CanMove ? ReadMoveInput() : Vector2.zero;

            IsMoving = input.sqrMagnitude > 0.01f;
            IsRunning = IsMoving && IsShiftPressed();

            // Dirección relativa a hacia dónde mira el jugador
            float targetSpeed = IsRunning ? runSpeed : walkSpeed;
            Vector3 desired = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, acceleration * Time.deltaTime);

            bool grounded = controller.isGrounded;
            // Solo cuenta como aterrizaje si estuvo un rato en el aire (no por escalones)
            if (grounded && !wasGrounded && airTime > 0.25f) Landed?.Invoke();
            airTime = grounded ? 0f : airTime + Time.deltaTime;
            wasGrounded = grounded;

            // Gravedad: pegado al suelo cuando está apoyado
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            // Salto: v = raíz(2 * g * h)
            if (CanMove && grounded && IsJumpPressed())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Jumped?.Invoke();
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        /// <summary>Lee WASD y devuelve un vector de magnitud máxima 1.</summary>
        private static Vector2 ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed) v.x -= 1f;
            return Vector2.ClampMagnitude(v, 1f);
        }

        private static bool IsJumpPressed()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
        }

        private static bool IsShiftPressed()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }
    }
}
