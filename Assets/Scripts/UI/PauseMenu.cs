using HauntedFactory.Core;
using HauntedFactory.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Menú de pausa con Esc: congela el tiempo y el audio, libera el cursor
    /// y ofrece reanudar, reiniciar o volver al menú principal.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private bool isPaused;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(() => SetPaused(false));
            if (restartButton != null) restartButton.onClick.AddListener(SceneLoader.ReloadCurrent);
            if (menuButton != null) menuButton.onClick.AddListener(SceneLoader.LoadMainMenu);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            // No se puede pausar en las pantallas de victoria/derrota
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing) return;

            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (pausePanel != null) pausePanel.SetActive(paused);

            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            PlayerLook.SetCursorLocked(!paused);
            if (GameManager.Instance != null) GameManager.Instance.SetPlayerControl(!paused);
        }

        private void OnDestroy()
        {
            // Por si la escena se descarga estando en pausa
            if (isPaused)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }
    }
}
