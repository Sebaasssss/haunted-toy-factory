using UnityEngine;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Temblor para elementos de UI: fuerte al aparecer y luego pequeños
    /// tirones de vez en cuando (efecto "glitch"). Usa tiempo sin escalar.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIShake : MonoBehaviour
    {
        [SerializeField] private float strongDuration = 0.8f;
        [SerializeField] private float strongAmount = 16f;
        [SerializeField] private float idleAmount = 1.2f;
        [Tooltip("Probabilidad por frame de un tirón fuerte después del temblor inicial.")]
        [SerializeField] private float glitchChance = 0.015f;

        private RectTransform rt;
        private Vector2 basePos;
        private float timer;

        private void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        private void OnEnable()
        {
            timer = strongDuration;
        }

        private void OnDisable()
        {
            if (rt != null) rt.anchoredPosition = basePos;
        }

        private void Update()
        {
            float amount = idleAmount;
            if (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;
                amount = Mathf.Lerp(idleAmount, strongAmount, timer / strongDuration);
            }
            else if (Random.value < glitchChance)
            {
                amount = strongAmount * 0.6f;
            }
            rt.anchoredPosition = basePos + Random.insideUnitCircle * amount;
        }
    }
}
