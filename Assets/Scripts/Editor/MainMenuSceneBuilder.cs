using HauntedFactory.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Construye la escena del menú principal (Assets/Scenes/MainMenu.unity):
    /// título, historia, botones Jugar / Controles / Salir y panel de controles.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        [MenuItem("Tools/Haunted Factory/Construir menú principal", priority = 2)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!SceneBuildUtility.ConfirmOverwrite(SceneBuildUtility.MainMenuPath)) return;
            Build();
        }

        /// <summary>Construye y guarda la escena sin preguntar (las confirmaciones van antes).</summary>
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.015f, 0.025f);
            camGo.AddComponent<AudioListener>();

            // Cajita musical navideña desafinada de fondo
            var music = camGo.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Placeholder/musica_cajita_bucle.wav");
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = 0.5f;

            UIFactory.CreateEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("UI_Menu", 0);
            Transform root = canvas.transform;
            MainMenuController controller = root.gameObject.AddComponent<MainMenuController>();

            UIFactory.CreatePanel("Fondo", root, new Color(0.04f, 0.02f, 0.03f, 1f)).raycastTarget = false;
            BuildLightString(root);

            // ---- Panel principal ----
            RectTransform main = UIFactory.CreateRect("Panel_Principal", root);
            UIFactory.Stretch(main);

            UIFactory.CreateCenteredText("Titulo", main, "FÁBRICA DE JUGUETES\nEMBRUJADA", 96, UIFactory.BloodRed,
                                         new Vector2(0f, 300f), new Vector2(1600f, 240f), FontStyle.Bold);
            UIFactory.CreateCenteredText("Subtitulo", main, "Una Navidad distinta a la ordinaria", 38,
                                         UIFactory.Gold, new Vector2(0f, 155f), new Vector2(1200f, 60f), FontStyle.Italic);
            UIFactory.CreateCenteredText("Historia", main,
                                         "Eres un elfo atrapado de noche en el taller de Santa. Los juguetes cobraron vida... y no vienen a jugar.\nEncuentra las 3 llaves, abre la puerta de salida y escapa.",
                                         28, UIFactory.Cream, new Vector2(0f, 60f), new Vector2(1300f, 110f));

            Button play = UIFactory.CreateButton("Btn_Jugar", main, "Jugar", new Vector2(0f, -80f), new Vector2(420f, 85f));
            Button controls = UIFactory.CreateButton("Btn_Controles", main, "Controles", new Vector2(0f, -185f), new Vector2(420f, 85f));
            Button quit = UIFactory.CreateButton("Btn_Salir", main, "Salir", new Vector2(0f, -290f), new Vector2(420f, 85f));

            // ---- Panel de controles ----
            Image controlsPanel = UIFactory.CreatePanel("Panel_Controles", root, new Color(0f, 0f, 0f, 0.85f));
            UIFactory.CreateCenteredText("Titulo", controlsPanel.transform, "CONTROLES", 80, UIFactory.Gold,
                                         new Vector2(0f, 300f), new Vector2(1000f, 120f), FontStyle.Bold);
            UIFactory.CreateCenteredText("Lista", controlsPanel.transform,
                                         "W A S D  —  Moverse\n" +
                                         "Shift  —  Correr (¡los juguetes te oyen!)\n" +
                                         "Mouse  —  Mirar\n" +
                                         "F  —  Encender / apagar linterna\n" +
                                         "Esc  —  Pausa\n\n" +
                                         "Consejos:\n" +
                                         "• Con la linterna encendida los juguetes te ven desde más lejos.\n" +
                                         "• Recoge pilas verdes para recargar la linterna.\n" +
                                         "• Corriendo eres más rápido que ellos, pero haces ruido.",
                                         32, UIFactory.Cream, new Vector2(0f, 20f), new Vector2(1300f, 440f));
            Button back = UIFactory.CreateButton("Btn_Volver", controlsPanel.transform, "Volver", new Vector2(0f, -320f), new Vector2(420f, 85f));
            controlsPanel.gameObject.SetActive(false);

            UIFactory.Wire(controller, "mainPanel", main.gameObject);
            UIFactory.Wire(controller, "controlsPanel", controlsPanel.gameObject);
            UIFactory.Wire(controller, "playButton", play);
            UIFactory.Wire(controller, "controlsButton", controls);
            UIFactory.Wire(controller, "quitButton", quit);
            UIFactory.Wire(controller, "backButton", back);

            EditorSceneManager.SaveScene(scene, SceneBuildUtility.MainMenuPath);
            SceneBuildUtility.ConfigureBuildSettings();
            Debug.Log("[Haunted Factory] Menú principal guardado en " + SceneBuildUtility.MainMenuPath);
        }

        /// <summary>Guirnalda de foquitos navideños en la parte de arriba (decorativa).</summary>
        private static void BuildLightString(Transform root)
        {
            RectTransform lights = UIFactory.CreateRect("Guirnalda", root);
            UIFactory.Stretch(lights);
            Color[] colors =
            {
                new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.8f, 0.2f),
                new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.4f, 1f)
            };
            const int count = 24;
            for (int i = 0; i < count; i++)
            {
                float x = -900f + i * (1800f / (count - 1));
                float y = -40f - Mathf.Sin(i / (float)(count - 1) * Mathf.PI * 3f) * 18f;
                UIFactory.CreateImage("Foco_" + i, lights, colors[i % colors.Length], new Vector2(0.5f, 1f),
                                      UIFactory.Center, new Vector2(x, y), new Vector2(18f, 26f));
            }
            lights.gameObject.AddComponent<UILightString>();
        }
    }
}
