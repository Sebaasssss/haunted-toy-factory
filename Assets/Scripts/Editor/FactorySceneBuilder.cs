using System.Collections.Generic;
using System.IO;
using HauntedFactory.Audio;
using HauntedFactory.Core;
using HauntedFactory.Enemy;
using HauntedFactory.Items;
using HauntedFactory.Lighting;
using HauntedFactory.Player;
using HauntedFactory.World;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Construye la escena de la fábrica con primitivas desde el menú
    /// Tools > Haunted Factory > Construir escena de la fábrica.
    ///
    /// Todo queda agrupado por salas para que después sea fácil reemplazar
    /// los cubos y cilindros por modelos reales (arrastrar el modelo como hijo
    /// y desactivar el MeshRenderer de la primitiva).
    ///
    /// Plano (vista desde arriba, Z hacia el norte):
    ///
    ///   [Almacén]--pasillo oeste--[Línea de ensamblaje]--pasillo este--[Sala salida]--> Exterior
    ///                                      |
    ///                                pasillo norte
    ///                                      |
    ///                             [Sala de envoltura] (inicio)
    /// </summary>
    public static class FactorySceneBuilder
    {
        private const string ScenePath = SceneBuildUtility.FactoryPath;
        private const string MaterialsFolder = "Assets/Materials";
        private const string NavMeshFolder = "Assets/Scenes/Factory";
        private const string NavMeshPath = NavMeshFolder + "/NavMesh-Factory.asset";
        private const string PostFxPath = NavMeshFolder + "/PostFX-Factory.asset";
        private const string AudioFolder = "Assets/Audio/Placeholder/";
        private const float WallHeight = 4f;
        private const float WallThickness = 0.3f;
        private const float DoorWidth = 4f;

        private static readonly Color FogColor = new Color(0.02f, 0.02f, 0.035f);

        private enum Side { South, North, West, East }

        /// <summary>Hueco en una pared (para puertas y pasillos).</summary>
        private struct Gap
        {
            public Side side;
            public float center;
            public float width;

            public Gap(Side side, float center, float width = DoorWidth)
            {
                this.side = side;
                this.center = center;
                this.width = width;
            }
        }

        // Materiales compartidos
        private static Material matFloor, matWall, matCeiling, matWood, matMetal, matConveyor;
        private static Material matGiftRed, matGiftGreen, matTree, matStar, matBattery, matSnow, matExitSign;
        private static Material matKey, matDoor;
        private static Material matToyBody, matToySkin, matToyHat, matToyEyes;
        private static Material matBulb, matLamp, matSnowFlake;

        [MenuItem("Tools/Haunted Factory/Construir escena de la fábrica", priority = 1)]
        public static void BuildFromMenu()
        {
            // Guardar la escena abierta si el usuario quiere
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!SceneBuildUtility.ConfirmOverwrite(ScenePath)) return;
            Build();
        }

        /// <summary>Construye y guarda la escena sin preguntar (las confirmaciones van antes).</summary>
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMaterials();
            ConfigureEnvironment();

            Transform level = new GameObject("Nivel").transform;
            BuildLayout(level);
            BuildProps(level);
            BuildBatteries();
            BuildKeys();
            BuildExitDoor(level);
            BakeNavMesh(level.gameObject);

            // Ambiente (Fase 5): fuera de "Nivel" para no afectar al NavMesh
            BuildLighting();
            BuildPostProcessing();
            BuildSnow();
            BuildWatchingToys();

            EnemyAI[] enemies = BuildEnemies();
            GameObject player = BuildPlayer(new Vector3(0f, 0.05f, 3f));
            BuildGameManager(player);
            BuildAmbientAudio(enemies);
            FactoryUIBuilder.Build(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneBuildUtility.ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Haunted Factory] Escena construida y guardada en " + ScenePath);
        }

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        private static void CreateMaterials()
        {
            matFloor = GetMaterial("M_Suelo", new Color(0.22f, 0.2f, 0.19f), 0.15f);
            matWall = GetMaterial("M_Pared", new Color(0.32f, 0.12f, 0.1f), 0.05f);
            matCeiling = GetMaterial("M_Techo", new Color(0.1f, 0.1f, 0.12f), 0f);
            matWood = GetMaterial("M_Madera", new Color(0.36f, 0.22f, 0.12f), 0.2f);
            matMetal = GetMaterial("M_Metal", new Color(0.4f, 0.42f, 0.45f), 0.6f, metallic: 0.8f);
            matConveyor = GetMaterial("M_Cinta", new Color(0.08f, 0.08f, 0.08f), 0.3f);
            matGiftRed = GetMaterial("M_RegaloRojo", new Color(0.55f, 0.05f, 0.05f), 0.4f);
            matGiftGreen = GetMaterial("M_RegaloVerde", new Color(0.05f, 0.35f, 0.12f), 0.4f);
            matTree = GetMaterial("M_ArbolMuerto", new Color(0.1f, 0.16f, 0.1f), 0.05f);
            matStar = GetMaterial("M_Estrella", new Color(1f, 0.85f, 0.3f), 0.5f, emission: new Color(2f, 1.6f, 0.4f));
            matBattery = GetMaterial("M_Pila", new Color(0.2f, 0.9f, 0.3f), 0.6f, emission: new Color(0.3f, 1.5f, 0.4f));
            matSnow = GetMaterial("M_Nieve", new Color(0.85f, 0.88f, 0.95f), 0.3f);
            matExitSign = GetMaterial("M_LetreroSalida", new Color(0.8f, 0.05f, 0.05f), 0.3f, emission: new Color(2.5f, 0.1f, 0.1f));
            matKey = GetMaterial("M_Llave", new Color(1f, 0.78f, 0.2f), 0.8f, metallic: 1f, emission: new Color(1.2f, 0.9f, 0.2f));
            matDoor = GetMaterial("M_PuertaSalida", new Color(0.35f, 0.05f, 0.05f), 0.5f, metallic: 0.6f);
            matToyBody = GetMaterial("M_Juguete_Cuerpo", new Color(0.5f, 0.04f, 0.06f), 0.5f);
            matToySkin = GetMaterial("M_Juguete_Cara", new Color(0.85f, 0.75f, 0.65f), 0.7f);
            matToyHat = GetMaterial("M_Juguete_Sombrero", new Color(0.05f, 0.05f, 0.05f), 0.4f);
            matToyEyes = GetMaterial("M_Juguete_Ojos", Color.red, 0.9f, emission: new Color(4f, 0.1f, 0.1f));
            matBulb = GetMaterial("M_FocoNavideno", Color.white, 0.9f, emission: Color.white);
            matLamp = GetMaterial("M_TuboLampara", new Color(1f, 0.9f, 0.7f), 0.5f, emission: new Color(2f, 1.6f, 1.1f));
            matSnowFlake = GetParticleMaterial("M_CopoNieve", new Color(1f, 1f, 1f, 0.9f));
        }

        /// <summary>
        /// Carga el material si ya existe (no sobrescribe lo que hayas editado);
        /// si no existe, lo crea con el shader Lit de URP.
        /// </summary>
        private static Material GetMaterial(string name, Color color, float smoothness,
                                            float metallic = 0f, Color? emission = null)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Material para partículas (shader Particles/Unlit de URP).</summary>
        private static Material GetParticleMaterial(string name, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ------------------------------------------------------------------
        // Ambiente básico (la iluminación final se ajusta en la Fase 5)
        // ------------------------------------------------------------------

        private static void ConfigureEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.06f);

            // Niebla densa y fría: la linterna apenas la atraviesa
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = 0.05f;

            // Luz de luna muy tenue (solo se nota en el exterior)
            var moon = new GameObject("Luz_Luna");
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.65f, 0.9f);
            light.intensity = 0.35f;
            light.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        // ------------------------------------------------------------------
        // Distribución de salas y pasillos
        // ------------------------------------------------------------------

        private static void BuildLayout(Transform level)
        {
            Transform rooms = Child(level, "Salas");

            // Sala de inicio
            BuildRoom(rooms, "Sala_Envoltura", -8f, 8f, 0f, 14f, true,
                new Gap(Side.North, 0f));

            BuildRoom(rooms, "Pasillo_Norte", -2f, 2f, 14f, 30f, true,
                new Gap(Side.South, 0f), new Gap(Side.North, 0f));

            // Sala central
            BuildRoom(rooms, "Linea_Ensamblaje", -14f, 14f, 30f, 50f, true,
                new Gap(Side.South, 0f), new Gap(Side.West, 40f), new Gap(Side.East, 40f));

            BuildRoom(rooms, "Pasillo_Oeste", -30f, -14f, 38f, 42f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));

            BuildRoom(rooms, "Almacen_Munecos", -46f, -30f, 30f, 50f, true,
                new Gap(Side.East, 40f));

            BuildRoom(rooms, "Pasillo_Este", 14f, 28f, 38f, 42f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));

            // Sala con la salida (la puerta final se agrega en la Fase 2)
            BuildRoom(rooms, "Sala_Salida", 28f, 40f, 33f, 47f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));

            // Patio nevado afuera de la fábrica (sin techo)
            BuildRoom(rooms, "Exterior_Nieve", 40f, 50f, 34f, 46f, false,
                new Gap(Side.West, 40f));
            SetFloorMaterial(rooms.Find("Exterior_Nieve"), matSnow);
        }

        /// <summary>
        /// Crea una sala rectangular: suelo, techo opcional y 4 paredes
        /// con huecos donde se indique.
        /// </summary>
        private static void BuildRoom(Transform parent, string name, float xMin, float xMax,
                                      float zMin, float zMax, bool withCeiling, params Gap[] gaps)
        {
            Transform root = Child(parent, name);
            float width = xMax - xMin;
            float depth = zMax - zMin;
            float cx = (xMin + xMax) * 0.5f;
            float cz = (zMin + zMax) * 0.5f;

            Box("Suelo", root, new Vector3(cx, -0.1f, cz), new Vector3(width, 0.2f, depth), matFloor);
            if (withCeiling)
            {
                Box("Techo", root, new Vector3(cx, WallHeight + 0.1f, cz), new Vector3(width, 0.2f, depth), matCeiling);
            }

            Transform walls = Child(root, "Paredes");
            BuildSide(walls, Side.South, xMin, xMax, zMin, gaps);
            BuildSide(walls, Side.North, xMin, xMax, zMax, gaps);
            BuildSide(walls, Side.West, zMin, zMax, xMin, gaps);
            BuildSide(walls, Side.East, zMin, zMax, xMax, gaps);
        }

        /// <summary>Divide un lado en segmentos de pared, saltándose los huecos.</summary>
        private static void BuildSide(Transform parent, Side side, float from, float to, float fixedCoord, Gap[] gaps)
        {
            var openings = new List<Vector2>();
            foreach (Gap g in gaps)
            {
                if (g.side == side) openings.Add(new Vector2(g.center - g.width * 0.5f, g.center + g.width * 0.5f));
            }
            openings.Sort((a, b) => a.x.CompareTo(b.x));

            float cursor = from;
            foreach (Vector2 o in openings)
            {
                WallSegment(parent, side, cursor, Mathf.Max(cursor, o.x), fixedCoord);
                cursor = Mathf.Max(cursor, o.y);
            }
            WallSegment(parent, side, cursor, to, fixedCoord);
        }

        private static void WallSegment(Transform parent, Side side, float a, float b, float fixedCoord)
        {
            float length = b - a;
            if (length < 0.05f) return;

            float mid = (a + b) * 0.5f;
            bool alongX = side == Side.South || side == Side.North;

            Vector3 pos = alongX
                ? new Vector3(mid, WallHeight * 0.5f, fixedCoord)
                : new Vector3(fixedCoord, WallHeight * 0.5f, mid);
            Vector3 size = alongX
                ? new Vector3(length + WallThickness, WallHeight, WallThickness)
                : new Vector3(WallThickness, WallHeight, length + WallThickness);

            Box("Pared_" + side, parent, pos, size, matWall);
        }

        private static void SetFloorMaterial(Transform room, Material mat)
        {
            if (room == null) return;
            Transform floor = room.Find("Suelo");
            if (floor != null) floor.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ------------------------------------------------------------------
        // Utilería (decoración con primitivas, fácil de reemplazar)
        // ------------------------------------------------------------------

        private static void BuildProps(Transform level)
        {
            Transform props = Child(level, "Utileria");

            // Sala de envoltura: mesas de trabajo y regalos a medio envolver
            Transform wrap = Child(props, "Envoltura");
            Box("Mesa_1", wrap, new Vector3(-5f, 0.45f, 5f), new Vector3(3f, 0.9f, 1.4f), matWood);
            Box("Mesa_2", wrap, new Vector3(5f, 0.45f, 5f), new Vector3(3f, 0.9f, 1.4f), matWood);
            Box("Mesa_3", wrap, new Vector3(-5f, 0.45f, 10f), new Vector3(3f, 0.9f, 1.4f), matWood);
            Box("Regalo_1", wrap, new Vector3(-5.5f, 1.15f, 5f), new Vector3(0.5f, 0.5f, 0.5f), matGiftRed);
            Box("Regalo_2", wrap, new Vector3(4.6f, 1.1f, 5.2f), new Vector3(0.4f, 0.4f, 0.6f), matGiftGreen);
            Box("Regalo_3", wrap, new Vector3(6.5f, 0.3f, 12f), new Vector3(0.6f, 0.6f, 0.6f), matGiftRed);
            Box("Regalo_4", wrap, new Vector3(7f, 0.25f, 11.2f), new Vector3(0.5f, 0.5f, 0.5f), matGiftGreen);
            Cylinder("RolloPapel_1", wrap, new Vector3(-6.8f, 0.6f, 1.5f), new Vector3(0.3f, 0.6f, 0.3f), matGiftRed);
            Cylinder("RolloPapel_2", wrap, new Vector3(-6.2f, 0.6f, 1.5f), new Vector3(0.3f, 0.6f, 0.3f), matGiftGreen);

            // Línea de ensamblaje: cintas transportadoras y árbol de Navidad marchito
            Transform line = Child(props, "Ensamblaje");
            Box("Cinta_SurOeste", line, new Vector3(-7f, 0.4f, 35f), new Vector3(8f, 0.8f, 1.6f), matConveyor);
            Box("Cinta_SurEste", line, new Vector3(7f, 0.4f, 35f), new Vector3(8f, 0.8f, 1.6f), matConveyor);
            Box("Cinta_Norte", line, new Vector3(0f, 0.4f, 45f), new Vector3(22f, 0.8f, 1.6f), matConveyor);
            Box("Maquina_1", line, new Vector3(-12f, 1.25f, 45f), new Vector3(2f, 2.5f, 2.5f), matMetal);
            Box("Maquina_2", line, new Vector3(12f, 1.25f, 35f), new Vector3(2f, 2.5f, 2.5f), matMetal);
            Box("Juguete_EnCinta_1", line, new Vector3(-4f, 1f, 45f), new Vector3(0.4f, 0.4f, 0.4f), matGiftRed);
            Box("Juguete_EnCinta_2", line, new Vector3(3f, 1f, 45f), new Vector3(0.4f, 0.4f, 0.4f), matGiftGreen);
            BuildChristmasTree(line, new Vector3(10f, 0f, 47.5f));

            // Almacén: estantes y cajas
            Transform storage = Child(props, "Almacen");
            for (int i = 0; i < 3; i++)
            {
                float x = -43f + i * 5f;
                Box("Estante_" + (i + 1), storage, new Vector3(x, 1.5f, 36f), new Vector3(1.2f, 3f, 6f), matWood);
            }
            Box("Estante_4", storage, new Vector3(-40f, 1.5f, 46f), new Vector3(8f, 3f, 1.2f), matWood);
            Box("Caja_1", storage, new Vector3(-34f, 0.5f, 33f), new Vector3(1f, 1f, 1f), matWood);
            Box("Caja_2", storage, new Vector3(-33f, 0.4f, 34.3f), new Vector3(0.8f, 0.8f, 0.8f), matWood);
            Box("Caja_3", storage, new Vector3(-34f, 1.4f, 33f), new Vector3(0.8f, 0.8f, 0.8f), matGiftRed);

            // Sala de salida: cajas y letrero
            Transform exit = Child(props, "Salida");
            Box("Caja_1", exit, new Vector3(31f, 0.6f, 45f), new Vector3(1.2f, 1.2f, 1.2f), matWood);
            Box("Caja_2", exit, new Vector3(37f, 0.5f, 35f), new Vector3(1f, 1f, 1f), matWood);
            Box("Letrero_Salida", exit, new Vector3(39.75f, 3.4f, 40f), new Vector3(0.1f, 0.4f, 1.4f), matExitSign, false);
        }

        /// <summary>Árbol de Navidad hecho con cilindros (reemplazable por un modelo).</summary>
        private static void BuildChristmasTree(Transform parent, Vector3 basePos)
        {
            Transform tree = Child(parent, "ArbolNavidad");
            tree.localPosition = basePos;
            Cylinder("Tronco", tree, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0.3f), matWood);
            Cylinder("Capa_1", tree, new Vector3(0f, 0.9f, 0f), new Vector3(2.2f, 0.35f, 2.2f), matTree);
            Cylinder("Capa_2", tree, new Vector3(0f, 1.6f, 0f), new Vector3(1.6f, 0.35f, 1.6f), matTree);
            Cylinder("Capa_3", tree, new Vector3(0f, 2.3f, 0f), new Vector3(1f, 0.35f, 1f), matTree);
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Estrella";
            star.transform.SetParent(tree, false);
            star.transform.localPosition = new Vector3(0f, 2.95f, 0f);
            star.transform.localScale = Vector3.one * 0.3f;
            star.GetComponent<MeshRenderer>().sharedMaterial = matStar;
        }

        // ------------------------------------------------------------------
        // Iluminación navideña embrujada (Fase 5)
        // ------------------------------------------------------------------

        private static void BuildLighting()
        {
            Transform root = new GameObject("Iluminacion").transform;
            Transform strings = Child(root, "Guirnaldas");
            const float h = 3.65f;
            var chase = ChristmasLightString.Pattern.Chase;
            var alternate = ChristmasLightString.Pattern.Alternate;
            var random = ChristmasLightString.Pattern.Random;

            // Sala de envoltura
            LightString(strings, "Guirnalda_Envoltura_Sur", new Vector3(-7.6f, h, 0.35f), new Vector3(7.6f, h, 0.35f), 16, 2, chase);
            LightString(strings, "Guirnalda_Envoltura_Norte", new Vector3(-7.6f, h, 13.65f), new Vector3(7.6f, h, 13.65f), 16, 2, alternate);
            // Pasillo norte (fallando todo el tiempo)
            LightString(strings, "Guirnalda_PasilloNorte", new Vector3(-1.65f, h, 15f), new Vector3(-1.65f, h, 29f), 14, 2, random);
            // Línea de ensamblaje
            LightString(strings, "Guirnalda_Ensamblaje_Sur", new Vector3(-13.6f, h, 30.35f), new Vector3(13.6f, h, 30.35f), 24, 3, chase);
            LightString(strings, "Guirnalda_Ensamblaje_Norte", new Vector3(-13.6f, h, 49.65f), new Vector3(13.6f, h, 49.65f), 24, 3, alternate);
            LightString(strings, "Guirnalda_Ensamblaje_Oeste", new Vector3(-13.65f, h, 31f), new Vector3(-13.65f, h, 49f), 16, 2, chase);
            LightString(strings, "Guirnalda_Ensamblaje_Este", new Vector3(13.65f, h, 31f), new Vector3(13.65f, h, 49f), 16, 2, random);
            // Pasillos oeste y este
            LightString(strings, "Guirnalda_PasilloOeste", new Vector3(-29f, h, 38.35f), new Vector3(-15f, h, 38.35f), 12, 1, random);
            LightString(strings, "Guirnalda_PasilloEste", new Vector3(15f, h, 41.65f), new Vector3(27f, h, 41.65f), 12, 1, chase);
            // Almacén
            LightString(strings, "Guirnalda_Almacen", new Vector3(-45.6f, h, 49.65f), new Vector3(-30.4f, h, 49.65f), 16, 2, random);
            // Sala de salida: solo focos rojos, como alarma
            LightString(strings, "Guirnalda_Salida", new Vector3(28.4f, h, 46.65f), new Vector3(39.6f, h, 46.65f), 12, 2, alternate, true);

            TreeLights(strings, new Vector3(10f, 0f, 47.5f));

            // Lámparas de trabajo dañadas
            Transform lamps = Child(root, "Lamparas");
            BrokenLamp(lamps, "Lampara_Envoltura", new Vector3(0f, 3.95f, 7f), new Color(1f, 0.78f, 0.55f), 2.2f, 10f);
            BrokenLamp(lamps, "Lampara_Ensamblaje", new Vector3(0f, 3.95f, 40f), new Color(1f, 0.7f, 0.45f), 2.5f, 12f);
            BrokenLamp(lamps, "Lampara_Almacen", new Vector3(-38f, 3.95f, 40f), new Color(0.6f, 0.7f, 1f), 1.6f, 10f);
            BrokenLamp(lamps, "Lampara_Salida", new Vector3(34f, 3.95f, 40f), new Color(1f, 0.2f, 0.12f), 2f, 9f);

            PointLight(root, "Luz_LetreroSalida", new Vector3(39.2f, 3.3f, 40f), new Color(1f, 0.1f, 0.08f), 1.2f, 5f);
        }

        /// <summary>Guirnalda recta entre dos puntos, con un poco de caída entre focos.</summary>
        private static void LightString(Transform parent, string name, Vector3 from, Vector3 to, int bulbCount,
                                        int lightCount, ChristmasLightString.Pattern pattern, bool redOnly = false)
        {
            Transform root = Child(parent, name);
            var bulbs = new Renderer[bulbCount];
            for (int i = 0; i < bulbCount; i++)
            {
                float t = bulbCount == 1 ? 0.5f : i / (float)(bulbCount - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y -= 0.15f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * bulbCount / 4f));
                bulbs[i] = CreateBulb(root, "Foco_" + i, p);
            }

            var lights = new Light[lightCount];
            for (int j = 0; j < lightCount; j++)
            {
                Vector3 p = Vector3.Lerp(from, to, (j + 0.5f) / lightCount) + Vector3.down * 0.35f;
                lights[j] = PointLight(root, "Luz_" + j, p, Color.white, 0.9f, 5f);
            }

            AddLightStringComponent(root.gameObject, bulbs, lights, pattern, redOnly);
        }

        /// <summary>Anillos de focos alrededor de las capas del árbol de Navidad.</summary>
        private static void TreeLights(Transform parent, Vector3 treeBase)
        {
            Transform root = Child(parent, "Guirnalda_Arbol");
            var bulbs = new List<Renderer>();
            float[] heights = { 1.0f, 1.7f, 2.4f };
            float[] radii = { 1.12f, 0.82f, 0.52f };
            int[] counts = { 10, 8, 6 };
            for (int ring = 0; ring < heights.Length; ring++)
            {
                for (int i = 0; i < counts[ring]; i++)
                {
                    float angle = (i / (float)counts[ring]) * Mathf.PI * 2f + ring * 0.4f;
                    Vector3 p = treeBase + new Vector3(Mathf.Cos(angle) * radii[ring], heights[ring], Mathf.Sin(angle) * radii[ring]);
                    bulbs.Add(CreateBulb(root, "Foco_" + ring + "_" + i, p));
                }
            }
            Light glow = PointLight(root, "Luz_Arbol", treeBase + new Vector3(0f, 1.8f, -1.3f), Color.white, 1.2f, 6f);
            AddLightStringComponent(root.gameObject, bulbs.ToArray(), new[] { glow }, ChristmasLightString.Pattern.Chase, false);
        }

        private static Renderer CreateBulb(Transform parent, string name, Vector3 worldPos)
        {
            GameObject bulb = Primitive(PrimitiveType.Sphere, name, parent, worldPos, new Vector3(0.09f, 0.13f, 0.09f), matBulb, false);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            var r = bulb.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        private static void AddLightStringComponent(GameObject go, Renderer[] bulbs, Light[] lights,
                                                    ChristmasLightString.Pattern pattern, bool redOnly)
        {
            var comp = go.AddComponent<ChristmasLightString>();
            var so = new SerializedObject(comp);
            SetObjectArray(so.FindProperty("bulbs"), bulbs);
            SetObjectArray(so.FindProperty("lights"), lights);
            so.FindProperty("pattern").enumValueIndex = (int)pattern;
            if (redOnly)
            {
                SerializedProperty palette = so.FindProperty("palette");
                palette.arraySize = 2;
                palette.GetArrayElementAtIndex(0).colorValue = new Color(1f, 0.05f, 0.05f);
                palette.GetArrayElementAtIndex(1).colorValue = new Color(0.6f, 0f, 0f);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Lámpara de techo con tubo emisivo y luz que parpadea.</summary>
        private static void BrokenLamp(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            Transform root = Child(parent, name);
            GameObject housing = Box("Carcasa", root, pos, new Vector3(0.7f, 0.08f, 0.3f), matMetal, false);
            GameObject tube = Box("Tubo", root, pos + Vector3.down * 0.06f, new Vector3(0.6f, 0.04f, 0.12f), matLamp, false);
            Object.DestroyImmediate(housing.GetComponent<Collider>());
            Object.DestroyImmediate(tube.GetComponent<Collider>());

            Light light = PointLight(root, "Luz", pos + Vector3.down * 0.35f, color, intensity, range);
            light.shadows = LightShadows.Soft;
            var flicker = light.gameObject.AddComponent<FlickeringLight>();
            var so = new SerializedObject(flicker);
            so.FindProperty("lampRenderer").objectReferenceValue = tube.GetComponent<MeshRenderer>();
            so.FindProperty("lampEmission").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedProperty prop, Object[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // ------------------------------------------------------------------
        // Post-procesado, nieve y juguetes que observan (Fase 5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Volumen global: bloom (focos y ojos brillan), viñeta, grano de película,
        /// colores fríos y desaturados, y algo de aberración cromática.
        /// El perfil es un asset generado: se reemplaza en cada reconstrucción.
        /// </summary>
        private static void BuildPostProcessing()
        {
            EnsureFolder(NavMeshFolder);
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath) != null)
            {
                AssetDatabase.DeleteAsset(PostFxPath);
            }
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostFxPath);

            Bloom bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = AddOverride<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);

            ColorAdjustments color = AddOverride<ColorAdjustments>(profile);
            color.contrast.Override(12f);
            color.saturation.Override(-20f);
            color.colorFilter.Override(new Color(0.88f, 0.93f, 1f));

            Tonemapping tonemapping = AddOverride<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            ChromaticAberration aberration = AddOverride<ChromaticAberration>(profile);
            aberration.intensity.Override(0.15f);

            EditorUtility.SetDirty(profile);

            var go = new GameObject("PostProcesado_Global");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        /// <summary>Nevada en el patio exterior (se ve por la puerta de salida).</summary>
        private static void BuildSnow()
        {
            var go = new GameObject("Nevada_Exterior");
            go.transform.position = new Vector3(45f, 6f, 40f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // emitir hacia abajo

            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = 0.03f;
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 150f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 14f, 0.1f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = matSnowFlake;
        }

        /// <summary>Juguetes quietos que giran para mirarte (solo ambientación).</summary>
        private static void BuildWatchingToys()
        {
            Transform parent = new GameObject("JuguetesDecorativos").transform;
            DecorToy(parent, "Muneco_Envoltura", new Vector3(7f, 0f, 1f), -45f);
            DecorToy(parent, "Muneco_Almacen", new Vector3(-45.2f, 0f, 46.8f), 90f);
            DecorToy(parent, "Muneco_Salida", new Vector3(30f, 0f, 34.2f), 30f);
        }

        private static void DecorToy(Transform parent, string name, Vector3 pos, float yaw)
        {
            var toy = new GameObject(name);
            toy.transform.SetParent(parent, false);
            toy.transform.position = pos;
            toy.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var col = toy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.height = 1.6f;
            col.radius = 0.35f;

            Transform visual = BuildToyVisual(toy.transform, false);
            var watch = toy.AddComponent<WatchingToy>();
            var so = new SerializedObject(watch);
            so.FindProperty("head").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Audio (Fase 5)
        // ------------------------------------------------------------------

        private static AudioClip Clip(string fileName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + fileName + ".wav");
            if (clip == null) Debug.LogWarning("[Haunted Factory] No se encontró el audio " + AudioFolder + fileName + ".wav");
            return clip;
        }

        private static void BuildAmbientAudio(EnemyAI[] enemies)
        {
            var go = new GameObject("Audio_Ambiente");
            var controller = go.AddComponent<AmbientMusicController>();
            var so = new SerializedObject(controller);
            so.FindProperty("ambientLoop").objectReferenceValue = Clip("amb_fabrica_bucle");
            so.FindProperty("musicBoxLoop").objectReferenceValue = Clip("musica_cajita_bucle");
            so.FindProperty("chaseLoop").objectReferenceValue = Clip("musica_persecucion_bucle");
            SetObjectArray(so.FindProperty("enemies"), enemies);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // ------------------------------------------------------------------
        // Pilas recolectables
        // ------------------------------------------------------------------

        private static void BuildBatteries()
        {
            Transform parent = new GameObject("Pilas").transform;
            CreateBattery(parent, "Pila_1", new Vector3(5f, 1.3f, 4.5f));     // sobre el borde de una mesa de envoltura
            CreateBattery(parent, "Pila_2", new Vector3(-12f, 0.6f, 48f));    // esquina de la línea de ensamblaje
            CreateBattery(parent, "Pila_3", new Vector3(-44.5f, 0.6f, 32f));  // fondo del almacén
            CreateBattery(parent, "Pila_4", new Vector3(21f, 0.6f, 41f));     // pasillo este
        }

        private static void CreateBattery(Transform parent, string name, Vector3 pos)
        {
            // Raíz con el trigger y los scripts; la parte visual es hija (fácil de reemplazar)
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;

            BatteryPickup pickup = root.AddComponent<BatteryPickup>();
            UIFactory.Wire(pickup, "pickupSound", Clip("sfx_pila"));
            root.AddComponent<Bobbing>();

            GameObject body = Cylinder("Visual", root.transform, Vector3.zero, new Vector3(0.18f, 0.2f, 0.18f), matBattery, false);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            GameObject cap = Cylinder("Polo", root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.03f, 0.08f), matMetal, false);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------
        // Jugador (elfo) con cámara y linterna
        // ------------------------------------------------------------------

        private static GameObject BuildPlayer(Vector3 spawn)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = spawn;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.6f;   // los elfos son bajitos
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.8f, 0f);
            controller.stepOffset = 0.3f;

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            var look = player.AddComponent<PlayerLook>();

            Transform cameraRoot = Child(player.transform, "CameraRoot");
            cameraRoot.localPosition = new Vector3(0f, 1.45f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(cameraRoot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();

            CameraBob bob = camGo.AddComponent<CameraBob>();
            UIFactory.Wire(bob, "movement", movement);

            var flashlightGo = new GameObject("Flashlight");
            flashlightGo.transform.SetParent(cameraRoot, false);
            flashlightGo.transform.localPosition = new Vector3(0.25f, -0.2f, 0.2f);
            var spot = flashlightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.95f, 0.85f);
            spot.intensity = 8f;
            spot.range = 25f;
            spot.spotAngle = 55f;
            spot.innerSpotAngle = 25f;
            spot.shadows = LightShadows.Soft;
            var flashlightAudio = flashlightGo.AddComponent<AudioSource>();
            flashlightAudio.playOnAwake = false;
            flashlightAudio.spatialBlend = 0f;
            Flashlight flashlight = flashlightGo.AddComponent<Flashlight>();
            UIFactory.Wire(flashlight, "toggleSound", Clip("sfx_linterna_click"));

            // Pasos
            var feet = new GameObject("Pasos");
            feet.transform.SetParent(player.transform, false);
            var feetAudio = feet.AddComponent<AudioSource>();
            feetAudio.playOnAwake = false;
            feetAudio.spatialBlend = 0f;
            FootstepAudio footsteps = feet.AddComponent<FootstepAudio>();
            var feetSo = new SerializedObject(footsteps);
            feetSo.FindProperty("movement").objectReferenceValue = movement;
            SetObjectArray(feetSo.FindProperty("steps"), new Object[] { Clip("sfx_paso_1"), Clip("sfx_paso_2"), Clip("sfx_paso_3") });
            feetSo.ApplyModifiedPropertiesWithoutUndo();

            // Asignar el campo privado serializado cameraRoot de PlayerLook
            var so = new SerializedObject(look);
            so.FindProperty("cameraRoot").objectReferenceValue = cameraRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        // ------------------------------------------------------------------
        // Llaves, puerta de salida y GameManager (Fase 2)
        // ------------------------------------------------------------------

        private static void BuildKeys()
        {
            Transform parent = new GameObject("Llaves").transform;
            CreateKey(parent, "Llave_1", new Vector3(-7f, 0.8f, 13f));      // esquina de la sala de envoltura
            CreateKey(parent, "Llave_2", new Vector3(12.5f, 0.8f, 48.5f));  // junto al árbol, línea de ensamblaje
            CreateKey(parent, "Llave_3", new Vector3(-45f, 0.8f, 48.5f));   // fondo del almacén de muñecos
        }

        private static void CreateKey(Transform parent, string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;

            root.AddComponent<KeyPickup>();
            root.AddComponent<Bobbing>();

            // Llave hecha con primitivas: anillo (cilindro plano), vástago y dientes
            Transform visual = Child(root.transform, "Visual");
            visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject ring = Cylinder("Anillo", visual, new Vector3(0f, 0.25f, 0f), new Vector3(0.25f, 0.03f, 0.25f), matKey, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject shaft = Box("Vastago", visual, new Vector3(0f, -0.05f, 0f), new Vector3(0.05f, 0.45f, 0.05f), matKey, false);
            GameObject tooth1 = Box("Diente_1", visual, new Vector3(0.06f, -0.22f, 0f), new Vector3(0.1f, 0.05f, 0.04f), matKey, false);
            GameObject tooth2 = Box("Diente_2", visual, new Vector3(0.05f, -0.12f, 0f), new Vector3(0.08f, 0.04f, 0.04f), matKey, false);
            foreach (GameObject part in new[] { ring, shaft, tooth1, tooth2 })
            {
                Object.DestroyImmediate(part.GetComponent<Collider>());
            }

            // Luz dorada pequeña para que la llave se vea a lo lejos
            var glow = new GameObject("Brillo");
            glow.transform.SetParent(root.transform, false);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.3f);
            light.intensity = 0.8f;
            light.range = 2.5f;
            light.shadows = LightShadows.None;
        }

        /// <summary>
        /// Puerta tipo cortina metálica en el hueco este de la sala de salida,
        /// y la zona de escape en el patio nevado.
        /// </summary>
        private static void BuildExitDoor(Transform level)
        {
            Transform exitRoot = Child(level, "Salida");

            var door = new GameObject("PuertaSalida");
            door.transform.SetParent(exitRoot, false);
            door.transform.localPosition = new Vector3(40f, 0f, 40f);

            // Trigger dentro de la sala, justo delante de la puerta
            var zone = door.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(-1.5f, 1f, 0f);
            zone.size = new Vector3(3f, 2f, DoorWidth);

            ExitDoor exitDoor = door.AddComponent<ExitDoor>();

            GameObject panel = Box("Hoja", door.transform, new Vector3(0f, WallHeight * 0.5f, 0f),
                                   new Vector3(0.25f, WallHeight, DoorWidth), matDoor, false);

            var so = new SerializedObject(exitDoor);
            so.FindProperty("doorPanel").objectReferenceValue = panel.transform;
            so.FindProperty("openSound").objectReferenceValue = Clip("sfx_puerta_abre");
            so.FindProperty("lockedSound").objectReferenceValue = Clip("sfx_puerta_cerrada");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Zona de escape: al pisar la nieve de afuera se gana
            var escape = new GameObject("ZonaEscape");
            escape.transform.SetParent(exitRoot, false);
            escape.transform.localPosition = new Vector3(45.5f, 1f, 40f);
            var escapeCol = escape.AddComponent<BoxCollider>();
            escapeCol.isTrigger = true;
            escapeCol.size = new Vector3(8f, 2f, 11f);
            escape.AddComponent<EscapeZone>();
        }

        private static void BuildGameManager(GameObject player)
        {
            var go = new GameObject("GameManager");
            GameManager gm = go.AddComponent<GameManager>();

            var so = new SerializedObject(gm);
            so.FindProperty("playerMovement").objectReferenceValue = player.GetComponent<PlayerMovement>();
            so.FindProperty("playerLook").objectReferenceValue = player.GetComponent<PlayerLook>();
            so.FindProperty("flashlight").objectReferenceValue = player.GetComponentInChildren<Flashlight>();
            so.FindProperty("keySound").objectReferenceValue = Clip("sfx_llave");
            so.FindProperty("winSound").objectReferenceValue = Clip("sfx_victoria");
            so.FindProperty("loseSound").objectReferenceValue = Clip("sfx_atrapado");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // NavMesh y enemigos (Fase 3)
        // ------------------------------------------------------------------

        /// <summary>
        /// Hornea el NavMesh con un NavMeshSurface sobre el objeto "Nivel"
        /// (solo salas, utilería y puerta; no jugador, pilas ni llaves)
        /// y lo guarda como asset para que la escena lo conserve.
        /// </summary>
        private static void BakeNavMesh(GameObject levelRoot)
        {
            var surface = levelRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[Haunted Factory] No se pudo hornear el NavMesh.");
                return;
            }

            EnsureFolder(NavMeshFolder);
            // El NavMesh es un archivo generado: se reemplaza en cada reconstrucción
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null)
            {
                AssetDatabase.DeleteAsset(NavMeshPath);
            }
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        private static EnemyAI[] BuildEnemies()
        {
            Transform parent = new GameObject("Enemigos").transform;

            // Juguete 1: recorre la línea de ensamblaje y a veces baja por el pasillo norte
            EnemyAI first = CreateToy(parent, "JuguetePoseido_Ensamblaje", new[]
            {
                new Vector3(0f, 0f, 40f),
                new Vector3(11f, 0f, 40f),
                new Vector3(6f, 0f, 48.5f),
                new Vector3(-6f, 0f, 48.5f),
                new Vector3(-11f, 0f, 40f),
                new Vector3(0f, 0f, 22f),
            });

            // Juguete 2: vigila el almacén y el pasillo oeste
            EnemyAI second = CreateToy(parent, "JuguetePoseido_Almacen", new[]
            {
                new Vector3(-38f, 0f, 40f),
                new Vector3(-40.5f, 0f, 34f),
                new Vector3(-45f, 0f, 41f),
                new Vector3(-31f, 0f, 48f),
                new Vector3(-22f, 0f, 40f),
            });

            return new[] { first, second };
        }

        /// <summary>
        /// Crea un juguete poseído (cascanueces de primitivas) con NavMeshAgent,
        /// percepción, IA y su ruta de patrulla. Empieza en el primer waypoint.
        /// </summary>
        private static EnemyAI CreateToy(Transform parent, string name, Vector3[] route)
        {
            var toy = new GameObject(name);
            toy.transform.SetParent(parent, false);
            toy.transform.position = route[0];

            var col = toy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.height = 1.6f;
            col.radius = 0.4f;

            var agent = toy.AddComponent<NavMeshAgent>();
            agent.height = 1.6f;
            agent.radius = 0.4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;

            toy.AddComponent<EnemyPerception>();
            EnemyAI ai = toy.AddComponent<EnemyAI>();

            BuildToyVisual(toy.transform);

            // Tic-tac 3D del mecanismo de cuerda + chillido al detectarte
            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(toy.transform, false);
            audioGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var source = audioGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.dopplerLevel = 0f;
            source.volume = 0.8f;
            EnemyAudio enemyAudio = audioGo.AddComponent<EnemyAudio>();
            var audioSo = new SerializedObject(enemyAudio);
            audioSo.FindProperty("enemy").objectReferenceValue = ai;
            audioSo.FindProperty("windUpLoop").objectReferenceValue = Clip("sfx_juguete_cuerda_bucle");
            audioSo.FindProperty("alertClip").objectReferenceValue = Clip("sfx_alerta_juguete");
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            // Waypoints agrupados en un objeto aparte para poder moverlos en el editor
            Transform routeRoot = Child(parent, "Ruta_" + name);
            var points = new Transform[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                Transform wp = Child(routeRoot, "Waypoint_" + i);
                wp.position = route[i];
                points[i] = wp;
            }

            var so = new SerializedObject(ai);
            SerializedProperty prop = so.FindProperty("waypoints");
            prop.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return ai;
        }

        /// <summary>Cascanueces poseído de primitivas (reemplazable por un modelo).</summary>
        private static Transform BuildToyVisual(Transform toy, bool withEyeLight = true)
        {
            Transform visual = Child(toy, "Visual");
            var parts = new List<GameObject>
            {
                Primitive(PrimitiveType.Capsule, "Cuerpo", visual, new Vector3(0f, 0.65f, 0f), new Vector3(0.7f, 0.6f, 0.5f), matToyBody, false),
                Primitive(PrimitiveType.Sphere, "Cabeza", visual, new Vector3(0f, 1.35f, 0f), new Vector3(0.45f, 0.5f, 0.45f), matToySkin, false),
                Primitive(PrimitiveType.Cylinder, "Sombrero", visual, new Vector3(0f, 1.78f, 0f), new Vector3(0.42f, 0.22f, 0.42f), matToyHat, false),
                Primitive(PrimitiveType.Cube, "Mandibula", visual, new Vector3(0f, 1.18f, 0.17f), new Vector3(0.25f, 0.08f, 0.12f), matToyHat, false),
                Primitive(PrimitiveType.Sphere, "Ojo_Izq", visual, new Vector3(-0.09f, 1.4f, 0.2f), Vector3.one * 0.08f, matToyEyes, false),
                Primitive(PrimitiveType.Sphere, "Ojo_Der", visual, new Vector3(0.09f, 1.4f, 0.2f), Vector3.one * 0.08f, matToyEyes, false),
                Primitive(PrimitiveType.Cube, "Brazo_Izq", visual, new Vector3(-0.42f, 0.8f, 0.1f), new Vector3(0.12f, 0.6f, 0.12f), matToyBody, false),
                Primitive(PrimitiveType.Cube, "Brazo_Der", visual, new Vector3(0.42f, 0.8f, 0.1f), new Vector3(0.12f, 0.6f, 0.12f), matToyBody, false),
            };
            // Los brazos estirados hacia adelante, como queriendo atrapar
            parts[6].transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            parts[7].transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);

            // Solo el CapsuleCollider de la raíz debe chocar
            foreach (GameObject part in parts)
            {
                Object.DestroyImmediate(part.GetComponent<Collider>());
            }

            if (!withEyeLight) return visual;

            // Brillo rojo de los ojos
            var glow = new GameObject("BrilloOjos");
            glow.transform.SetParent(visual, false);
            glow.transform.localPosition = new Vector3(0f, 1.4f, 0.35f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.1f, 0.05f);
            light.intensity = 1.5f;
            light.range = 3f;
            light.shadows = LightShadows.None;
            return visual;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool isStatic = true)
        {
            return Primitive(PrimitiveType.Cube, name, parent, pos, size, mat, isStatic);
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool isStatic = true)
        {
            return Primitive(PrimitiveType.Cylinder, name, parent, pos, scale, mat, isStatic);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos,
                                            Vector3 scale, Material mat, bool isStatic)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            if (isStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(go,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic);
            }
            return go;
        }
    }
}
