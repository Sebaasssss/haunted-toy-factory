using System.Collections;
using HauntedFactory.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Pantallas finales.
    /// - Victoria: tiempo y pilas recogidas.
    /// - Derrota: primero la cámara mira al juguete (CaughtCamera) y hay un destello;
    ///   después aparece la pantalla (semitransparente, el juguete se sigue viendo detrás)
    ///   con quién te atrapó, tarjetas de estadísticas y una frase.
    /// Todo con tiempo sin escalar (el juego queda congelado).
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
        [SerializeField] private CanvasGroup defeatGroup;
        [SerializeField] private Text defeatCaughtByText;
        [SerializeField] private Text defeatKeysValue;
        [SerializeField] private Text defeatBatteriesValue;
        [SerializeField] private Text defeatTimeValue;
        [SerializeField] private Text defeatQuoteText;
        [SerializeField] private Button defeatRestartButton;
        [SerializeField] private Button defeatMenuButton;
        [Tooltip("Segundos de la secuencia de captura antes de mostrar la pantalla.")]
        [SerializeField] private float defeatDelay = 1.4f;
        [SerializeField] private float fadeDuration = 0.8f;

        [SerializeField] private string[] defeatQuotes =
        {
            "Esta Navidad tienes un lugar en el estante.",
            "Shhh... ahora eres parte de la colección.",
            "Los juguetes nunca olvidan a quien no juega con ellos.",
            "Santa no vendrá por ti esta noche.",
            "¿Por qué te ibas? La fiesta apenas empieza.",
        };

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
            GameManager gm = GameManager.Instance;
            if (victoryTimeText != null && gm != null)
            {
                victoryTimeText.text = "Tiempo: " + FormatTime(gm.ElapsedTime) + "     ·     Pilas recogidas: " + gm.BatteriesCollected;
            }
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }

        private void ShowDefeat()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                string who = string.IsNullOrEmpty(gm.CatcherName) ? "Un juguete" : gm.CatcherName;
                SetText(defeatCaughtByText, who + " te atrapó");
                SetText(defeatKeysValue, gm.KeysCollected + " / " + gm.RequiredKeys);
                SetText(defeatBatteriesValue, gm.BatteriesCollected.ToString());
                SetText(defeatTimeValue, FormatTime(gm.ElapsedTime));
            }
            if (defeatQuotes != null && defeatQuotes.Length > 0)
            {
                SetText(defeatQuoteText, "“" + defeatQuotes[Random.Range(0, defeatQuotes.Length)] + "”");
            }
            StartCoroutine(DefeatSequence());
        }

        /// <summary>Espera la secuencia de captura y luego aparece la pantalla poco a poco.</summary>
        private IEnumerator DefeatSequence()
        {
            yield return new WaitForSecondsRealtime(defeatDelay);
            if (defeatPanel == null) yield break;

            defeatPanel.SetActive(true);
            if (defeatGroup == null) yield break;

            defeatGroup.alpha = 0f;
            defeatGroup.interactable = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
                defeatGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
            defeatGroup.alpha = 1f;
            defeatGroup.interactable = true;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private static string FormatTime(float seconds)
        {
            return Mathf.FloorToInt(seconds / 60f).ToString("00") + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00");
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
