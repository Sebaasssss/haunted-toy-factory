using System;
using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.Core
{
    /// <summary>Estados posibles de la partida.</summary>
    public enum GameState { Playing, Won, Lost }

    /// <summary>
    /// Controla el estado de la partida: llaves recolectadas, victoria y derrota.
    /// Hay una sola instancia por escena (GameManager.Instance).
    /// La UI (Fase 4) se suscribe a sus eventos para mostrar HUD y pantallas finales.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Objetivo")]
        [SerializeField] private int requiredKeys = 3;

        [Header("Referencias del jugador (las asigna el constructor de escena)")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private Flashlight flashlight;

        [Header("Sonidos (opcionales)")]
        [SerializeField] private AudioClip keySound;
        [SerializeField] private AudioClip winSound;
        [SerializeField] private AudioClip loseSound;

        private int keysCollected;
        private float startTime;
        private float endTime = -1f;

        public GameState State { get; private set; } = GameState.Playing;
        public int KeysCollected => keysCollected;
        public int RequiredKeys => requiredKeys;
        public bool HasAllKeys => keysCollected >= requiredKeys;

        /// <summary>Segundos de partida (se congela al terminar).</summary>
        public float ElapsedTime => (endTime >= 0f ? endTime : Time.time) - startTime;

        /// <summary>(llaves actuales, llaves necesarias)</summary>
        public event Action<int, int> KeysChanged;
        /// <summary>Mensaje corto para mostrar en pantalla (puerta cerrada, etc.).</summary>
        public event Action<string> MessageShown;
        public event Action GameWon;
        public event Action GameLost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Había más de un GameManager en la escena; se destruye el duplicado.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            startTime = Time.time;
            KeysChanged?.Invoke(keysCollected, requiredKeys);
            // Un poco después, para que el HUD ya esté suscrito
            Invoke(nameof(ShowIntroMessage), 0.5f);
            Invoke(nameof(ShowFlashlightHint), 5f);
        }

        private void ShowFlashlightHint()
        {
            if (State == GameState.Playing)
            {
                ShowMessage("Los juguetes no se mueven si los iluminas... pero la luz los atrae.");
            }
        }

        private void ShowIntroMessage()
        {
            ShowMessage("Encuentra las " + requiredKeys + " llaves y escapa de la fábrica.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Suma una llave. Lo llaman las llaves recolectables.</summary>
        public void AddKey()
        {
            if (State != GameState.Playing) return;

            keysCollected = Mathf.Min(keysCollected + 1, requiredKeys);
            PlaySound(keySound);
            KeysChanged?.Invoke(keysCollected, requiredKeys);

            ShowMessage(HasAllKeys
                ? "¡Tienes todas las llaves! Corre a la puerta de salida."
                : "Llave " + keysCollected + " de " + requiredKeys);
        }

        public void ShowMessage(string message)
        {
            Debug.Log("[Juego] " + message);
            MessageShown?.Invoke(message);
        }

        /// <summary>El jugador escapó por la salida.</summary>
        public void Win()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
            PlaySound(winSound);
            EndGame();
            Debug.Log("[Juego] ¡VICTORIA! El elfo escapó.");
            GameWon?.Invoke();
        }

        /// <summary>Un juguete poseído atrapó al jugador.</summary>
        public void Lose()
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            PlaySound(loseSound);
            EndGame();
            Debug.Log("[Juego] GAME OVER: te atraparon.");
            GameLost?.Invoke();
        }

        /// <summary>Recarga la escena actual desde cero.</summary>
        public void RestartLevel()
        {
            SceneLoader.ReloadCurrent();
        }

        /// <summary>Activa o bloquea el control del jugador (movimiento, cámara y linterna).</summary>
        public void SetPlayerControl(bool canControl)
        {
            if (playerMovement != null) playerMovement.CanMove = canControl;
            if (playerLook != null) playerLook.CanLook = canControl;
            if (flashlight != null) flashlight.CanUseInput = canControl;
        }

        /// <summary>Bloquea al jugador, libera el cursor y congela el juego.</summary>
        private void EndGame()
        {
            endTime = Time.time;
            SetPlayerControl(false);
            PlayerLook.SetCursorLocked(false);
            Time.timeScale = 0f;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            Vector3 pos = playerMovement != null ? playerMovement.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }
}
