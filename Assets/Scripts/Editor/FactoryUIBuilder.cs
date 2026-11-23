using HauntedFactory.Player;
using HauntedFactory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Crea la UI de la escena de juego: HUD, aviso de escondite, menú de pausa,
    /// pantallas de victoria y derrota, y el susto (destello + juguete frente a la cámara).
    /// </summary>
    internal static class FactoryUIBuilder
    {
        public static void Build(GameObject player)
        {
            UIFactory.CreateEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("UI_Juego", 10);
            BuildHUD(canvas.transform, player);
            BuildPauseMenu(canvas.transform);
            BuildEndScreens(canvas.transform);
            BuildJumpScare(canvas.transform, player); // al final: queda encima de todo
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------

        private static void BuildHUD(Transform root, GameObject player)
        {
            RectTransform hud = UIFactory.CreateRect("HUD", root);
            UIFactory.Stretch(hud);
            HUDController controller = hud.gameObject.AddComponent<HUDController>();

            Vector2 bottomLeft = Vector2.zero;
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 bottomCenter = new Vector2(0.5f, 0f);
            Vector2 bottomRight = new Vector2(1f, 0f);

            // Viñeta oscura al estar escondido (debajo de todo lo demás del HUD)
            Image hidden = UIFactory.CreatePanel("Escondido", hud, new Color(0f, 0f, 0f, 0.55f));
            hidden.raycastTarget = false;
            Text hiddenText = UIFactory.CreateText("Texto", hidden.transform, "Escondido... no hagas ruido", 26,
                                                   new Color(0.95f, 0.9f, 0.8f, 0.6f), TextAnchor.UpperCenter,
                                                   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                                                   new Vector2(900f, 40f), FontStyle.Italic);
            UIFactory.AddShadow(hiddenText, Color.black, new Vector2(2f, -2f));
            hidden.gameObject.SetActive(false);

            UIFactory.CreateImage("Mira", hud, new Color(1f, 1f, 1f, 0.45f), UIFactory.Center, UIFactory.Center,
                                  Vector2.zero, new Vector2(6f, 6f));

            // Batería
            UIFactory.CreateText("Bateria_Titulo", hud, "LINTERNA [F]", 24, UIFactory.Cream, TextAnchor.LowerLeft,
                                 bottomLeft, bottomLeft, new Vector2(40f, 78f), new Vector2(400f, 30f), FontStyle.Bold);
            Image background = UIFactory.CreateImage("Bateria_Fondo", hud, new Color(0f, 0f, 0f, 0.65f),
                                                     bottomLeft, bottomLeft, new Vector2(40f, 40f), new Vector2(320f, 30f));
            UIFactory.AddOutline(background, new Color(1f, 1f, 1f, 0.25f), new Vector2(1f, 1f));
            RectTransform fillRt = UIFactory.CreateRect("Bateria_Relleno", background.transform);
            UIFactory.Stretch(fillRt);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = new Color(0.95f, 0.85f, 0.3f);
            fill.raycastTarget = false;
            Text percent = UIFactory.CreateCenteredText("Bateria_Porcentaje", background.transform, "100%", 22,
                                                        Color.white, Vector2.zero, new Vector2(320f, 30f), FontStyle.Bold);

            // Llaves: texto + 3 casillas
            Text keys = UIFactory.CreateText("Llaves", hud, "LLAVES  0 / 3", 32, UIFactory.Gold, TextAnchor.UpperLeft,
                                             topLeft, topLeft, new Vector2(40f, -30f), new Vector2(500f, 45f), FontStyle.Bold);
            UIFactory.AddShadow(keys, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));
            var slots = new Image[3];
            for (int i = 0; i < slots.Length; i++)
            {
                Image frame = UIFactory.CreateImage("Casilla_" + i, hud, new Color(0f, 0f, 0f, 0.6f), topLeft, topLeft,
                                                    new Vector2(40f + i * 52f, -82f), new Vector2(44f, 44f));
                UIFactory.AddOutline(frame, new Color(1f, 0.8f, 0.3f, 0.5f), new Vector2(2f, 2f));
                RectTransform inner = UIFactory.CreateRect("Llave", frame.transform);
                UIFactory.Stretch(inner);
                inner.offsetMin = new Vector2(8f, 8f);
                inner.offsetMax = new Vector2(-8f, -8f);
                slots[i] = inner.gameObject.AddComponent<Image>();
                slots[i].color = new Color(1f, 1f, 1f, 0.12f);
                slots[i].raycastTarget = false;
            }

            // Aviso de interacción (esconderse) y mensajes
            Text prompt = UIFactory.CreateText("Aviso_E", hud, "", 30, UIFactory.Gold, TextAnchor.MiddleCenter,
                                               UIFactory.Center, UIFactory.Center, new Vector2(0f, -90f), new Vector2(900f, 50f), FontStyle.Bold);
            UIFactory.AddShadow(prompt, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));
            Text message = UIFactory.CreateText("Mensaje", hud, "", 32, UIFactory.Cream, TextAnchor.MiddleCenter,
                                                bottomCenter, bottomCenter, new Vector2(0f, 150f), new Vector2(1400f, 60f));
            UIFactory.AddShadow(message, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));

            UIFactory.CreateText("Controles", hud, "WASD mover · Espacio saltar · Shift correr · F linterna · E esconderse · Esc pausa", 20,
                                 new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerRight, bottomRight, bottomRight,
                                 new Vector2(-40f, 40f), new Vector2(1100f, 30f));

            UIFactory.Wire(controller, "flashlight", player.GetComponentInChildren<Flashlight>());
            UIFactory.Wire(controller, "batteryFill", fill);
            UIFactory.Wire(controller, "batteryText", percent);
            UIFactory.Wire(controller, "keysText", keys);
            UIFactory.Wire(controller, "messageText", message);
            UIFactory.Wire(controller, "hiding", player.GetComponent<PlayerHiding>());
            UIFactory.Wire(controller, "promptText", prompt);
            UIFactory.Wire(controller, "hiddenOverlay", hidden.gameObject);
            var so = new SerializedObject(controller);
            EnvironmentFactory.SetObjectArray(so.FindProperty("keySlots"), slots);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Pausa
        // ------------------------------------------------------------------

        private static void BuildPauseMenu(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("MenuPausa", root);
            UIFactory.Stretch(holder);
            PauseMenu pause = holder.gameObject.AddComponent<PauseMenu>();

            Image panel = UIFactory.CreatePanel("Panel", holder, new Color(0f, 0f, 0f, 0.82f));
            Text title = UIFactory.CreateCenteredText("Titulo", panel.transform, "PAUSA", 90, UIFactory.Cream,
                                                      new Vector2(0f, 220f), new Vector2(1000f, 120f), FontStyle.Bold);
            UIFactory.AddShadow(title, new Color(0.5f, 0f, 0f, 0.8f), new Vector2(4f, -4f));
            Button resume = UIFactory.CreateButton("Btn_Reanudar", panel.transform, "Reanudar", new Vector2(0f, 40f), new Vector2(400f, 80f));
            Button restart = UIFactory.CreateButton("Btn_Reiniciar", panel.transform, "Reiniciar", new Vector2(0f, -60f), new Vector2(400f, 80f));
            Button menu = UIFactory.CreateButton("Btn_Menu", panel.transform, "Menú principal", new Vector2(0f, -160f), new Vector2(400f, 80f));
            panel.gameObject.SetActive(false);

            UIFactory.Wire(pause, "pausePanel", panel.gameObject);
            UIFactory.Wire(pause, "resumeButton", resume);
            UIFactory.Wire(pause, "restartButton", restart);
            UIFactory.Wire(pause, "menuButton", menu);
        }

        // ------------------------------------------------------------------
        // Victoria y derrota
        // ------------------------------------------------------------------

        private static void BuildEndScreens(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("PantallasFinales", root);
            UIFactory.Stretch(holder);
            EndScreenController end = holder.gameObject.AddComponent<EndScreenController>();

            // ---- Victoria ----
            Image win = UIFactory.CreatePanel("Panel_Victoria", holder, new Color(0.02f, 0.05f, 0.12f, 0.92f));
            Text winTitle = UIFactory.CreateCenteredText("Titulo", win.transform, "¡ESCAPASTE!", 120, new Color(0.75f, 0.9f, 1f),
                                                         new Vector2(0f, 260f), new Vector2(1400f, 160f), FontStyle.Bold);
            UIFactory.AddShadow(winTitle, new Color(0f, 0f, 0f, 0.9f), new Vector2(5f, -5f));
            UIFactory.CreateCenteredText("Subtitulo", win.transform,
                                         "Saliste a la nieve justo antes del amanecer.\nEsta Navidad, ningún juguete te llevará a su colección.",
                                         34, UIFactory.Cream, new Vector2(0f, 130f), new Vector2(1400f, 120f));
            Text time = UIFactory.CreateCenteredText("Tiempo", win.transform, "Tiempo: 00:00", 38, UIFactory.Gold,
                                                     new Vector2(0f, 30f), new Vector2(1200f, 60f), FontStyle.Bold);
            Button winRestart = UIFactory.CreateButton("Btn_JugarDeNuevo", win.transform, "Jugar de nuevo", new Vector2(-230f, -110f), new Vector2(420f, 80f));
            Button winMenu = UIFactory.CreateButton("Btn_Menu", win.transform, "Menú principal", new Vector2(230f, -110f), new Vector2(420f, 80f));
            win.gameObject.SetActive(false);

            // ---- Derrota ----
            Image lose = UIFactory.CreatePanel("Panel_Derrota", holder, new Color(0.07f, 0f, 0.01f, 0.94f));
            CanvasGroup group = lose.gameObject.AddComponent<CanvasGroup>();

            // Franja rojo sangre en la parte baja
            RectTransform bloodRt = UIFactory.CreateRect("Franja_Sangre", lose.transform);
            bloodRt.anchorMin = new Vector2(0f, 0f);
            bloodRt.anchorMax = new Vector2(1f, 0f);
            bloodRt.pivot = new Vector2(0.5f, 0f);
            bloodRt.sizeDelta = new Vector2(0f, 260f);
            bloodRt.anchoredPosition = Vector2.zero;
            var blood = bloodRt.gameObject.AddComponent<Image>();
            blood.color = new Color(0.35f, 0f, 0.02f, 0.55f);
            blood.raycastTarget = false;

            // Fila de ojos rojos mirándote desde la oscuridad (parpadean)
            RectTransform eyes = UIFactory.CreateRect("Ojos", lose.transform);
            UIFactory.Stretch(eyes);
            float[] xs = { -760f, -520f, -300f, 310f, 540f, 780f, -60f };
            float[] ys = { 120f, 70f, 140f, 95f, 150f, 60f, 40f };
            for (int i = 0; i < xs.Length; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    UIFactory.CreateImage("Ojo_" + i + "_" + e, eyes, new Color(1f, 0.08f, 0.05f, 0.9f), new Vector2(0.5f, 0f),
                                          UIFactory.Center, new Vector2(xs[i] + (e == 0 ? -14f : 14f), ys[i]), new Vector2(10f, 6f));
                }
            }
            eyes.gameObject.AddComponent<UILightString>();

            Text loseTitle = UIFactory.CreateCenteredText("Titulo", lose.transform, "¡TE ATRAPARON!", 130, UIFactory.BloodRed,
                                                          new Vector2(0f, 300f), new Vector2(1600f, 170f), FontStyle.Bold);
            UIFactory.AddOutline(loseTitle, new Color(0.3f, 0f, 0f, 0.8f), new Vector2(4f, 4f));
            UIFactory.AddShadow(loseTitle, Color.black, new Vector2(7f, -7f));
            loseTitle.gameObject.AddComponent<TextFlicker>();

            Text caughtBy = UIFactory.CreateCenteredText("AtrapadoPor", lose.transform, "Un juguete te atrapó", 42, UIFactory.Cream,
                                                         new Vector2(0f, 195f), new Vector2(1400f, 60f), FontStyle.Italic);
            UIFactory.CreateImage("Linea", lose.transform, new Color(0.85f, 0.1f, 0.1f, 0.8f), UIFactory.Center, UIFactory.Center,
                                  new Vector2(0f, 155f), new Vector2(640f, 3f));

            Image statsCard = UIFactory.CreateImage("Estadisticas", lose.transform, new Color(0f, 0f, 0f, 0.6f), UIFactory.Center,
                                                    UIFactory.Center, new Vector2(0f, 45f), new Vector2(760f, 170f));
            UIFactory.AddOutline(statsCard, new Color(0.85f, 0.1f, 0.1f, 0.6f), new Vector2(2f, 2f));
            Text stats = UIFactory.CreateCenteredText("Texto", statsCard.transform, "", 34, UIFactory.Gold,
                                                      Vector2.zero, new Vector2(740f, 160f), FontStyle.Bold);

            Text quote = UIFactory.CreateCenteredText("Frase", lose.transform, "", 30, new Color(0.95f, 0.9f, 0.8f, 0.75f),
                                                      new Vector2(0f, -85f), new Vector2(1400f, 50f), FontStyle.Italic);

            Button loseRestart = UIFactory.CreateButton("Btn_Reintentar", lose.transform, "Reintentar", new Vector2(-230f, -200f), new Vector2(420f, 85f));
            Button loseMenu = UIFactory.CreateButton("Btn_Menu", lose.transform, "Menú principal", new Vector2(230f, -200f), new Vector2(420f, 85f));
            lose.gameObject.SetActive(false);

            UIFactory.Wire(end, "victoryPanel", win.gameObject);
            UIFactory.Wire(end, "victoryTimeText", time);
            UIFactory.Wire(end, "victoryRestartButton", winRestart);
            UIFactory.Wire(end, "victoryMenuButton", winMenu);
            UIFactory.Wire(end, "defeatPanel", lose.gameObject);
            UIFactory.Wire(end, "defeatGroup", group);
            UIFactory.Wire(end, "defeatCaughtByText", caughtBy);
            UIFactory.Wire(end, "defeatStatsText", stats);
            UIFactory.Wire(end, "defeatQuoteText", quote);
            UIFactory.Wire(end, "defeatRestartButton", loseRestart);
            UIFactory.Wire(end, "defeatMenuButton", loseMenu);
        }

        // ------------------------------------------------------------------
        // Susto
        // ------------------------------------------------------------------

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
                // Luz roja entre la cámara y la cara para que se vea en la oscuridad
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
