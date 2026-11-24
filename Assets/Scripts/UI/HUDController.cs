using HauntedFactory.Core;
using HauntedFactory.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// HUD durante la partida:
    /// - Linterna: batería en celdas (verde, amarilla, roja), ícono que se apaga
    ///   cuando la linterna está apagada y parpadeo con poca carga.
    /// - Llaves: tres íconos que se iluminan y "saltan" al encontrar cada una.
    /// - Mensajes temporales, aviso de escondite con la tecla E y viñeta al esconderse.
    /// - Ayuda de controles que se desvanece sola después de unos segundos.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Linterna")]
        [SerializeField] private Flashlight flashlight;
        [SerializeField] private Image[] batterySegments;
        [SerializeField] private Image batteryFrame;
        [SerializeField] private Image batteryTerminal;
        [SerializeField] private Image flashlightIcon;
        [SerializeField] private Text batteryText;
        [SerializeField] private Color segmentHigh = new Color(0.45f, 0.95f, 0.5f);
        [SerializeField] private Color segmentMid = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private Color segmentLow = new Color(1f, 0.2f, 0.15f);
        [SerializeField] private Color segmentEmpty = new Color(1f, 1f, 1f, 0.07f);
        [SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 0.55f);
        [Range(0f, 1f)]
        [SerializeField] private float lowThreshold = 0.25f;

        [Header("Llaves")]
        [SerializeField] private Text keysText;
        [SerializeField] private Image[] keyIcons;
        [SerializeField] private Image[] keyGlows;
        [SerializeField] private Color keyMissing = new Color(1f, 1f, 1f, 0.14f);
        [SerializeField] private Color keyFound = new Color(1f, 0.8f, 0.25f);

        [Header("Mensajes")]
        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField] private float messageDuration = 3.5f;
        [SerializeField] private float messageFade = 0.6f;

        [Header("Escondites")]
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject hiddenOverlay;

        [Header("Ayuda de controles")]
        [SerializeField] private CanvasGroup controlsGroup;
        [Tooltip("Segundos que se muestra la ayuda al empezar (luego se ve en la pausa).")]
        [SerializeField] private float controlsVisibleTime = 15f;
        [SerializeField] private float controlsFade = 1.5f;

        private float messageTimer;
        private float battery = 1f;
        private int shownKeys;
        private float[] keyPunch = new float[0];
        private float startTime;

        private void Start()
        {
            startTime = Time.time;
            if (keyIcons != null) keyPunch = new float[keyIcons.Length];

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
                shownKeys = gm.KeysCollected; // sin animación al empezar
                OnKeysChanged(gm.KeysCollected, gm.RequiredKeys);
            }

            if (hiding != null)
            {
                hiding.PromptChanged += OnPromptChanged;
                hiding.HiddenChanged += OnHiddenChanged;
            }

            SetMessageAlpha(0f);
            OnPromptChanged("");
            OnHiddenChanged(false);
        }

        private void OnDestroy()
        {
            if (flashlight != null) flashlight.BatteryChanged -= OnBatteryChanged;
            if (hiding != null)
            {
                hiding.PromptChanged -= OnPromptChanged;
                hiding.HiddenChanged -= OnHiddenChanged;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.KeysChanged -= OnKeysChanged;
                GameManager.Instance.MessageShown -= OnMessageShown;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Mensajes: se desvanecen aunque el juego esté en pausa
            if (messageTimer > 0f)
            {
                messageTimer -= dt;
                SetMessageAlpha(Mathf.Clamp01(messageTimer / messageFade));
            }

            UpdateFlashlightVisuals();
            UpdateKeyAnimations(dt);

            // Ayuda de controles: visible al inicio y luego se desvanece
            if (controlsGroup != null)
            {
                float t = Time.time - startTime - controlsVisibleTime;
                controlsGroup.alpha = t <= 0f ? 1f : Mathf.Clamp01(1f - t / controlsFade);
            }
        }

        // ---------------- Linterna ----------------

        private void UpdateFlashlightVisuals()
        {
            bool on = flashlight != null && flashlight.IsOn;
            if (flashlightIcon != null)
            {
                flashlightIcon.color = on ? new Color(1f, 0.95f, 0.8f, 1f) : new Color(1f, 1f, 1f, 0.28f);
            }

            bool low = battery < lowThreshold;
            float blink = low ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            Color frame = low ? Color.Lerp(frameColor, segmentLow, 0.7f) : frameColor;
            frame.a *= blink;
            if (batteryFrame != null) batteryFrame.color = frame;
            if (batteryTerminal != null) batteryTerminal.color = frame;

            if (low) ApplySegments(blink);
        }

        private void OnBatteryChanged(float value)
        {
            battery = Mathf.Clamp01(value);
            ApplySegments(1f);
            if (batteryText != null)
            {
                batteryText.text = Mathf.CeilToInt(battery * 100f) + "%";
                batteryText.color = battery < lowThreshold ? segmentLow : new Color(0.95f, 0.9f, 0.8f);
            }
        }

        private void ApplySegments(float alphaFactor)
        {
            if (batterySegments == null || batterySegments.Length == 0) return;
            int lit = Mathf.CeilToInt(battery * batterySegments.Length - 0.001f);
            Color color = battery < lowThreshold ? segmentLow : (battery < 0.5f ? segmentMid : segmentHigh);
            color.a *= alphaFactor;
            for (int i = 0; i < batterySegments.Length; i++)
            {
                if (batterySegments[i] != null) batterySegments[i].color = i < lit ? color : segmentEmpty;
            }
        }

        // ---------------- Llaves ----------------

        private void OnKeysChanged(int collected, int required)
        {
            if (keysText != null) keysText.text = collected + " / " + required;
            if (keyIcons == null) return;

            for (int i = 0; i < keyIcons.Length; i++)
            {
                bool found = i < collected;
                if (keyIcons[i] != null) keyIcons[i].color = found ? keyFound : keyMissing;
                if (keyGlows != null && i < keyGlows.Length && keyGlows[i] != null) keyGlows[i].gameObject.SetActive(found);
                if (found && i >= shownKeys && i < keyPunch.Length) keyPunch[i] = 1f; // recién encontrada
            }
            shownKeys = collected;
        }

        private void UpdateKeyAnimations(float dt)
        {
            if (keyIcons == null) return;
            for (int i = 0; i < keyIcons.Length && i < keyPunch.Length; i++)
            {
                if (keyIcons[i] == null) continue;
                float scale = 1f;
                if (keyPunch[i] > 0f)
                {
                    keyPunch[i] = Mathf.Max(0f, keyPunch[i] - dt * 2f);
                    scale = 1f + Mathf.Sin((1f - keyPunch[i]) * Mathf.PI) * 0.5f;
                }
                keyIcons[i].rectTransform.localScale = Vector3.one * scale;

                if (keyGlows != null && i < keyGlows.Length && keyGlows[i] != null && keyGlows[i].gameObject.activeSelf)
                {
                    Color g = keyGlows[i].color;
                    g.a = 0.3f + 0.15f * Mathf.Sin(Time.unscaledTime * 2.5f + i);
                    keyGlows[i].color = g;
                }
            }
        }

        // ---------------- Mensajes y escondites ----------------

        private void OnMessageShown(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageTimer = messageDuration;
            SetMessageAlpha(1f);
        }

        private void SetMessageAlpha(float alpha)
        {
            if (messageGroup != null) messageGroup.alpha = alpha;
        }

        private void OnPromptChanged(string prompt)
        {
            bool show = !string.IsNullOrEmpty(prompt);
            if (promptRoot != null) promptRoot.SetActive(show);
            if (promptText != null && show)
            {
                // La tecla [E] se dibuja aparte como una tecla
                promptText.text = prompt.StartsWith("[E] ") ? prompt.Substring(4) : prompt;
            }
        }

        private void OnHiddenChanged(bool hidden)
        {
            if (hiddenOverlay != null) hiddenOverlay.SetActive(hidden);
        }
    }
}
