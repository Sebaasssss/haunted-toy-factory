using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Efecto de botón: al pasar el mouse (o seleccionarlo con teclado/mando)
    /// el texto cambia de color, se desplaza un poco, aparece "» " y suena un clic.
    /// Funciona con el juego pausado (usa tiempo sin escalar).
    /// </summary>
    public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private Color normalColor = new Color(0.95f, 0.9f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.3f);
        [SerializeField] private string hoverPrefix = "» ";
        [SerializeField] private Vector2 hoverOffset = new Vector2(14f, 0f);
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private float speed = 14f;

        private string baseText;
        private Vector2 labelStartPos;
        private bool hovered;

        private void Awake()
        {
            if (label == null) label = GetComponentInChildren<Text>();
            if (label != null)
            {
                baseText = label.text;
                labelStartPos = label.rectTransform.anchoredPosition;
                label.color = normalColor;
            }
        }

        private void OnDisable()
        {
            // Al cerrar un panel, el botón vuelve a su estado normal
            hovered = false;
            transform.localScale = Vector3.one;
            if (label != null)
            {
                label.text = baseText;
                label.color = normalColor;
                label.rectTransform.anchoredPosition = labelStartPos;
            }
        }

        private void Update()
        {
            float t = Mathf.Clamp01(Time.unscaledDeltaTime * speed);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (hovered ? hoverScale : 1f), t);
            if (label == null) return;
            label.color = Color.Lerp(label.color, hovered ? hoverColor : normalColor, t);
            label.rectTransform.anchoredPosition = Vector2.Lerp(label.rectTransform.anchoredPosition,
                labelStartPos + (hovered ? hoverOffset : Vector2.zero), t);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);
        public void OnSelect(BaseEventData eventData) => SetHovered(true);
        public void OnDeselect(BaseEventData eventData) => SetHovered(false);

        private void SetHovered(bool value)
        {
            if (hovered == value) return;
            hovered = value;
            if (label != null) label.text = value ? hoverPrefix + baseText : baseText;

            if (value && hoverSound != null)
            {
                Camera cam = Camera.main;
                Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(hoverSound, pos, 0.4f);
            }
        }
    }
}
