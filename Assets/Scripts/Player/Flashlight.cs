using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Linterna con batería. Tecla F para encender/apagar.
    /// La batería se gasta mientras está encendida y parpadea cuando queda poca.
    /// Las pilas recolectables llaman a AddBattery().
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        [Header("Batería")]
        [SerializeField] private float maxBattery = 100f;
        [SerializeField] private float startBattery = 100f;
        [Tooltip("Unidades de batería que se gastan por segundo con la linterna encendida.")]
        [SerializeField] private float drainPerSecond = 2.2f;
        [SerializeField] private bool startOn = true;

        [Header("Parpadeo con batería baja")]
        [Tooltip("Por debajo de este porcentaje (0-1) la luz empieza a parpadear.")]
        [Range(0f, 1f)]
        [SerializeField] private float lowBatteryThreshold = 0.2f;
        [Tooltip("Probabilidad por frame de un parpadeo fuerte con batería baja.")]
        [Range(0f, 1f)]
        [SerializeField] private float flickerChance = 0.08f;

        [Header("Sonido (opcional)")]
        [SerializeField] private AudioClip toggleSound;

        private Light lightSource;
        private AudioSource audioSource;
        private float baseIntensity;
        private float battery;
        private bool isOn;
        private float currentFactor = 1f;

        /// <summary>Batería normalizada entre 0 y 1 (para el HUD).</summary>
        public float Battery01 => maxBattery > 0f ? battery / maxBattery : 0f;

        public bool IsOn => isOn;

        /// <summary>
        /// Qué tan fuerte alumbra ahora (0 a 1). Con batería baja baja y parpadea,
        /// así que los juguetes pueden "escaparse" de la luz en los parpadeos.
        /// </summary>
        public float CurrentStrength => isOn ? currentFactor : 0f;

        /// <summary>Componente Light de la linterna (alcance y ángulo del haz).</summary>
        public Light LightSource => lightSource;

        /// <summary>Permite bloquear la tecla F (menús, game over).</summary>
        public bool CanUseInput { get; set; } = true;

        /// <summary>Se dispara cuando cambia la batería; envía el valor 0-1.</summary>
        public event Action<float> BatteryChanged;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            audioSource = GetComponent<AudioSource>();
            baseIntensity = lightSource.intensity;
            battery = Mathf.Clamp(startBattery, 0f, maxBattery);
            SetOn(startOn && battery > 0f);
        }

        private void Start()
        {
            BatteryChanged?.Invoke(Battery01);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (CanUseInput && kb != null && kb.fKey.wasPressedThisFrame)
            {
                Toggle();
            }

            if (!isOn) return;

            battery -= drainPerSecond * Time.deltaTime;
            if (battery <= 0f)
            {
                battery = 0f;
                SetOn(false);
            }
            BatteryChanged?.Invoke(Battery01);
            UpdateFlicker();
        }

        /// <summary>Enciende o apaga la linterna (no enciende si no hay batería).</summary>
        public void Toggle()
        {
            PlayToggleSound();
            if (!isOn && battery <= 0f) return;
            SetOn(!isOn);
        }

        /// <summary>Apaga la linterna (por ejemplo al esconderse).</summary>
        public void TurnOff()
        {
            if (isOn) SetOn(false);
        }

        /// <summary>
        /// Recarga la batería. Devuelve false si ya estaba llena
        /// (así la pila no se consume en vano).
        /// </summary>
        public bool AddBattery(float amount)
        {
            if (battery >= maxBattery) return false;
            battery = Mathf.Min(maxBattery, battery + amount);
            BatteryChanged?.Invoke(Battery01);
            return true;
        }

        private void SetOn(bool on)
        {
            isOn = on;
            lightSource.enabled = on;
            lightSource.intensity = baseIntensity;
            currentFactor = 1f;
        }

        /// <summary>Con poca batería la luz se debilita y da tirones aleatorios.</summary>
        private void UpdateFlicker()
        {
            float factor = 1f;
            if (Battery01 < lowBatteryThreshold)
            {
                factor = UnityEngine.Random.value < flickerChance
                    ? UnityEngine.Random.Range(0.05f, 0.5f)
                    : Mathf.Lerp(0.45f, 1f, Battery01 / lowBatteryThreshold);
            }
            currentFactor = factor;
            lightSource.intensity = baseIntensity * factor;
        }

        private void PlayToggleSound()
        {
            if (toggleSound == null) return;
            if (audioSource != null) audioSource.PlayOneShot(toggleSound);
            else AudioSource.PlayClipAtPoint(toggleSound, transform.position);
        }
    }
}
