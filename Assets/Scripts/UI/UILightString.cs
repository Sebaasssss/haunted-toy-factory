using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Guirnalda de foquitos del menú principal: parpadea en secuencia
    /// y a veces falla, igual que las luces de la fábrica.
    /// Usa las imágenes hijas de este objeto.
    /// </summary>
    public class UILightString : MonoBehaviour
    {
        [SerializeField] private float stepInterval = 0.45f;
        [Range(0f, 1f)]
        [SerializeField] private float malfunctionChance = 0.06f;

        private Image[] bulbs;
        private Color[] baseColors;
        private float timer;
        private int step;
        private float blackout;

        private void Awake()
        {
            bulbs = GetComponentsInChildren<Image>();
            baseColors = new Color[bulbs.Length];
            for (int i = 0; i < bulbs.Length; i++) baseColors[i] = bulbs[i].color;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (blackout > 0f)
            {
                blackout -= dt;
                SetAll(Random.value > 0.7f ? 0.6f : 0.08f);
                return;
            }

            timer -= dt;
            if (timer > 0f) return;
            timer = stepInterval;
            step++;

            if (Random.value < malfunctionChance)
            {
                blackout = Random.Range(0.3f, 1.2f);
                return;
            }

            for (int i = 0; i < bulbs.Length; i++)
            {
                bool on = (i + step) % 3 != 0;
                Color c = baseColors[i];
                c.a = on ? 1f : 0.15f;
                bulbs[i].color = c;
            }
        }

        private void SetAll(float alpha)
        {
            for (int i = 0; i < bulbs.Length; i++)
            {
                Color c = baseColors[i];
                c.a = alpha;
                bulbs[i].color = c;
            }
        }
    }
}
