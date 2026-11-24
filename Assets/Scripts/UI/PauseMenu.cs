using HauntedFactory.Core;
using HauntedFactory.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Menú de pausa con Esc: congela el tiempo y el audio, libera el cursor,
    /// muestra el progreso (llaves, pilas, tiempo), los controles y un consejo al azar.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("Progreso")]
        [SerializeField] private Text keysValue;
        [SerializeField] private Text batteriesValue;
        [SerializeField] private Text timeValue;
        [SerializeField] private Text tipText;
        [SerializeField] private string[] tips =
        {
            "Ilumina a un juguete para congelarlo... pero la batería no dura para siempre.",
            "Si oyes un tic-tac, hay un juguete cerca. Apaga la linterna y escóndete.",
            "Las llaves y las pilas brillan cuando las alumbras.",
            "Correr es más rápido, pero los juguetes te oyen a través de las paredes.",
            "Patear una pelota hace ruido: úsala para distraerlos... o no.",
            "Dentro de un armario no pueden verte. Espera a que se alejen.",
        };

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
            if (paused) RefreshInfo();
            if (pausePanel != null) pausePanel.SetActive(paused);

            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            PlayerLook.SetCursorLocked(!paused);
            if (GameManager.Instance != null) GameManager.Instance.SetPlayerControl(!paused);
        }

        private void RefreshInfo()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (keysValue != null) keysValue.text = gm.KeysCollected + " / " + gm.RequiredKeys;
                if (batteriesValue != null) batteriesValue.text = gm.BatteriesCollected.ToString();
                if (timeValue != null)
                {
                    float t = gm.ElapsedTime;
                    timeValue.text = Mathf.FloorToInt(t / 60f).ToString("00") + ":" + Mathf.FloorToInt(t % 60f).ToString("00");
                }
            }
            if (tipText != null && tips != null && tips.Length > 0)
            {
                tipText.text = "Consejo: " + tips[Random.Range(0, tips.Length)];
            }
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
