using HauntedFactory.Core;
using HauntedFactory.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// HUD durante la partida: barra de batería, contador de llaves
    /// y mensajes temporales (puerta cerrada, llave encontrada...).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Flashlight flashlight;
        [Tooltip("Imagen de relleno: su ancho se ajusta con anchorMax.x (0 a 1).")]
        [SerializeField] private Image batteryFill;
        [SerializeField] private Text batteryText;
        [SerializeField] private Text keysText;
        [SerializeField] private Text messageText;

        [Header("Colores de batería")]
        [SerializeField] private Color batteryNormal = new Color(0.95f, 0.85f, 0.3f);
        [SerializeField] private Color batteryLow = new Color(0.9f, 0.15f, 0.1f);
        [Range(0f, 1f)]
        [SerializeField] private float lowThreshold = 0.2f;

        [Header("Mensajes")]
        [SerializeField] private float messageDuration = 3.5f;
        [SerializeField] private float messageFade = 0.6f;

        private float messageTimer;

        private void Start()
        {
            if (flashlight != null)
            {
                flashlight.BatteryChanged += OnBatteryChanged;
                OnBatteryChanged(flashlight.Battery01);
            }

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.KeysChanged += OnKeysChanged;
                gm.MessageShown += OnMessageShown;
                OnKeysChanged(gm.KeysCollected, gm.RequiredKeys);
            }

            SetMessageAlpha(0f);
        }

        private void OnDestroy()
        {
            if (flashlight != null) flashlight.BatteryChanged -= OnBatteryChanged;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.KeysChanged -= OnKeysChanged;
                GameManager.Instance.MessageShown -= OnMessageShown;
            }
        }

        private void Update()
        {
            if (messageTimer <= 0f) return;
            // Tiempo sin escalar: los mensajes se desvanecen aunque el juego esté pausado
            messageTimer -= Time.unscaledDeltaTime;
            SetMessageAlpha(Mathf.Clamp01(messageTimer / messageFade));
        }

        private void OnBatteryChanged(float value)
        {
            if (batteryFill != null)
            {
                RectTransform rt = batteryFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(value), rt.anchorMax.y);
                batteryFill.color = value < lowThreshold ? batteryLow : batteryNormal;
            }
            if (batteryText != null) batteryText.text = Mathf.CeilToInt(value * 100f) + "%";
        }

        private void OnKeysChanged(int collected, int required)
        {
            if (keysText != null) keysText.text = "Llaves: " + collected + " / " + required;
        }

        private void OnMessageShown(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageTimer = messageDuration;
            SetMessageAlpha(1f);
        }

        private void SetMessageAlpha(float alpha)
        {
            if (messageText == null) return;
            Color c = messageText.color;
            c.a = alpha;
            messageText.color = c;
        }
    }
}
