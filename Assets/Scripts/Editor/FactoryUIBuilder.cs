using HauntedFactory.Player;
using HauntedFactory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Crea la UI de la escena de juego: HUD (linterna, llaves, mensajes, aviso de
    /// escondite, ayuda de controles), menú de pausa, pantallas de victoria y derrota,
    /// y el susto (destello + juguete frente a la cámara).
    /// </summary>
    internal static class FactoryUIBuilder
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 BottomLeft = Vector2.zero;
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 LeftMiddle = new Vector2(0f, 0.5f);
        private static readonly Vector2 RightMiddle = new Vector2(1f, 0.5f);

        public static void Build(GameObject player)
        {
            UIFactory.CreateEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("UI_Juego", 10);
            BuildHUD(canvas.transform, player);
            BuildPauseMenu(canvas.transform);
            BuildEndScreens(canvas.transform);
            BuildJumpScare(canvas.transform, player); // al final: queda encima de todo
        }

        // ==================================================================
        // HUD
        // ==================================================================

        private static void BuildHUD(Transform root, GameObject player)
        {
            RectTransform hud = UIFactory.CreateRect("HUD", root);
            UIFactory.Stretch(hud);
            HUDController controller = hud.gameObject.AddComponent<HUDController>();

            // Viñeta al estar escondido: bordes negros, solo se ve por "la rendija"
            Image hidden = UIFactory.CreateStretchedSprite("Escondido", hud, UIFactory.Vignette, new Color(0f, 0f, 0f, 0.97f));
            Text hiddenText = UIFactory.CreateText("Texto", hidden.transform, "Escondido... no hagas ruido", 26,
                                                   new Color(0.95f, 0.9f, 0.8f, 0.7f), TextAnchor.UpperCenter,
                                                   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                                                   new Vector2(900f, 40f), FontStyle.Italic);
            UIFactory.AddShadow(hiddenText, Color.black, new Vector2(2f, -2f));
            hidden.gameObject.SetActive(false);

            // Sombra suave abajo para que el HUD se lea sobre cualquier fondo
            Image bottomShade = UIFactory.CreateSprite("SombraInferior", hud, UIFactory.GradientV, new Color(0f, 0f, 0f, 0.55f),
                                                       BottomCenter, BottomCenter, Vector2.zero, new Vector2(1920f, 220f));
            bottomShade.preserveAspect = false;
            RectTransform shadeRt = bottomShade.rectTransform;
            shadeRt.anchorMin = Vector2.zero;
            shadeRt.anchorMax = new Vector2(1f, 0f);
            shadeRt.sizeDelta = new Vector2(0f, 220f); // el sprite ya es opaco abajo y transparente arriba

            UIFactory.CreateSprite("Mira", hud, UIFactory.Glow, new Color(1f, 1f, 1f, 0.6f), UIFactory.Center, UIFactory.Center,
                                   Vector2.zero, new Vector2(10f, 10f));

            BuildFlashlightBlock(hud, controller, player);
            BuildKeysBlock(hud, controller);

            // Aviso de escondite: [E] + texto
            RectTransform prompt = UIFactory.CreateCard("Aviso", hud, UIFactory.Center, UIFactory.Center, new Vector2(0f, -120f),
                                                        new Vector2(640f, 64f), new Color(0f, 0f, 0f, 0.6f), new Color(1f, 0.8f, 0.3f, 0.35f));
            UIFactory.CreateKeycap("TeclaE", prompt, "E", LeftMiddle, LeftMiddle, new Vector2(14f, 0f), 42f, 22);
            Text promptText = UIFactory.CreateText("Texto", prompt, "", 28, UIFactory.Gold, TextAnchor.MiddleLeft,
                                                   LeftMiddle, LeftMiddle, new Vector2(72f, 0f), new Vector2(560f, 50f), FontStyle.Bold);
            UIFactory.AddShadow(promptText, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));
            prompt.gameObject.SetActive(false);

            // Mensajes
            RectTransform msg = UIFactory.CreateCard("Mensaje", hud, BottomCenter, BottomCenter, new Vector2(0f, 175f),
                                                     new Vector2(1300f, 64f), new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0f));
            CanvasGroup msgGroup = msg.gameObject.AddComponent<CanvasGroup>();
            msgGroup.alpha = 0f;
            msgGroup.blocksRaycasts = false;
            Text message = UIFactory.CreateCenteredText("Texto", msg, "", 30, UIFactory.Cream, Vector2.zero, new Vector2(1260f, 60f));
            UIFactory.AddShadow(message, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));

            // Solo el recordatorio de la pausa (los controles completos están en la pausa y en el menú)
            RectTransform pauseHint = UIFactory.CreateCard("PistaPausa", hud, BottomRight, BottomRight, new Vector2(-36f, 30f),
                                                           new Vector2(176f, 52f), new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0f));
            UIFactory.CreateKeycap("TeclaEsc", pauseHint, "ESC", LeftMiddle, LeftMiddle, new Vector2(10f, 0f), 34f, 16);
            Text pauseText = UIFactory.CreateText("Texto", pauseHint, "Pausa", 21, new Color(0.95f, 0.9f, 0.8f, 0.85f), TextAnchor.MiddleLeft,
                                                  LeftMiddle, LeftMiddle, new Vector2(84f, 0f), new Vector2(90f, 34f), FontStyle.Bold);
            UIFactory.AddShadow(pauseText, new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f));

            UIFactory.Wire(controller, "messageText", message);
            UIFactory.Wire(controller, "messageGroup", msgGroup);
            UIFactory.Wire(controller, "hiding", player.GetComponent<PlayerHiding>());
            UIFactory.Wire(controller, "promptRoot", prompt.gameObject);
            UIFactory.Wire(controller, "promptText", promptText);
            UIFactory.Wire(controller, "hiddenOverlay", hidden.gameObject);
        }

        /// <summary>Linterna: ícono, título con tecla F, porcentaje y batería en 10 celdas.</summary>
        private static void BuildFlashlightBlock(RectTransform hud, HUDController controller, GameObject player)
        {
            RectTransform block = UIFactory.CreateCard("Linterna", hud, BottomLeft, BottomLeft, new Vector2(36f, 30f),
                                                       new Vector2(440f, 112f), new Color(0f, 0f, 0f, 0.55f), new Color(1f, 1f, 1f, 0.12f));

            Image icon = UIFactory.CreateSprite("Icono", block, UIFactory.FlashlightIcon, Color.white, BottomLeft, BottomLeft,
                                                new Vector2(14f, 22f), new Vector2(72f, 72f));
            Text title = UIFactory.CreateText("Titulo", block, "LINTERNA", 22, UIFactory.Cream, TextAnchor.MiddleLeft,
                                              BottomLeft, BottomLeft, new Vector2(98f, 68f), new Vector2(140f, 30f), FontStyle.Bold);
            UIFactory.AddShadow(title, new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f));
            UIFactory.CreateKeycap("TeclaF", block, "F", BottomLeft, BottomLeft, new Vector2(214f, 70f), 26f, 15);
            Text percent = UIFactory.CreateText("Porcentaje", block, "100%", 22, UIFactory.Cream, TextAnchor.MiddleRight,
                                                BottomLeft, BottomLeft, new Vector2(300f, 68f), new Vector2(100f, 30f), FontStyle.Bold);

            const float frameX = 98f, frameY = 16f, frameW = 290f, frameH = 46f;
            Image frame = UIFactory.CreateSprite("Marco", block, UIFactory.Frame, new Color(1f, 1f, 1f, 0.55f), BottomLeft, BottomLeft,
                                                 new Vector2(frameX, frameY), new Vector2(frameW, frameH), 0.5f);
            Image terminal = UIFactory.CreateSprite("Polo", block, UIFactory.Rounded, new Color(1f, 1f, 1f, 0.55f), BottomLeft, BottomLeft,
                                                    new Vector2(frameX + frameW + 2f, frameY + 13f), new Vector2(10f, 20f), 0.25f);

            const int cells = 10;
            const float pad = 7f, gap = 4f;
            float cellW = (frameW - pad * 2f - gap * (cells - 1)) / cells;
            var segments = new Image[cells];
            for (int i = 0; i < cells; i++)
            {
                segments[i] = UIFactory.CreateSprite("Celda_" + i, block, UIFactory.Rounded, new Color(0.45f, 0.95f, 0.5f), BottomLeft, BottomLeft,
                                                     new Vector2(frameX + pad + i * (cellW + gap), frameY + pad),
                                                     new Vector2(cellW, frameH - pad * 2f), 0.25f);
            }

            UIFactory.Wire(controller, "flashlight", player.GetComponentInChildren<Flashlight>());
            UIFactory.Wire(controller, "batteryFrame", frame);
            UIFactory.Wire(controller, "batteryTerminal", terminal);
            UIFactory.Wire(controller, "flashlightIcon", icon);
            UIFactory.Wire(controller, "batteryText", percent);
            var so = new SerializedObject(controller);
            EnvironmentFactory.SetObjectArray(so.FindProperty("batterySegments"), segments);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Llaves: título, contador y tres íconos que se iluminan al encontrarlas.</summary>
        private static void BuildKeysBlock(RectTransform hud, HUDController controller)
        {
            RectTransform block = UIFactory.CreateCard("Llaves", hud, TopLeft, TopLeft, new Vector2(36f, -30f),
                                                       new Vector2(330f, 136f), new Color(0f, 0f, 0f, 0.55f), new Color(1f, 0.8f, 0.3f, 0.3f));
            Text title = UIFactory.CreateText("Titulo", block, "LLAVES", 22, UIFactory.Gold, TextAnchor.MiddleLeft,
                                              TopLeft, TopLeft, new Vector2(20f, -10f), new Vector2(150f, 30f), FontStyle.Bold);
            UIFactory.AddShadow(title, new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f));
            Text count = UIFactory.CreateText("Contador", block, "0 / 3", 22, UIFactory.Cream, TextAnchor.MiddleRight,
                                              TopLeft, TopLeft, new Vector2(190f, -10f), new Vector2(120f, 30f), FontStyle.Bold);

            var icons = new Image[3];
            var glows = new Image[3];
            for (int i = 0; i < icons.Length; i++)
            {
                var pos = new Vector2(65f + i * 100f, -88f);
                glows[i] = UIFactory.CreateSprite("Brillo_" + i, block, UIFactory.Glow, new Color(1f, 0.75f, 0.2f, 0.4f), TopLeft,
                                                  UIFactory.Center, pos, new Vector2(120f, 120f));
                glows[i].gameObject.SetActive(false);
                icons[i] = UIFactory.CreateSprite("Llave_" + i, block, UIFactory.KeyIcon, new Color(1f, 1f, 1f, 0.14f), TopLeft,
                                                  UIFactory.Center, pos, new Vector2(84f, 84f));
            }

            UIFactory.Wire(controller, "keysText", count);
            var so = new SerializedObject(controller);
            EnvironmentFactory.SetObjectArray(so.FindProperty("keyIcons"), icons);
            EnvironmentFactory.SetObjectArray(so.FindProperty("keyGlows"), glows);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==================================================================
        // Pausa
        // ==================================================================

        private static void BuildPauseMenu(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("MenuPausa", root);
            UIFactory.Stretch(holder);
            PauseMenu pause = holder.gameObject.AddComponent<PauseMenu>();

            Image panel = UIFactory.CreatePanel("Panel", holder, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.CreateStretchedSprite("Vineta", panel.transform, UIFactory.Vignette, new Color(0f, 0f, 0f, 0.9f));
            Image gradient = UIFactory.CreateSprite("Degradado", panel.transform, UIFactory.GradientH, new Color(0f, 0f, 0f, 0.85f),
                                                    LeftMiddle, LeftMiddle, Vector2.zero, new Vector2(1100f, 1080f));
            gradient.preserveAspect = false;
            gradient.rectTransform.anchorMin = Vector2.zero;
            gradient.rectTransform.anchorMax = new Vector2(0f, 1f);
            gradient.rectTransform.sizeDelta = new Vector2(1100f, 0f);

            // ---- Columna izquierda: título y botones ----
            Text title = UIFactory.CreateText("Titulo", panel.transform, "PAUSA", 120, UIFactory.Cream, TextAnchor.MiddleLeft,
                                              LeftMiddle, LeftMiddle, new Vector2(116f, 250f), new Vector2(800f, 150f), FontStyle.Bold);
            UIFactory.AddShadow(title, new Color(0.55f, 0f, 0f, 0.9f), new Vector2(5f, -5f));
            Text subtitle = UIFactory.CreateText("Subtitulo", panel.transform, "Los juguetes también esperan...", 30, UIFactory.Gold,
                                                 TextAnchor.MiddleLeft, LeftMiddle, LeftMiddle, new Vector2(122f, 168f),
                                                 new Vector2(800f, 44f), FontStyle.Italic);
            UIFactory.AddShadow(subtitle, Color.black, new Vector2(2f, -2f));
            UIFactory.CreateImage("Linea", panel.transform, UIFactory.Gold, LeftMiddle, LeftMiddle, new Vector2(124f, 135f), new Vector2(440f, 3f));

            Button resume = UIFactory.CreateMenuButton("Btn_Reanudar", panel.transform, "REANUDAR", LeftMiddle, new Vector2(112f, 50f), new Vector2(560f, 74f), 52);
            Button restart = UIFactory.CreateMenuButton("Btn_Reiniciar", panel.transform, "REINICIAR", LeftMiddle, new Vector2(112f, -30f), new Vector2(560f, 70f));
            Button menu = UIFactory.CreateMenuButton("Btn_Menu", panel.transform, "MENÚ PRINCIPAL", LeftMiddle, new Vector2(112f, -105f), new Vector2(560f, 70f));

            RectTransform escHint = UIFactory.CreateGroup("PistaEsc", panel.transform, LeftMiddle, LeftMiddle, new Vector2(124f, -190f), new Vector2(400f, 36f));
            UIFactory.CreateKeycap("TeclaEsc", escHint, "ESC", LeftMiddle, LeftMiddle, Vector2.zero, 32f, 16);
            UIFactory.CreateText("Texto", escHint, "para continuar", 20, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft,
                                 LeftMiddle, LeftMiddle, new Vector2(78f, 0f), new Vector2(300f, 32f), FontStyle.Italic);

            // ---- Tarjeta derecha: progreso, controles y consejo ----
            RectTransform card = UIFactory.CreateCard("Tarjeta", panel.transform, RightMiddle, RightMiddle, new Vector2(-110f, 0f),
                                                      new Vector2(640f, 700f), new Color(0.05f, 0.02f, 0.03f, 0.92f), new Color(0.85f, 0.1f, 0.1f, 0.55f));
            SectionTitle(card, "PROGRESO", -26f);

            Text keysValue = StatItem(card, "Llaves", UIFactory.KeyIcon, "LLAVES", new Vector2(120f, -115f));
            Text batteriesValue = StatItem(card, "Pilas", UIFactory.BatteryIcon, "PILAS", new Vector2(320f, -115f));
            Text timeValue = StatItem(card, "Tiempo", UIFactory.ClockIcon, "TIEMPO", new Vector2(520f, -115f));

            UIFactory.CreateImage("Separador", card, new Color(0.85f, 0.1f, 0.1f, 0.45f), TopLeft, TopLeft, new Vector2(36f, -250f), new Vector2(568f, 2f));
            SectionTitle(card, "CONTROLES", -270f);
            UIFactory.CreateControlsList("Controles", card, UIFactory.GameControls, TopLeft, TopLeft, new Vector2(40f, -318f),
                                         560f, 44f, 34f, 17, 22, UIFactory.Cream, false);

            Text tip = UIFactory.CreateText("Consejo", card, "", 21, new Color(0.95f, 0.9f, 0.8f, 0.7f), TextAnchor.UpperLeft,
                                            BottomLeft, BottomLeft, new Vector2(36f, 24f), new Vector2(568f, 60f), FontStyle.Italic);

            panel.gameObject.SetActive(false);

            UIFactory.Wire(pause, "pausePanel", panel.gameObject);
            UIFactory.Wire(pause, "resumeButton", resume);
            UIFactory.Wire(pause, "restartButton", restart);
            UIFactory.Wire(pause, "menuButton", menu);
            UIFactory.Wire(pause, "keysValue", keysValue);
            UIFactory.Wire(pause, "batteriesValue", batteriesValue);
            UIFactory.Wire(pause, "timeValue", timeValue);
            UIFactory.Wire(pause, "tipText", tip);
        }

        private static void SectionTitle(RectTransform card, string text, float y)
        {
            Text t = UIFactory.CreateText("Titulo_" + text, card, text, 26, UIFactory.Gold, TextAnchor.MiddleLeft,
                                          TopLeft, TopLeft, new Vector2(36f, y), new Vector2(400f, 36f), FontStyle.Bold);
            UIFactory.AddShadow(t, Color.black, new Vector2(1f, -1f));
        }

        /// <summary>Ícono + valor grande + etiqueta, centrados en "center" (coordenadas desde la esquina sup. izq.).</summary>
        private static Text StatItem(RectTransform card, string name, Sprite icon, string label, Vector2 center)
        {
            UIFactory.CreateSprite("Icono_" + name, card, icon, UIFactory.Gold, TopLeft, UIFactory.Center, center, new Vector2(48f, 48f));
            Text value = UIFactory.CreateText("Valor_" + name, card, "-", 34, UIFactory.Cream, TextAnchor.MiddleCenter,
                                              TopLeft, UIFactory.Center, center + new Vector2(0f, -52f), new Vector2(180f, 44f), FontStyle.Bold);
            UIFactory.CreateText("Etiqueta_" + name, card, label, 17, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter,
                                 TopLeft, UIFactory.Center, center + new Vector2(0f, -86f), new Vector2(180f, 24f), FontStyle.Bold);
            return value;
        }

        // ==================================================================
        // Victoria y derrota
        // ==================================================================

        private static void BuildEndScreens(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("PantallasFinales", root);
            UIFactory.Stretch(holder);
            EndScreenController end = holder.gameObject.AddComponent<EndScreenController>();

            // ---- Victoria ----
            Image win = UIFactory.CreatePanel("Panel_Victoria", holder, new Color(0.02f, 0.05f, 0.12f, 0.92f));
            UIFactory.CreateStretchedSprite("Vineta", win.transform, UIFactory.Vignette, new Color(0f, 0f, 0.02f, 0.9f));
            Text winTitle = UIFactory.CreateCenteredText("Titulo", win.transform, "¡ESCAPASTE!", 130, new Color(0.75f, 0.9f, 1f),
                                                         new Vector2(0f, 260f), new Vector2(1400f, 170f), FontStyle.Bold);
            UIFactory.AddShadow(winTitle, new Color(0f, 0f, 0f, 0.9f), new Vector2(5f, -5f));
            UIFactory.CreateCenteredText("Subtitulo", win.transform,
                                         "Saliste a la nieve justo antes del amanecer.\nEsta Navidad, ningún juguete te llevará a su colección.",
                                         34, UIFactory.Cream, new Vector2(0f, 130f), new Vector2(1400f, 120f));
            Text time = UIFactory.CreateCenteredText("Tiempo", win.transform, "Tiempo: 00:00", 38, UIFactory.Gold,
                                                     new Vector2(0f, 30f), new Vector2(1200f, 60f), FontStyle.Bold);
            Button winRestart = UIFactory.CreateButton("Btn_JugarDeNuevo", win.transform, "JUGAR DE NUEVO", new Vector2(-220f, -110f), new Vector2(400f, 84f));
            Button winMenu = UIFactory.CreateButton("Btn_Menu", win.transform, "MENÚ PRINCIPAL", new Vector2(220f, -110f), new Vector2(400f, 84f),
                                                    UIFactory.ButtonStyle.Secondary);
            win.gameObject.SetActive(false);

            // ---- Derrota (semitransparente: el juguete que te atrapó se ve detrás) ----
            Image lose = UIFactory.CreatePanel("Panel_Derrota", holder, new Color(0.12f, 0f, 0.01f, 0.4f));
            CanvasGroup group = lose.gameObject.AddComponent<CanvasGroup>();

            Image vignette = UIFactory.CreateStretchedSprite("Vineta", lose.transform, UIFactory.Vignette, new Color(0.25f, 0f, 0.02f, 1f));
            vignette.gameObject.AddComponent<UIPulse>();

            Image blood = UIFactory.CreateSprite("Franja_Sangre", lose.transform, UIFactory.GradientV, new Color(0.3f, 0f, 0.02f, 0.85f),
                                                 BottomCenter, BottomCenter, Vector2.zero, new Vector2(1920f, 460f));
            blood.preserveAspect = false;
            RectTransform bloodRt = blood.rectTransform;
            bloodRt.anchorMin = Vector2.zero;
            bloodRt.anchorMax = new Vector2(1f, 0f);
            bloodRt.sizeDelta = new Vector2(0f, 460f);

            BuildRedEyes(lose.transform);

            Text loseTitle = UIFactory.CreateCenteredText("Titulo", lose.transform, "¡TE ATRAPARON!", 140, UIFactory.BloodRed,
                                                          new Vector2(0f, 310f), new Vector2(1700f, 180f), FontStyle.Bold);
            UIFactory.AddOutline(loseTitle, new Color(0.3f, 0f, 0f, 0.8f), new Vector2(4f, 4f));
            UIFactory.AddShadow(loseTitle, Color.black, new Vector2(8f, -8f));
            loseTitle.gameObject.AddComponent<TextFlicker>();
            loseTitle.gameObject.AddComponent<UIShake>();

            Text caughtBy = UIFactory.CreateCenteredText("AtrapadoPor", lose.transform, "Un juguete te atrapó", 44, UIFactory.Cream,
                                                         new Vector2(0f, 200f), new Vector2(1400f, 60f), FontStyle.Italic);
            UIFactory.AddShadow(caughtBy, Color.black, new Vector2(3f, -3f));
            UIFactory.CreateImage("Linea", lose.transform, new Color(0.85f, 0.1f, 0.1f, 0.8f), UIFactory.Center, UIFactory.Center,
                                  new Vector2(0f, 160f), new Vector2(640f, 3f));

            Text keysValue = DefeatTile(lose.transform, "Llaves", UIFactory.KeyIcon, "LLAVES", -280f);
            Text batteriesValue = DefeatTile(lose.transform, "Pilas", UIFactory.BatteryIcon, "PILAS", 0f);
            Text timeValue = DefeatTile(lose.transform, "Tiempo", UIFactory.ClockIcon, "TIEMPO", 280f);

            Text quote = UIFactory.CreateCenteredText("Frase", lose.transform, "", 32, new Color(0.95f, 0.9f, 0.8f, 0.85f),
                                                      new Vector2(0f, -120f), new Vector2(1400f, 50f), FontStyle.Italic);
            UIFactory.AddShadow(quote, Color.black, new Vector2(2f, -2f));

            Button loseRestart = UIFactory.CreateButton("Btn_Reintentar", lose.transform, "REINTENTAR", new Vector2(-220f, -230f), new Vector2(400f, 86f));
            Button loseMenu = UIFactory.CreateButton("Btn_Menu", lose.transform, "MENÚ PRINCIPAL", new Vector2(220f, -230f), new Vector2(400f, 86f),
                                                     UIFactory.ButtonStyle.Secondary);
            lose.gameObject.SetActive(false);

            UIFactory.Wire(end, "victoryPanel", win.gameObject);
            UIFactory.Wire(end, "victoryTimeText", time);
            UIFactory.Wire(end, "victoryRestartButton", winRestart);
            UIFactory.Wire(end, "victoryMenuButton", winMenu);
            UIFactory.Wire(end, "defeatPanel", lose.gameObject);
            UIFactory.Wire(end, "defeatGroup", group);
            UIFactory.Wire(end, "defeatCaughtByText", caughtBy);
            UIFactory.Wire(end, "defeatKeysValue", keysValue);
            UIFactory.Wire(end, "defeatBatteriesValue", batteriesValue);
            UIFactory.Wire(end, "defeatTimeValue", timeValue);
            UIFactory.Wire(end, "defeatQuoteText", quote);
            UIFactory.Wire(end, "defeatRestartButton", loseRestart);
            UIFactory.Wire(end, "defeatMenuButton", loseMenu);
        }

        /// <summary>Tarjeta de estadística de la pantalla de derrota: ícono, valor y etiqueta.</summary>
        private static Text DefeatTile(Transform parent, string name, Sprite icon, string label, float x)
        {
            RectTransform tile = UIFactory.CreateCard("Tarjeta_" + name, parent, UIFactory.Center, UIFactory.Center, new Vector2(x, 25f),
                                                      new Vector2(240f, 170f), new Color(0f, 0f, 0f, 0.7f), new Color(0.85f, 0.1f, 0.1f, 0.6f));
            UIFactory.CreateSprite("Icono", tile, icon, UIFactory.Gold, UIFactory.Center, UIFactory.Center, new Vector2(0f, 42f), new Vector2(54f, 54f));
            Text value = UIFactory.CreateCenteredText("Valor", tile, "-", 46, UIFactory.Cream, new Vector2(0f, -16f), new Vector2(220f, 56f), FontStyle.Bold);
            UIFactory.AddShadow(value, Color.black, new Vector2(2f, -2f));
            UIFactory.CreateCenteredText("Etiqueta", tile, label, 19, new Color(1f, 1f, 1f, 0.6f), new Vector2(0f, -60f), new Vector2(220f, 26f), FontStyle.Bold);
            return value;
        }

        /// <summary>Pares de ojos rojos que parpadean en la oscuridad.</summary>
        private static void BuildRedEyes(Transform parent)
        {
            RectTransform eyes = UIFactory.CreateRect("Ojos", parent);
            UIFactory.Stretch(eyes);
            float[] xs = { -800f, -560f, -330f, 330f, 570f, 810f, -80f, 120f };
            float[] ys = { 120f, 70f, 150f, 95f, 155f, 60f, 40f, 75f };
            for (int i = 0; i < xs.Length; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    UIFactory.CreateSprite("Ojo_" + i + "_" + e, eyes, UIFactory.Glow, new Color(1f, 0.08f, 0.05f, 0.95f), BottomCenter,
                                           UIFactory.Center, new Vector2(xs[i] + (e == 0 ? -15f : 15f), ys[i]), new Vector2(18f, 12f));
                }
            }
            eyes.gameObject.AddComponent<UILightString>();
        }

        // ==================================================================
        // Susto
        // ==================================================================

        /// <summary>
        /// Destello rojo a pantalla completa + dos juguetes (cascanueces y oso) colocados
        /// a medio metro de la cámara, desactivados hasta el momento del susto.
        /// </summary>
        private static void BuildJumpScare(Transform root, GameObject player)
        {
            Image flash = UIFactory.CreatePanel("Susto_Destello", root, new Color(0.8f, 0f, 0f, 0f));
            flash.raycastTarget = false;
            JumpScare scare = flash.gameObject.AddComponent<JumpScare>();
            var audio = flash.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;

            Camera cam = player.GetComponentInChildren<Camera>();
            var models = new Object[2];
            for (int i = 0; i < 2; i++)
            {
                Transform holder = PropFactory.Child(cam.transform, i == 0 ? "Susto_Cascanueces" : "Susto_Oso");
                ToyParts parts = i == 0 ? PropFactory.BuildNutcracker(holder) : PropFactory.BuildPossessedTeddy(holder);
                // Cara a la altura de los ojos de la cámara, mirándola de frente
                holder.localPosition = new Vector3(0f, -parts.EyeHeight, 0.6f);
                holder.localRotation = Quaternion.Euler(0f, 180f, 0f);
                PropFactory.AddEyeGlow(parts, 3f, 2f);
                var faceLight = new GameObject("LuzCara").AddComponent<Light>();
                faceLight.transform.SetParent(holder, false);
                faceLight.transform.localPosition = new Vector3(0f, parts.EyeHeight, 0.45f);
                faceLight.type = LightType.Point;
                faceLight.color = new Color(1f, 0.3f, 0.25f);
                faceLight.intensity = 2.5f;
                faceLight.range = 2f;
                faceLight.shadows = LightShadows.None;
                holder.gameObject.SetActive(false);
                models[i] = holder.gameObject;
            }

            var so = new SerializedObject(scare);
            EnvironmentFactory.SetObjectArray(so.FindProperty("scareModels"), models);
            so.FindProperty("flashOverlay").objectReferenceValue = flash;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.FindProperty("scareClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Placeholder/sfx_susto.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
