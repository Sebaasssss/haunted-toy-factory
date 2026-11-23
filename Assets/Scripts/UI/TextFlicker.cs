using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Parpadeo de neón averiado para textos o imágenes de UI
    /// (título del menú y letreros de la fábrica).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class TextFlicker : MonoBehaviour
    {
        [Tooltip("Probabilidad por segundo de que empiece a fallar.")]
        [SerializeField] private float flickerChancePerSecond = 0.35f;
        [SerializeField] private Vector2 flickerDuration = new Vector2(0.1f, 0.6f);
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.15f;

        private Graphic graphic;
        private Color baseColor;
        private float flickerTimer;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            baseColor = graphic.color;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Color c = baseColor;

            if (flickerTimer > 0f)
            {
                flickerTimer -= dt;
                c.a = baseColor.a * (Random.value > 0.5f ? 1f : minAlpha);
            }
            else if (Random.value < flickerChancePerSecond * dt)
            {
                flickerTimer = Random.Range(flickerDuration.x, flickerDuration.y);
            }
            graphic.color = c;
        }
    }
}
