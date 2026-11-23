using System;
using HauntedFactory.Core;
using HauntedFactory.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Esconderse con E en armarios y regalos gigantes.
    /// Escondido: no te mueves, la linterna se apaga, solo puedes mirar un poco
    /// a los lados y los juguetes no pueden detectarte ni atraparte.
    /// </summary>
    public class PlayerHiding : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Flashlight flashlight;
        [SerializeField] private Transform cameraRoot;
        [Tooltip("Cuánto puedes girar la cabeza dentro del escondite (grados a cada lado).")]
        [SerializeField] private float lookLimit = 50f;

        private CharacterController controller;
        private HidingSpot nearby;
        private HidingSpot current;
        private string lastPrompt = "";

        public bool IsHidden => current != null;

        /// <summary>Texto de ayuda para el HUD ("[E] Esconderse", "[E] Salir" o vacío).</summary>
        public event Action<string> PromptChanged;
        public event Action<bool> HiddenChanged;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void SetNearby(HidingSpot spot) => nearby = spot;

        public void ClearNearby(HidingSpot spot)
        {
            if (nearby == spot) nearby = null;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            bool playing = (gm == null || gm.State == GameState.Playing) && Time.timeScale > 0f;
            if (!playing)
            {
                SetPrompt("");
                return;
            }

            if (IsHidden)
            {
                // Asegurar el bloqueo aunque otra cosa (la pausa) lo haya reactivado
                movement.CanMove = false;
                if (flashlight != null) flashlight.CanUseInput = false;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                if (IsHidden) Exit();
                else if (nearby != null && !nearby.Occupied) Enter(nearby);
            }

            if (IsHidden) SetPrompt("[E] Salir");
            else if (nearby != null && !nearby.Occupied) SetPrompt("[E] Esconderse en el " + nearby.DisplayName);
            else SetPrompt("");
        }

        private void LateUpdate()
        {
            if (!IsHidden) return;
            // Limitar el giro: desde el escondite solo se ve por la rendija
            float baseYaw = current.ViewPoint.eulerAngles.y;
            float delta = Mathf.DeltaAngle(baseYaw, transform.eulerAngles.y);
            float clamped = Mathf.Clamp(delta, -lookLimit, lookLimit);
            if (!Mathf.Approximately(delta, clamped))
            {
                transform.rotation = Quaternion.Euler(0f, baseYaw + clamped, 0f);
            }
        }

        private void Enter(HidingSpot spot)
        {
            current = spot;
            spot.Occupied = true;
            spot.PlayDoorSound();

            controller.enabled = false;
            movement.CanMove = false;
            if (flashlight != null)
            {
                flashlight.TurnOff();
                flashlight.CanUseInput = false;
            }

            float eye = cameraRoot != null ? cameraRoot.localPosition.y : 1.45f;
            transform.position = spot.ViewPoint.position - Vector3.up * eye;
            transform.rotation = Quaternion.Euler(0f, spot.ViewPoint.eulerAngles.y, 0f);
            HiddenChanged?.Invoke(true);
        }

        private void Exit()
        {
            HidingSpot spot = current;
            current = null;
            spot.Occupied = false;
            spot.PlayDoorSound();

            transform.position = spot.ExitPoint.position;
            controller.enabled = true;
            movement.CanMove = true;
            if (flashlight != null) flashlight.CanUseInput = true;
            HiddenChanged?.Invoke(false);
        }

        private void SetPrompt(string prompt)
        {
            if (prompt == lastPrompt) return;
            lastPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }
    }
}
