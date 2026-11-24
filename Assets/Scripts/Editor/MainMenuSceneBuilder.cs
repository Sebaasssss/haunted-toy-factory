using HauntedFactory.Lighting;
using HauntedFactory.UI;
using HauntedFactory.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Construye el menú principal (Assets/Scenes/MainMenu.unity):
    /// - Fondo 3D: un rincón de la juguetería a oscuras con el árbol, el trenecito,
    ///   un cascanueces y un oso poseídos mirando a la cámara, foquitos y nieve.
    /// - UI a la izquierda: título "HAUNTED TOY FACTORY", historia y botones con efecto.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string PostFxPath = "Assets/Scenes/MainMenu/PostFX-Menu.asset";

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
            PropFactory.ResetRandom(2512);

            var fog = new Color(0.01f, 0.01f, 0.02f);
            EnvironmentFactory.ApplyDarkEnvironment(fog, 0.06f, new Color(0.02f, 0.02f, 0.035f));

            BuildDiorama();
            Camera cam = BuildCamera(fog);
            EnvironmentFactory.CreatePostFxVolume(PostFxPath, 0.55f);
            EnvironmentFactory.Snowfall("PolvoNieve", cam.transform.position + new Vector3(1.5f, 3f, 3.5f), new Vector2(8f, 6f), 60f, 0.3f);

            UIFactory.CreateEventSystem();
            BuildUI();

            EditorSceneManager.SaveScene(scene, SceneBuildUtility.MainMenuPath);
            SceneBuildUtility.ConfigureBuildSettings();
            Debug.Log("[Haunted Factory] Menú principal guardado en " + SceneBuildUtility.MainMenuPath);
        }

        // ------------------------------------------------------------------
        // Fondo 3D
        // ------------------------------------------------------------------

        private static void BuildDiorama()
        {
            Transform root = new GameObject("Diorama").transform;

            GameObject floor = PropFactory.Box("Suelo", root, new Vector3(1f, -0.1f, 2f), new Vector3(16f, 0.2f, 14f), MaterialLibrary.Floor);
            PropFactory.Tile(floor, AutoTiling.Mode.Floor, 2f);
            GameObject back = PropFactory.Box("Pared_Fondo", root, new Vector3(1f, 2f, 7.65f), new Vector3(16f, 4f, 0.3f), MaterialLibrary.Wallpaper);
            PropFactory.Tile(back, AutoTiling.Mode.Wall, 2f);
            GameObject right = PropFactory.Box("Pared_Der", root, new Vector3(7.65f, 2f, 2f), new Vector3(0.3f, 4f, 11.6f), MaterialLibrary.Wallpaper);
            PropFactory.Tile(right, AutoTiling.Mode.Wall, 2f);
            GameObject trimBack = PropFactory.Box("Zocalo_Fondo", root, new Vector3(1f, 0.55f, 7.6f), new Vector3(16f, 1.1f, 0.36f), MaterialLibrary.Wood, false);
            PropFactory.Tile(trimBack, AutoTiling.Mode.Wall, 1.5f);
            GameObject trimRight = PropFactory.Box("Zocalo_Der", root, new Vector3(7.6f, 0.55f, 2f), new Vector3(0.36f, 1.1f, 11.6f), MaterialLibrary.Wood, false);
            PropFactory.Tile(trimRight, AutoTiling.Mode.Wall, 1.5f);

            PropFactory.ShelfUnit(root, "Estante_1", new Vector3(-2.5f, 0f, 7.1f), 0f, 4f, 2.8f, 0.8f, 4, PropFactory.ShelfFill.Teddies);
            PropFactory.ShelfUnit(root, "Estante_2", new Vector3(5.4f, 0f, 7.1f), 0f, 4f, 2.8f, 0.8f, 4, PropFactory.ShelfFill.Gifts);

            var treePos = new Vector3(2.4f, 0f, 4.6f);
            PropFactory.ChristmasTree(root, "Arbol", treePos);
            PropFactory.GiftPile(root, "Regalos", treePos, 1.0f, 1.6f, 10);
            PropFactory.ToyTrain(root, "Tren", treePos, 2.1f);
            PropFactory.RockingHorse(root, "Caballito", new Vector3(-1.8f, 0f, 4.6f), 40f);
            PropFactory.CandyCane(root, "Baston", new Vector3(6.4f, 0f, 5.4f), 2.4f, -90f);
            PropFactory.Balloon(root, "Globo_1", new Vector3(-0.8f, 2.6f, 5.2f));
            PropFactory.Balloon(root, "Globo_2", new Vector3(0.3f, 2.9f, 5.8f));

            // Los juguetes poseídos, quietos, mirando a la cámara
            var camPos = new Vector3(-2.2f, 1.5f, -2.8f);
            Transform nut = PropFactory.Child(root, "Cascanueces", new Vector3(0.5f, 0f, 2.5f));
            nut.rotation = LookYaw(nut.position, camPos);
            PropFactory.AddEyeGlow(PropFactory.BuildNutcracker(nut), 2f, 3f);

            Transform bear = PropFactory.Child(root, "Oso", new Vector3(4.6f, 0f, 2.4f));
            bear.rotation = LookYaw(bear.position, camPos);
            ToyParts bearParts = PropFactory.BuildPossessedTeddy(bear);
            PropFactory.AddEyeGlow(bearParts, 2f, 3f);
            bearParts.RightArm.localRotation = Quaternion.Euler(-70f, 0f, 0f); // saluda... o algo así

            // Luces
            Transform lights = PropFactory.Child(root, "Luces");
            EnvironmentFactory.TreeLights(lights, "Guirnalda_Arbol", treePos);
            EnvironmentFactory.LightString(lights, "Guirnalda_Fondo", new Vector3(-6.5f, 3.6f, 7.4f), new Vector3(7.3f, 3.6f, 7.4f), 26, 2,
                                           ChristmasLightString.Pattern.Chase, false, 0.6f, 4f);
            EnvironmentFactory.LightString(lights, "Guirnalda_Der", new Vector3(7.4f, 3.6f, -3f), new Vector3(7.4f, 3.6f, 7.2f), 18, 1,
                                           ChristmasLightString.Pattern.Random, false, 0.6f, 4f);
            EnvironmentFactory.BrokenLamp(lights, "Lampara", new Vector3(1.2f, 3.4f, 3f), new Color(1f, 0.78f, 0.55f), 1.6f, 8f);
            EnvironmentFactory.PointLight(lights, "Relleno_Calido", new Vector3(2.2f, 1.2f, 2.2f), new Color(1f, 0.6f, 0.35f), 0.8f, 5f);
            EnvironmentFactory.PointLight(lights, "Contraluz_Rojo", new Vector3(0.5f, 2.2f, 3.6f), new Color(1f, 0.1f, 0.1f), 1.2f, 3.5f);
        }

        private static Quaternion LookYaw(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            dir.y = 0f;
            return Quaternion.LookRotation(dir);
        }

        private static Camera BuildCamera(Color background)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(-2.2f, 1.5f, -2.8f);
            camGo.transform.rotation = Quaternion.Euler(4f, 20f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<MenuCameraDrift>();

            // Cajita musical navideña desafinada de fondo
            var music = camGo.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Placeholder/musica_cajita_bucle.wav");
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = 0.5f;
            return cam;
        }

        // ------------------------------------------------------------------
        // Interfaz
        // ------------------------------------------------------------------

        private static void BuildUI()
        {
            Canvas canvas = UIFactory.CreateCanvas("UI_Menu", 0);
            Transform root = canvas.transform;
            MainMenuController controller = root.gameObject.AddComponent<MainMenuController>();

            // Degradado negro a la izquierda para que el texto se lea sobre la escena 3D
            RectTransform gradientRt = UIFactory.CreateRect("Degradado", root);
            gradientRt.anchorMin = new Vector2(0f, 0f);
            gradientRt.anchorMax = new Vector2(0f, 1f);
            gradientRt.pivot = new Vector2(0f, 0.5f);
            gradientRt.offsetMin = Vector2.zero;
            gradientRt.offsetMax = Vector2.zero;
            gradientRt.sizeDelta = new Vector2(1250f, 0f);
            var gradient = gradientRt.gameObject.AddComponent<Image>();
            gradient.sprite = UIFactory.GradientH;
            gradient.color = gradient.sprite != null ? Color.black : new Color(0f, 0f, 0f, 0.6f);
            gradient.raycastTarget = false;

            BuildLightString(root);

            // ---- Panel principal (alineado a la izquierda) ----
            RectTransform main = UIFactory.CreateRect("Panel_Principal", root);
            UIFactory.Stretch(main);
            var left = new Vector2(0f, 0.5f);

            Text title1 = UIFactory.CreateText("Titulo_1", main, "HAUNTED TOY", 96, UIFactory.Cream, TextAnchor.MiddleLeft,
                                               left, left, new Vector2(110f, 330f), new Vector2(1200f, 120f), FontStyle.Bold);
            UIFactory.AddShadow(title1, new Color(0f, 0f, 0f, 0.9f), new Vector2(5f, -5f));
            Text title2 = UIFactory.CreateText("Titulo_2", main, "FACTORY", 150, UIFactory.BloodRed, TextAnchor.MiddleLeft,
                                               left, left, new Vector2(102f, 215f), new Vector2(1200f, 170f), FontStyle.Bold);
            UIFactory.AddOutline(title2, new Color(0.4f, 0f, 0f, 0.6f), new Vector2(4f, 4f));
            UIFactory.AddShadow(title2, new Color(0f, 0f, 0f, 0.9f), new Vector2(6f, -6f));
            title2.gameObject.AddComponent<TextFlicker>();

            UIFactory.CreateImage("Linea", main, UIFactory.Gold, left, left, new Vector2(112f, 128f), new Vector2(460f, 3f));
            Text subtitle = UIFactory.CreateText("Subtitulo", main, "Una Navidad distinta a la ordinaria", 34, UIFactory.Gold,
                                                 TextAnchor.MiddleLeft, left, left, new Vector2(112f, 95f), new Vector2(900f, 50f), FontStyle.Italic);
            UIFactory.AddShadow(subtitle, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));
            Text story = UIFactory.CreateText("Historia", main,
                                              "Eres un elfo atrapado de noche en el taller de Santa. Los juguetes cobraron vida... y no vienen a jugar. " +
                                              "Encuentra las 3 llaves y escapa antes de que te hagan parte de la colección.",
                                              24, new Color(0.95f, 0.9f, 0.8f, 0.8f), TextAnchor.UpperLeft, left, left,
                                              new Vector2(112f, 5f), new Vector2(760f, 110f));
            UIFactory.AddShadow(story, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));

            Button play = UIFactory.CreateMenuButton("Btn_Jugar", main, "JUGAR", left, new Vector2(100f, -110f), new Vector2(520f, 72f), 52);
            Button controls = UIFactory.CreateMenuButton("Btn_Controles", main, "CONTROLES", left, new Vector2(100f, -190f), new Vector2(520f, 72f));
            Button quit = UIFactory.CreateMenuButton("Btn_Salir", main, "SALIR", left, new Vector2(100f, -265f), new Vector2(520f, 72f));


            // ---- Panel de controles (tarjeta centrada) ----
            Image overlay = UIFactory.CreatePanel("Panel_Controles", root, new Color(0f, 0f, 0f, 0.75f));
            Image card = UIFactory.CreateImage("Tarjeta", overlay.transform, new Color(0.06f, 0.02f, 0.03f, 0.96f),
                                               UIFactory.Center, UIFactory.Center, Vector2.zero, new Vector2(1300f, 760f));
            UIFactory.AddOutline(card, new Color(0.85f, 0.1f, 0.1f, 0.8f), new Vector2(3f, 3f));
            UIFactory.CreateCenteredText("Titulo", card.transform, "CONTROLES", 72, UIFactory.Gold,
                                         new Vector2(0f, 300f), new Vector2(1000f, 100f), FontStyle.Bold);
            UIFactory.CreateText("Teclas", card.transform,
                                 "W A S D  —  Moverse\n" +
                                 "Mouse  —  Mirar\n" +
                                 "Shift  —  Correr\n" +
                                 "Espacio  —  Saltar\n" +
                                 "F  —  Linterna\n" +
                                 "E  —  Esconderse\n" +
                                 "Esc  —  Pausa",
                                 34, UIFactory.Cream, TextAnchor.UpperLeft, UIFactory.Center, UIFactory.Center,
                                 new Vector2(-330f, 0f), new Vector2(520f, 420f));
            UIFactory.CreateText("Consejos", card.transform,
                                 "CONSEJOS\n\n" +
                                 "• Los juguetes se congelan mientras los iluminas con la linterna.\n" +
                                 "• Pero la luz los atrae: te ven desde más lejos.\n" +
                                 "• Alumbra los rincones: las llaves y pilas brillan con la luz.\n" +
                                 "• Escóndete en armarios y regalos gigantes (E).\n" +
                                 "• Las pelotas hacen ruido... y el ruido los atrae.\n" +
                                 "• Si oyes un tic-tac... hay un juguete cerca.",
                                 26, UIFactory.Cream, TextAnchor.UpperLeft, UIFactory.Center, UIFactory.Center,
                                 new Vector2(280f, 0f), new Vector2(620f, 420f));
            Button back = UIFactory.CreateButton("Btn_Volver", card.transform, "Volver", new Vector2(0f, -300f), new Vector2(380f, 80f));
            overlay.gameObject.SetActive(false);

            UIFactory.Wire(controller, "mainPanel", main.gameObject);
            UIFactory.Wire(controller, "controlsPanel", overlay.gameObject);
            UIFactory.Wire(controller, "playButton", play);
            UIFactory.Wire(controller, "controlsButton", controls);
            UIFactory.Wire(controller, "quitButton", quit);
            UIFactory.Wire(controller, "backButton", back);
        }

        /// <summary>Guirnalda de foquitos en la parte de arriba (parpadea con UILightString).</summary>
        private static void BuildLightString(Transform root)
        {
            RectTransform lights = UIFactory.CreateRect("Guirnalda", root);
            UIFactory.Stretch(lights);
            Color[] colors =
            {
                new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.8f, 0.2f),
                new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.4f, 1f)
            };
            const int count = 28;
            for (int i = 0; i < count; i++)
            {
                float x = -930f + i * (1860f / (count - 1));
                float y = -30f - Mathf.Abs(Mathf.Sin(i / (float)(count - 1) * Mathf.PI * 4f)) * 22f;
                UIFactory.CreateImage("Foco_" + i, lights, colors[i % colors.Length], new Vector2(0.5f, 1f),
                                      UIFactory.Center, new Vector2(x, y), new Vector2(16f, 24f));
            }
            lights.gameObject.AddComponent<UILightString>();
        }
    }
}
