using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.Items
{
    /// <summary>
    /// Hace que una llave o una pila casi no se vea a oscuras, pero que brille,
    /// palpite y suene un destello cuando el haz de la linterna la alcanza.
    /// Así la linterna sirve para buscar objetos.
    /// </summary>
    public class FlashlightReveal : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Light glow;
        [SerializeField] private Color emissionColor = new Color(1f, 0.8f, 0.25f);

        [Header("Intensidad")]
        [SerializeField] private float hiddenEmission = 0.08f;
        [SerializeField] private float revealedEmission = 4f;
        [SerializeField] private float hiddenLight = 0.15f;
        [SerializeField] private float revealedLight = 2.5f;

        [Header("Detección")]
        [Tooltip("Multiplica el alcance de la linterna para detectar el objeto.")]
        [SerializeField] private float rangeMultiplier = 1.3f;
        [SerializeField] private float fadeSpeed = 4f;
        [SerializeField] private float pulseScale = 0.15f;

        [Header("Sonido")]
        [SerializeField] private AudioClip revealSound;
        [SerializeField] private float soundCooldown = 4f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock block;
        private Flashlight flashlight;
        private Vector3 baseScale;
        private float reveal;
        private float lastSoundTime = -100f;

        private void Start()
        {
            block = new MaterialPropertyBlock();
            baseScale = transform.localScale;
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) flashlight = player.GetComponentInChildren<Flashlight>();
            Apply(0f);
        }

        private void Update()
        {
            bool lit = IsLit();
            reveal = Mathf.MoveTowards(reveal, lit ? 1f : 0f, Time.deltaTime * fadeSpeed);
            Apply(reveal);

            if (lit && revealSound != null && Time.time - lastSoundTime > soundCooldown)
            {
                lastSoundTime = Time.time;
                AudioSource.PlayClipAtPoint(revealSound, transform.position, 0.7f);
            }
        }

        private void Apply(float amount)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.4f * amount;
            Color emission = emissionColor * Mathf.Lerp(hiddenEmission, revealedEmission * pulse, amount);
            if (renderers != null)
            {
                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor(EmissionId, emission);
                    r.SetPropertyBlock(block);
                }
            }
            if (glow != null) glow.intensity = Mathf.Lerp(hiddenLight, revealedLight * pulse, amount);
            transform.localScale = baseScale * (1f + amount * pulseScale * Mathf.Abs(Mathf.Sin(Time.time * 4f)));
        }

        private bool IsLit()
        {
            if (flashlight == null || flashlight.CurrentStrength < 0.3f) return false;
            Light beam = flashlight.LightSource;
            if (beam == null || !beam.enabled) return false;

            Vector3 from = beam.transform.position;
            Vector3 to = transform.position - from;
            float distance = to.magnitude;
            if (distance > beam.range * rangeMultiplier) return false;
            if (Vector3.Angle(beam.transform.forward, to) > beam.spotAngle * 0.5f + 4f) return false;

            // Línea de visión (los triggers de las llaves/pilas se ignoran)
            if (Physics.Raycast(from, to / distance, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.distance >= distance - 0.4f;
            }
            return true;
        }
    }
}
