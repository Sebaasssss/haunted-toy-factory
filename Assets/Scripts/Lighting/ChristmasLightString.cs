using System.Collections;
using UnityEngine;

namespace HauntedFactory.Lighting
{
    /// <summary>
    /// Guirnalda de foquitos navideños "embrujada": los focos cambian de color
    /// en secuencia y de vez en cuando toda la guirnalda falla y parpadea.
    /// Los focos usan un MaterialPropertyBlock (no se duplican materiales).
    /// </summary>
    public class ChristmasLightString : MonoBehaviour
    {
        public enum Pattern { Chase, Alternate, Random }

        [Header("Referencias")]
        [SerializeField] private Renderer[] bulbs;
        [Tooltip("Luces reales (pocas) que iluminan el entorno con el color de la guirnalda.")]
        [SerializeField] private Light[] lights;

        [Header("Comportamiento")]
        [SerializeField] private Pattern pattern = Pattern.Chase;
        [SerializeField] private Color[] palette =
        {
            new Color(1f, 0.1f, 0.1f), new Color(0.1f, 1f, 0.25f),
            new Color(1f, 0.75f, 0.15f), new Color(0.2f, 0.4f, 1f)
        };
        [SerializeField] private float stepInterval = 0.5f;
        [SerializeField] private float emissionIntensity = 3f;
        [Tooltip("Probabilidad (por paso) de que la guirnalda falle y parpadee.")]
        [Range(0f, 1f)]
        [SerializeField] private float malfunctionChance = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock block;
        private float[] lightBaseIntensity;
        private int step;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (bulbs == null) bulbs = new Renderer[0];
            lightBaseIntensity = new float[lights != null ? lights.Length : 0];
            for (int i = 0; i < lightBaseIntensity.Length; i++)
            {
                lightBaseIntensity[i] = lights[i] != null ? lights[i].intensity : 1f;
            }
            // Desfase aleatorio para que no todas las guirnaldas vayan iguales
            step = Random.Range(0, 100);
        }

        private void OnEnable()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (true)
            {
                if (Random.value < malfunctionChance)
                {
                    yield return Malfunction();
                }
                ApplyStep(1f);
                step++;
                yield return new WaitForSeconds(stepInterval);
            }
        }

        /// <summary>Falla: parpadeos rápidos y un apagón corto.</summary>
        private IEnumerator Malfunction()
        {
            int flickers = Random.Range(3, 7);
            for (int i = 0; i < flickers; i++)
            {
                ApplyStep(0f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                ApplyStep(Random.Range(0.3f, 1f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
            }
            ApplyStep(0f);
            yield return new WaitForSeconds(Random.Range(0.4f, 1.5f));
        }

        private void ApplyStep(float brightness)
        {
            if (palette == null || palette.Length == 0) return;

            for (int i = 0; i < bulbs.Length; i++)
            {
                if (bulbs[i] == null) continue;
                Color c = palette[i % palette.Length];
                bool on = IsBulbOn(i) && brightness > 0f;
                block.SetColor(BaseColorId, on ? c : c * 0.15f);
                block.SetColor(EmissionColorId, on ? c * emissionIntensity * brightness : Color.black);
                bulbs[i].SetPropertyBlock(block);
            }

            for (int i = 0; i < lightBaseIntensity.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].color = palette[(step + i) % palette.Length];
                lights[i].intensity = lightBaseIntensity[i] * brightness;
            }
        }

        private bool IsBulbOn(int index)
        {
            switch (pattern)
            {
                case Pattern.Chase: return (index + step) % 3 != 0;
                case Pattern.Alternate: return (index + step) % 2 == 0;
                default: return Random.value > 0.3f;
            }
        }
    }
}
