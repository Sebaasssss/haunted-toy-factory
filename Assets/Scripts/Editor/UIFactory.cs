using System.Collections.Generic;
using HauntedFactory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Ayudantes para crear UI de uGUI desde código de editor con un estilo común:
    /// paneles redondeados (sprites 9-slice de Assets/Art/UI), botones, teclas dibujadas,
    /// íconos y textos. Usa la fuente integrada de Unity (no depende de TextMeshPro).
    /// </summary>
    internal static class UIFactory
    {
        public static readonly Color Cream = new Color(0.95f, 0.9f, 0.8f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public static readonly Color BloodRed = new Color(0.85f, 0.1f, 0.1f);
        public static readonly Color PanelDark = new Color(0.04f, 0.02f, 0.03f, 0.88f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public enum ButtonStyle { Primary, Secondary }

        private const string UiFolder = "Assets/Art/UI/";
        private static readonly Vector4 RoundBorder = new Vector4(20f, 20f, 20f, 20f);

        // ---------- Sprites ----------
        public static Sprite Rounded => LoadSprite(UiFolder + "UI_Redondeado.png", RoundBorder);
        public static Sprite Frame => LoadSprite(UiFolder + "UI_Marco.png", RoundBorder);
        public static Sprite Keycap => LoadSprite(UiFolder + "UI_Tecla.png", new Vector4(16f, 16f, 16f, 16f));
        public static Sprite KeyIcon => LoadSprite(UiFolder + "UI_Llave.png", Vector4.zero);
        public static Sprite FlashlightIcon => LoadSprite(UiFolder + "UI_Linterna.png", Vector4.zero);
        public static Sprite BatteryIcon => LoadSprite(UiFolder + "UI_Pila.png", Vector4.zero);
        public static Sprite ClockIcon => LoadSprite(UiFolder + "UI_Reloj.png", Vector4.zero);
        public static Sprite Vignette => LoadSprite(UiFolder + "UI_Vineta.png", Vector4.zero);
        public static Sprite Glow => LoadSprite(UiFolder + "UI_Brillo.png", Vector4.zero);
        public static Sprite GradientH => LoadSprite(UiFolder + "T_UI_Degradado.png", Vector4.zero);
        public static Sprite GradientV => LoadSprite(UiFolder + "UI_DegradadoV.png", Vector4.zero);

        /// <summary>Sonido corto al pasar el mouse por un botón.</summary>
        public static AudioClip HoverClip =>
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Placeholder/sfx_linterna_click.wav");

        private static readonly Color ButtonNormal = new Color(0.55f, 0.06f, 0.07f, 1f);
        private static readonly Color ButtonHighlight = new Color(0.8f, 0.13f, 0.1f, 1f);
        private static readonly Color ButtonPressed = new Color(0.35f, 0.03f, 0.03f, 1f);
        private static readonly Color SecondaryNormal = new Color(0.08f, 0.04f, 0.05f, 0.85f);
        private static readonly Color SecondaryHighlight = new Color(0.3f, 0.08f, 0.08f, 0.95f);

        private static Font font;
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ==================================================================
        // Canvas y estructura
        // ==================================================================

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

        /// <summary>Contenedor vacío colocado en un punto del padre.</summary>
        public static RectTransform CreateGroup(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                                Vector2 position, Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, anchor, pivot, position, size);
            return rt;
        }

        // ==================================================================
        // Imágenes y paneles
        // ==================================================================

        /// <summary>Panel a pantalla completa (bloquea clics).</summary>
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

        /// <summary>
        /// Imagen con sprite. Si el sprite tiene bordes 9-slice se dibuja "Sliced";
        /// cornerScale controla el tamaño de las esquinas redondeadas (1 = 20 px).
        /// </summary>
        public static Image CreateSprite(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor,
                                         Vector2 pivot, Vector2 position, Vector2 size, float cornerScale = 1f)
        {
            Image img = CreateImage(name, parent, color, anchor, pivot, position, size);
            ApplySprite(img, sprite, cornerScale);
            // Los íconos (sin bordes 9-slice) conservan su proporción
            img.preserveAspect = sprite != null && sprite.border == Vector4.zero;
            return img;
        }

        /// <summary>Imagen con sprite que ocupa todo su padre (fondos, viñetas, marcos).</summary>
        public static Image CreateStretchedSprite(string name, Transform parent, Sprite sprite, Color color, float cornerScale = 1f)
        {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            ApplySprite(img, sprite, cornerScale);
            return img;
        }

        /// <summary>Tarjeta: fondo redondeado oscuro + marco de color.</summary>
        public static RectTransform CreateCard(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position,
                                               Vector2 size, Color background, Color frame)
        {
            RectTransform card = CreateGroup(name, parent, anchor, pivot, position, size);
            CreateStretchedSprite("Fondo", card, Rounded, background);
            if (frame.a > 0f) CreateStretchedSprite("Marco", card, Frame, frame);
            return card;
        }

        private static void ApplySprite(Image img, Sprite sprite, float cornerScale)
        {
            if (sprite == null) return;
            img.sprite = sprite;
            if (sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, cornerScale);
            }
        }

        /// <summary>Carga una textura como Sprite (ajusta su importador la primera vez).</summary>
        public static Sprite LoadSprite(string path, Vector4 border)
        {
            if (SpriteCache.TryGetValue(path, out Sprite cached) && cached != null) return cached;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[UIFactory] No se encontró la textura " + path);
                return null;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (changed) importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            SpriteCache[path] = sprite;
            return sprite;
        }

        // ==================================================================
        // Texto
        // ==================================================================

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

        // ==================================================================
        // Teclas dibujadas y listas de controles
        // ==================================================================

        /// <summary>Tecla de teclado dibujada (por ejemplo "E" o "ESPACIO"). El ancho se ajusta al texto.</summary>
        public static RectTransform CreateKeycap(string name, Transform parent, string key, Vector2 anchor, Vector2 pivot,
                                                 Vector2 position, float height = 40f, int fontSize = 20)
        {
            float width = Mathf.Max(height, 22f + key.Length * fontSize * 0.62f);
            Image cap = CreateSprite(name, parent, Keycap, new Color(0.95f, 0.92f, 0.86f, 0.95f), anchor, pivot,
                                     position, new Vector2(width, height), 0.6f);
            CreateCenteredText("Tecla", cap.transform, key, fontSize, new Color(0.12f, 0.08f, 0.08f),
                               new Vector2(0f, 2f), new Vector2(width, height), FontStyle.Bold);
            return cap.rectTransform;
        }

        /// <summary>
        /// Lista vertical de controles: tecla dibujada + descripción en cada fila.
        /// Devuelve el contenedor (su esquina superior izquierda es el origen de las filas).
        /// </summary>
        public static RectTransform CreateControlsList(string name, Transform parent, string[,] rows, Vector2 anchor,
                                                       Vector2 pivot, Vector2 position, float width, float rowHeight,
                                                       float keyHeight, int keyFont, int labelFont, Color labelColor,
                                                       bool withBackground)
        {
            int count = rows.GetLength(0);
            const float padding = 14f;
            var size = new Vector2(width, count * rowHeight + (withBackground ? padding * 2f : 0f));
            RectTransform list = CreateGroup(name, parent, anchor, pivot, position, size);
            if (withBackground) CreateStretchedSprite("Fondo", list, Rounded, new Color(0f, 0f, 0f, 0.5f));

            var topLeft = new Vector2(0f, 1f);
            float startX = withBackground ? padding : 0f;
            float startY = withBackground ? -padding : 0f;
            for (int i = 0; i < count; i++)
            {
                float y = startY - i * rowHeight - (rowHeight - keyHeight) * 0.5f;
                RectTransform cap = CreateKeycap("Tecla_" + i, list, rows[i, 0], topLeft, topLeft,
                                                 new Vector2(startX, y), keyHeight, keyFont);
                float labelX = startX + cap.sizeDelta.x + 12f;
                Text label = CreateText("Accion_" + i, list, rows[i, 1], labelFont, labelColor, TextAnchor.MiddleLeft,
                                        topLeft, topLeft, new Vector2(labelX, y), new Vector2(width - labelX, keyHeight));
                AddShadow(label, new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f));
            }
            return list;
        }

        /// <summary>Los controles del juego, en el mismo orden en el HUD y en la pausa.</summary>
        public static string[,] GameControls => new[,]
        {
            { "W A S D", "Moverse" },
            { "SHIFT", "Correr" },
            { "ESPACIO", "Saltar" },
            { "F", "Linterna" },
            { "E", "Esconderse" },
            { "ESC", "Pausa" },
        };

        // ==================================================================
        // Botones
        // ==================================================================

        /// <summary>
        /// Botón "de texto" para menús: sin fondo, alineado a la izquierda,
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

        /// <summary>
        /// Botón redondeado. Primary: relleno rojo. Secondary: fondo oscuro con marco dorado.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, Vector2 position,
                                          Vector2 size, ButtonStyle style = ButtonStyle.Primary)
        {
            RectTransform rt = CreateRect(name, parent);
            Place(rt, Center, Center, position, size);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white; // el color real lo da el ColorBlock del botón
            ApplySprite(img, Rounded, 1.2f);

            var button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            bool primary = style == ButtonStyle.Primary;
            colors.normalColor = primary ? ButtonNormal : SecondaryNormal;
            colors.highlightedColor = primary ? ButtonHighlight : SecondaryHighlight;
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = ButtonPressed;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.targetGraphic = img;

            if (!primary) CreateStretchedSprite("Marco", rt, Frame, new Color(1f, 0.8f, 0.3f, 0.6f), 1.2f);

            Text text = CreateCenteredText("Texto", rt, label, 32, Cream, Vector2.zero, size, FontStyle.Bold);
            AddShadow(text, new Color(0f, 0f, 0f, 0.6f), new Vector2(2f, -2f));
            AddButtonFX(rt.gameObject, text);
            return button;
        }

        private static void AddButtonFX(GameObject go, Text label)
        {
            var fx = go.AddComponent<MenuButtonFX>();
            Wire(fx, "label", label);
            Wire(fx, "hoverSound", HoverClip);
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
