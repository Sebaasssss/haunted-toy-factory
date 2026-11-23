using System.Collections.Generic;
using System.IO;
using HauntedFactory.Core;
using HauntedFactory.Enemy;
using HauntedFactory.Items;
using HauntedFactory.Player;
using HauntedFactory.World;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

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
        private const string ScenePath = "Assets/Scenes/Factory.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string NavMeshFolder = "Assets/Scenes/Factory";
        private const string NavMeshPath = NavMeshFolder + "/NavMesh-Factory.asset";
        private const float WallHeight = 4f;
        private const float WallThickness = 0.3f;
        private const float DoorWidth = 4f;

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

        [MenuItem("Tools/Haunted Factory/Construir escena de la fábrica")]
        public static void BuildScene()
        {
            // Guardar la escena abierta si el usuario quiere
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Reconstruir la fábrica",
                    "Ya existe " + ScenePath + ".\nSe va a regenerar (escena y NavMesh) y se perderán los cambios hechos a mano en esa escena.\n\n¿Continuar?",
                    "Sobrescribir", "Cancelar"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMaterials();
            ConfigureEnvironment();

            Transform level = new GameObject("Nivel").transform;
            BuildLayout(level);
            BuildProps(level);
            BuildWorkLights(level);
            BuildBatteries();
            BuildKeys();
            BuildExitDoor(level);
            BakeNavMesh(level.gameObject);
            BuildEnemies();
            GameObject player = BuildPlayer(new Vector3(0f, 0.05f, 3f));
            BuildGameManager(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
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
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
            RenderSettings.fog = false;

            // Luz de luna muy tenue (solo se nota en el exterior)
            var moon = new GameObject("Luz_Luna");
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.65f, 0.9f);
            light.intensity = 0.15f;
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
        // Luces de trabajo temporales (en la Fase 5 se cambian por luces navideñas)
        // ------------------------------------------------------------------

        private static void BuildWorkLights(Transform level)
        {
            Transform lights = Child(level, "Luces");
            PointLight(lights, "Luz_Envoltura", new Vector3(0f, 3.5f, 7f), new Color(1f, 0.75f, 0.5f), 1.5f, 10f);
            PointLight(lights, "Luz_Ensamblaje", new Vector3(0f, 3.5f, 40f), new Color(1f, 0.6f, 0.4f), 1.5f, 12f);
            PointLight(lights, "Luz_Arbol", new Vector3(10f, 2.5f, 47f), new Color(1f, 0.85f, 0.4f), 1.2f, 6f);
            PointLight(lights, "Luz_Almacen", new Vector3(-38f, 3.5f, 40f), new Color(0.6f, 0.7f, 1f), 1f, 10f);
            PointLight(lights, "Luz_Salida", new Vector3(38f, 3.2f, 40f), new Color(1f, 0.15f, 0.1f), 2f, 8f);
        }

        private static void PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
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

            root.AddComponent<BatteryPickup>();
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

            player.AddComponent<PlayerMovement>();
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
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

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
            flashlightGo.AddComponent<Flashlight>();

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

        private static void BuildEnemies()
        {
            Transform parent = new GameObject("Enemigos").transform;

            // Juguete 1: recorre la línea de ensamblaje y a veces baja por el pasillo norte
            CreateToy(parent, "JuguetePoseido_Ensamblaje", new[]
            {
                new Vector3(0f, 0f, 40f),
                new Vector3(11f, 0f, 40f),
                new Vector3(6f, 0f, 48.5f),
                new Vector3(-6f, 0f, 48.5f),
                new Vector3(-11f, 0f, 40f),
                new Vector3(0f, 0f, 22f),
            });

            // Juguete 2: vigila el almacén y el pasillo oeste
            CreateToy(parent, "JuguetePoseido_Almacen", new[]
            {
                new Vector3(-38f, 0f, 40f),
                new Vector3(-40.5f, 0f, 34f),
                new Vector3(-45f, 0f, 41f),
                new Vector3(-31f, 0f, 48f),
                new Vector3(-22f, 0f, 40f),
            });
        }

        /// <summary>
        /// Crea un juguete poseído (cascanueces de primitivas) con NavMeshAgent,
        /// percepción, IA y su ruta de patrulla. Empieza en el primer waypoint.
        /// </summary>
        private static void CreateToy(Transform parent, string name, Vector3[] route)
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
        }

        /// <summary>Cascanueces poseído de primitivas (reemplazable por un modelo).</summary>
        private static void BuildToyVisual(Transform toy)
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
        }

        /// <summary>Agrega la escena a Build Settings (sin quitar las que ya estaban) para poder reiniciarla.</summary>
        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == path) return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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
