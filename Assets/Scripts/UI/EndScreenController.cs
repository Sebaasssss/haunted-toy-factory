using HauntedFactory.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Muestra la pantalla de victoria o de derrota cuando el GameManager
    /// termina la partida, con botones para reintentar o ir al menú.
    /// </summary>
    public class EndScreenController : MonoBehaviour
    {
        [Header("Victoria")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryTimeText;
        [SerializeField] private Button victoryRestartButton;
        [SerializeField] private Button victoryMenuButton;

        [Header("Derrota")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Text defeatKeysText;
        [SerializeField] private Button defeatRestartButton;
        [SerializeField] private Button defeatMenuButton;

        private void Awake()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

            AddListener(victoryRestartButton, SceneLoader.ReloadCurrent);
            AddListener(victoryMenuButton, SceneLoader.LoadMainMenu);
            AddListener(defeatRestartButton, SceneLoader.ReloadCurrent);
            AddListener(defeatMenuButton, SceneLoader.LoadMainMenu);
        }

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.GameWon += ShowVictory;
            gm.GameLost += ShowDefeat;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.GameWon -= ShowVictory;
            GameManager.Instance.GameLost -= ShowDefeat;
        }

        private void ShowVictory()
        {
            if (victoryTimeText != null && GameManager.Instance != null)
            {
                float t = GameManager.Instance.ElapsedTime;
                victoryTimeText.text = "Tiempo: " + Mathf.FloorToInt(t / 60f).ToString("00") + ":" +
                                       Mathf.FloorToInt(t % 60f).ToString("00");
            }
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }

        private void ShowDefeat()
        {
            if (defeatKeysText != null && GameManager.Instance != null)
            {
                defeatKeysText.text = "Llaves encontradas: " + GameManager.Instance.KeysCollected +
                                      " / " + GameManager.Instance.RequiredKeys;
            }
            if (defeatPanel != null) defeatPanel.SetActive(true);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
