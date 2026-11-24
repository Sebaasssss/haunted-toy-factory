using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>Hace "respirar" la transparencia de un gráfico de UI (tiempo sin escalar).</summary>
    [RequireComponent(typeof(Graphic))]
    public class UIPulse : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float speed = 1.5f;

        private Graphic graphic;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            Color c = graphic.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
            graphic.color = c;
        }
    }
}
