using HauntedFactory.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Menú principal: Jugar, Controles (panel con instrucciones) y Salir.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(SceneLoader.LoadGame);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowControls(true));
            if (backButton != null) backButton.onClick.AddListener(() => ShowControls(false));
            if (quitButton != null) quitButton.onClick.AddListener(SceneLoader.QuitGame);
        }

        private void Start()
        {
            // Al volver de una partida el tiempo y el cursor deben quedar normales
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowControls(false);
        }

        private void ShowControls(bool show)
        {
            if (mainPanel != null) mainPanel.SetActive(!show);
            if (controlsPanel != null) controlsPanel.SetActive(show);
        }
    }
}
