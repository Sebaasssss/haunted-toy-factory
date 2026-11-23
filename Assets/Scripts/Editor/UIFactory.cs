using HauntedFactory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Ayudantes para crear UI de uGUI desde código de editor
    /// (canvas, textos, botones, paneles) con un estilo común.
    /// Usa la fuente integrada de Unity para no depender de TextMeshPro.
    /// </summary>
    internal static class UIFactory
    {
        public static readonly Color Cream = new Color(0.95f, 0.9f, 0.8f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public static readonly Color BloodRed = new Color(0.85f, 0.1f, 0.1f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static readonly Color ButtonNormal = new Color(0.4f, 0.05f, 0.06f, 0.95f);
        private static readonly Color ButtonHighlight = new Color(0.65f, 0.12f, 0.1f, 1f);
        private static readonly Color ButtonPressed = new Color(0.25f, 0.02f, 0.02f, 1f);

        private static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>EventSystem compatible con el Input System nuevo.</summary>
        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Sin acciones asignadas, el módulo usa las acciones UI por defecto al activarse (OnEnable).
            // No se llama a AssignDefaultActions() aquí porque crearía un asset temporal que no se guarda.
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Hace que el rect ocupe todo su padre.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Coloca el rect anclado a un punto del padre.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        /// <summary>Panel a pantalla completa (fondo semitransparente que bloquea clics).</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image CreateImage(string name, Transform parent, Color color,
                                        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, anchor, pivot, position, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, Color color,
                                      TextAnchor alignment, Vector2 anchor, Vector2 pivot, Vector2 position,
                                      Vector2 size, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, anchor, pivot, position, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Texto centrado en un punto (atajo para títulos).</summary>
        public static Text CreateCenteredText(string name, Transform parent, string content, int fontSize,
                                              Color color, Vector2 position, Vector2 size,
                                              FontStyle style = FontStyle.Normal)
        {
            return CreateText(name, parent, content, fontSize, color, TextAnchor.MiddleCenter,
                              Center, Center, position, size, style);
        }

        /// <summary>Sonido corto al pasar el mouse por un botón.</summary>
        public static AudioClip HoverClip =>
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Placeholder/sfx_linterna_click.wav");

        /// <summary>
        /// Botón "de texto" para el menú principal: sin fondo, alineado a la izquierda,
        /// con efecto al pasar el mouse (color, desplazamiento y "» ").
        /// </summary>
        public static Button CreateMenuButton(string name, Transform parent, string label, Vector2 anchor,
                                              Vector2 position, Vector2 size, int fontSize = 46)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, anchor, new Vector2(0f, 0.5f), position, size);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // invisible pero recibe clics

            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;

            Text text = CreateText("Texto", rt, label, fontSize, Cream, TextAnchor.MiddleLeft,
                                   new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, size, FontStyle.Bold);
            AddShadow(text, new Color(0f, 0f, 0f, 0.8f), new Vector2(3f, -3f));
            AddButtonFX(rt.gameObject, text);
            return button;
        }

        public static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void AddButtonFX(GameObject go, Text label)
        {
            var fx = go.AddComponent<MenuButtonFX>();
            Wire(fx, "label", label);
            Wire(fx, "hoverSound", HoverClip);
        }

        /// <summary>Carga una textura como Sprite (cambia su importador si hace falta).</summary>
        public static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[UIFactory] No se encontró la textura " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.alphaIsTransparency == false)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 position,
                                          Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, Center, Center, position, size);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white; // el color real lo da el ColorBlock del botón

            var button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHighlight;
            colors.selectedColor = ButtonHighlight;
            colors.pressedColor = ButtonPressed;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.targetGraphic = img;

            AddOutline(img, new Color(1f, 0.75f, 0.3f, 0.35f), new Vector2(2f, 2f));
            Text text = CreateCenteredText("Texto", rt, label, 34, Cream, Vector2.zero, size, FontStyle.Bold);
            AddButtonFX(rt.gameObject, text);
            return button;
        }

        /// <summary>Asigna un campo privado [SerializeField] de un componente.</summary>
        public static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("[UIFactory] El campo '" + field + "' no existe en " + target.GetType().Name);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
