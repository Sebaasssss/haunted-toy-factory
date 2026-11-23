using UnityEngine;

namespace HauntedFactory.Lighting
{
    /// <summary>
    /// Lámpara dañada: la intensidad tiembla con ruido y a ratos se apaga.
    /// Si tiene un Renderer de la pantalla de la lámpara, su brillo la sigue.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float noiseSpeed = 8f;
        [Range(0f, 1f)]
        [SerializeField] private float noiseAmount = 0.35f;
        [Tooltip("Probabilidad por segundo de un apagón breve.")]
        [SerializeField] private float blackoutChancePerSecond = 0.25f;
        [SerializeField] private Vector2 blackoutDuration = new Vector2(0.05f, 0.6f);
        [SerializeField] private Renderer lampRenderer;
        [SerializeField] private Color lampEmission = new Color(1f, 0.8f, 0.55f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Light lightSource;
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private float blackoutTimer;
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            block = new MaterialPropertyBlock();
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float factor;
            if (blackoutTimer > 0f)
            {
                blackoutTimer -= Time.deltaTime;
                factor = 0f;
            }
            else
            {
                if (Random.value < blackoutChancePerSecond * Time.deltaTime)
                {
                    blackoutTimer = Random.Range(blackoutDuration.x, blackoutDuration.y);
                }
                float noise = Mathf.PerlinNoise(seed, Time.time * noiseSpeed);
                factor = 1f - noiseAmount + noise * noiseAmount;
            }

            lightSource.intensity = baseIntensity * factor;

            if (lampRenderer != null)
            {
                block.SetColor(EmissionColorId, lampEmission * (2f * factor));
                lampRenderer.SetPropertyBlock(block);
            }
        }
    }
}
