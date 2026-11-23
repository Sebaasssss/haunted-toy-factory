using HauntedFactory.Player;
using HauntedFactory.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Crea la UI de la escena de juego: HUD (batería, llaves, mensajes),
    /// menú de pausa y pantallas de victoria y derrota, ya conectadas a sus scripts.
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
        }

        private static void BuildHUD(Transform root, GameObject player)
        {
            RectTransform hud = UIFactory.CreateRect("HUD", root);
            UIFactory.Stretch(hud);
            HUDController controller = hud.gameObject.AddComponent<HUDController>();

            Vector2 bottomLeft = Vector2.zero;
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 bottomCenter = new Vector2(0.5f, 0f);
            Vector2 bottomRight = new Vector2(1f, 0f);

            // Mira central
            UIFactory.CreateImage("Mira", hud, new Color(1f, 1f, 1f, 0.45f), UIFactory.Center, UIFactory.Center,
                                  Vector2.zero, new Vector2(6f, 6f));

            // Batería (abajo a la izquierda)
            UIFactory.CreateText("Bateria_Titulo", hud, "LINTERNA [F]", 24, UIFactory.Cream, TextAnchor.LowerLeft,
                                 bottomLeft, bottomLeft, new Vector2(40f, 78f), new Vector2(400f, 30f), FontStyle.Bold);
            Image background = UIFactory.CreateImage("Bateria_Fondo", hud, new Color(0f, 0f, 0f, 0.65f),
                                                     bottomLeft, bottomLeft, new Vector2(40f, 40f), new Vector2(320f, 30f));
            RectTransform fillRt = UIFactory.CreateRect("Bateria_Relleno", background.transform);
            UIFactory.Stretch(fillRt);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = new Color(0.95f, 0.85f, 0.3f);
            fill.raycastTarget = false;
            Text percent = UIFactory.CreateCenteredText("Bateria_Porcentaje", background.transform, "100%", 22,
                                                        Color.white, Vector2.zero, new Vector2(320f, 30f), FontStyle.Bold);

            // Llaves (arriba a la izquierda): texto + 3 casillas que se llenan de dorado
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

            // Mensajes (abajo al centro)
            Text message = UIFactory.CreateText("Mensaje", hud, "", 32, UIFactory.Cream, TextAnchor.MiddleCenter,
                                                bottomCenter, bottomCenter, new Vector2(0f, 150f), new Vector2(1400f, 60f));

            // Recordatorio de controles (abajo a la derecha)
            UIFactory.CreateText("Controles", hud, "WASD mover · Espacio saltar · Shift correr · F linterna · Esc pausa", 20,
                                 new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerRight, bottomRight, bottomRight,
                                 new Vector2(-40f, 40f), new Vector2(1000f, 30f));

            UIFactory.Wire(controller, "flashlight", player.GetComponentInChildren<Flashlight>());
            UIFactory.Wire(controller, "batteryFill", fill);
            UIFactory.Wire(controller, "batteryText", percent);
            UIFactory.Wire(controller, "keysText", keys);
            UIFactory.Wire(controller, "messageText", message);
            UIFactory.AddShadow(message, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));

            var so = new UnityEditor.SerializedObject(controller);
            EnvironmentFactory.SetObjectArray(so.FindProperty("keySlots"), slots);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPauseMenu(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("MenuPausa", root);
            UIFactory.Stretch(holder);
            PauseMenu pause = holder.gameObject.AddComponent<PauseMenu>();

            Image panel = UIFactory.CreatePanel("Panel", holder, new Color(0f, 0f, 0f, 0.8f));
            UIFactory.CreateCenteredText("Titulo", panel.transform, "PAUSA", 80, UIFactory.Cream,
                                         new Vector2(0f, 220f), new Vector2(1000f, 120f), FontStyle.Bold);
            Button resume = UIFactory.CreateButton("Btn_Reanudar", panel.transform, "Reanudar", new Vector2(0f, 40f), new Vector2(400f, 80f));
            Button restart = UIFactory.CreateButton("Btn_Reiniciar", panel.transform, "Reiniciar", new Vector2(0f, -60f), new Vector2(400f, 80f));
            Button menu = UIFactory.CreateButton("Btn_Menu", panel.transform, "Menú principal", new Vector2(0f, -160f), new Vector2(400f, 80f));
            panel.gameObject.SetActive(false);

            UIFactory.Wire(pause, "pausePanel", panel.gameObject);
            UIFactory.Wire(pause, "resumeButton", resume);
            UIFactory.Wire(pause, "restartButton", restart);
            UIFactory.Wire(pause, "menuButton", menu);
        }

        private static void BuildEndScreens(Transform root)
        {
            RectTransform holder = UIFactory.CreateRect("PantallasFinales", root);
            UIFactory.Stretch(holder);
            EndScreenController end = holder.gameObject.AddComponent<EndScreenController>();

            // Victoria: tonos fríos de noche nevada
            Image win = UIFactory.CreatePanel("Panel_Victoria", holder, new Color(0.02f, 0.05f, 0.12f, 0.92f));
            UIFactory.CreateCenteredText("Titulo", win.transform, "¡ESCAPASTE!", 110, new Color(0.75f, 0.9f, 1f),
                                         new Vector2(0f, 260f), new Vector2(1400f, 150f), FontStyle.Bold);
            UIFactory.CreateCenteredText("Subtitulo", win.transform,
                                         "Saliste a la nieve justo antes del amanecer.\nEsta Navidad, ningún juguete te llevará a su colección.",
                                         34, UIFactory.Cream, new Vector2(0f, 130f), new Vector2(1400f, 120f));
            Text time = UIFactory.CreateCenteredText("Tiempo", win.transform, "Tiempo: 00:00", 40, UIFactory.Gold,
                                                     new Vector2(0f, 30f), new Vector2(800f, 60f), FontStyle.Bold);
            Button winRestart = UIFactory.CreateButton("Btn_JugarDeNuevo", win.transform, "Jugar de nuevo", new Vector2(0f, -90f), new Vector2(420f, 80f));
            Button winMenu = UIFactory.CreateButton("Btn_Menu", win.transform, "Menú principal", new Vector2(0f, -190f), new Vector2(420f, 80f));
            win.gameObject.SetActive(false);

            // Derrota: rojo sangre
            Image lose = UIFactory.CreatePanel("Panel_Derrota", holder, new Color(0.15f, 0f, 0f, 0.92f));
            UIFactory.CreateCenteredText("Titulo", lose.transform, "¡TE ATRAPARON!", 110, UIFactory.BloodRed,
                                         new Vector2(0f, 260f), new Vector2(1400f, 150f), FontStyle.Bold);
            UIFactory.CreateCenteredText("Subtitulo", lose.transform,
                                         "Ahora eres parte de la colección de juguetes de esta Navidad...",
                                         34, UIFactory.Cream, new Vector2(0f, 140f), new Vector2(1400f, 80f));
            Text lostKeys = UIFactory.CreateCenteredText("Llaves", lose.transform, "Llaves encontradas: 0 / 3", 40,
                                                         UIFactory.Gold, new Vector2(0f, 30f), new Vector2(800f, 60f), FontStyle.Bold);
            Button loseRestart = UIFactory.CreateButton("Btn_Reintentar", lose.transform, "Reintentar", new Vector2(0f, -90f), new Vector2(420f, 80f));
            Button loseMenu = UIFactory.CreateButton("Btn_Menu", lose.transform, "Menú principal", new Vector2(0f, -190f), new Vector2(420f, 80f));
            lose.gameObject.SetActive(false);

            UIFactory.Wire(end, "victoryPanel", win.gameObject);
            UIFactory.Wire(end, "victoryTimeText", time);
            UIFactory.Wire(end, "victoryRestartButton", winRestart);
            UIFactory.Wire(end, "victoryMenuButton", winMenu);
            UIFactory.Wire(end, "defeatPanel", lose.gameObject);
            UIFactory.Wire(end, "defeatKeysText", lostKeys);
            UIFactory.Wire(end, "defeatRestartButton", loseRestart);
            UIFactory.Wire(end, "defeatMenuButton", loseMenu);
        }
    }
}
