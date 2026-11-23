using System.Collections.Generic;
using HauntedFactory.Audio;
using HauntedFactory.Core;
using HauntedFactory.Enemy;
using HauntedFactory.Items;
using HauntedFactory.Lighting;
using HauntedFactory.Player;
using HauntedFactory.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Construye la escena de juego desde el menú
    /// Tools > Haunted Factory > Construir escena de la fábrica.
    ///
    /// La fábrica es una juguetería de Santa a oscuras: sin linterna solo se ven
    /// los foquitos navideños, los letreros de neón y los ojos de los juguetes.
    ///
    /// Plano (vista desde arriba, Z hacia el norte):
    ///
    ///   [Almacén]--pasillo oeste--[Juguetería]--pasillo este--[Caja y salida]--> Patio nevado
    ///                                   |
    ///                             pasillo norte
    ///                                   |
    ///                         [Taller de envoltura] (inicio)
    /// </summary>
    public static class FactorySceneBuilder
    {
        private const string ScenePath = SceneBuildUtility.FactoryPath;
        private const string GeneratedFolder = "Assets/Scenes/Factory";
        private const string NavMeshPath = GeneratedFolder + "/NavMesh-Factory.asset";
        private const string PostFxPath = GeneratedFolder + "/PostFX-Factory.asset";
        private const string AudioFolder = "Assets/Audio/Placeholder/";

        private const float WallHeight = 4f;
        private const float WallThickness = 0.3f;
        private const float DoorWidth = 4f;

        private static readonly Color FogColor = new Color(0.006f, 0.006f, 0.012f);
        private static readonly Color Ambient = new Color(0.006f, 0.006f, 0.012f);
        private const float FogDensity = 0.07f;

        private enum Side { South, North, West, East }
        private enum ToyKind { Nutcracker, Teddy }

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

        [MenuItem("Tools/Haunted Factory/Construir escena de la fábrica", priority = 1)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!SceneBuildUtility.ConfirmOverwrite(ScenePath)) return;
            Build();
        }

        /// <summary>Construye y guarda la escena sin preguntar (las confirmaciones van antes).</summary>
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PropFactory.ResetRandom(1225);

            EnvironmentFactory.ApplyDarkEnvironment(FogColor, FogDensity, Ambient);
            BuildMoonlight();

            // Todo lo que esté dentro de "Nivel" cuenta para el NavMesh
            Transform level = new GameObject("Nivel").transform;
            BuildLayout(level);
            BuildProps(level);
            BuildExitDoor(level);
            BakeNavMesh(level.gameObject);

            // Lo demás va fuera de "Nivel" (no afecta al NavMesh)
            BuildBatteries();
            BuildKeys();
            BuildAnimatedProps();
            BuildLighting();
            BuildSigns();
            EnvironmentFactory.CreatePostFxVolume(PostFxPath, 0.5f);
            EnvironmentFactory.Snowfall("Nevada_Exterior", new Vector3(45f, 6f, 40f), new Vector2(12f, 14f), 150f, 0.6f);
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

        private static void BuildMoonlight()
        {
            // Luz de luna tenue: solo se nota en el patio exterior
            var moon = new GameObject("Luz_Luna");
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.65f, 0.9f);
            light.intensity = 0.35f;
            light.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        // ==================================================================
        // Salas y pasillos
        // ==================================================================

        private static void BuildLayout(Transform level)
        {
            Transform rooms = PropFactory.Child(level, "Salas");

            BuildRoom(rooms, "Taller_Envoltura", -8f, 8f, 0f, 14f, true,
                new Gap(Side.North, 0f));
            BuildRoom(rooms, "Pasillo_Norte", -2f, 2f, 14f, 30f, true,
                new Gap(Side.South, 0f), new Gap(Side.North, 0f));
            BuildRoom(rooms, "Jugueteria", -14f, 14f, 30f, 50f, true,
                new Gap(Side.South, 0f), new Gap(Side.West, 40f), new Gap(Side.East, 40f));
            BuildRoom(rooms, "Pasillo_Oeste", -30f, -14f, 38f, 42f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));
            BuildRoom(rooms, "Almacen_Munecos", -46f, -30f, 30f, 50f, true,
                new Gap(Side.East, 40f));
            BuildRoom(rooms, "Pasillo_Este", 14f, 28f, 38f, 42f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));
            BuildRoom(rooms, "Caja_Salida", 28f, 40f, 33f, 47f, true,
                new Gap(Side.West, 40f), new Gap(Side.East, 40f));
            BuildRoom(rooms, "Patio_Nevado", 40f, 50f, 34f, 46f, false,
                new Gap(Side.West, 40f));
        }

        /// <summary>
        /// Sala rectangular: suelo, techo opcional y 4 paredes con huecos.
        /// Interiores con piso de damero, papel tapiz y zócalo de madera;
        /// el patio sin techo lleva nieve y valla de madera.
        /// </summary>
        private static void BuildRoom(Transform parent, string name, float xMin, float xMax,
                                      float zMin, float zMax, bool interior, params Gap[] gaps)
        {
            Transform root = PropFactory.Child(parent, name);
            float width = xMax - xMin;
            float depth = zMax - zMin;
            float cx = (xMin + xMax) * 0.5f;
            float cz = (zMin + zMax) * 0.5f;

            GameObject floor = PropFactory.Box("Suelo", root, new Vector3(cx, -0.1f, cz), new Vector3(width, 0.2f, depth),
                                               interior ? MaterialLibrary.Floor : MaterialLibrary.Snow);
            PropFactory.Tile(floor, AutoTiling.Mode.Floor, interior ? 2f : 4f);

            if (interior)
            {
                GameObject ceiling = PropFactory.Box("Techo", root, new Vector3(cx, WallHeight + 0.1f, cz),
                                                     new Vector3(width, 0.2f, depth), MaterialLibrary.DarkWood);
                PropFactory.Tile(ceiling, AutoTiling.Mode.Floor, 3f);
            }

            Transform walls = PropFactory.Child(root, "Paredes");
            BuildSide(walls, Side.South, xMin, xMax, zMin, gaps, interior);
            BuildSide(walls, Side.North, xMin, xMax, zMax, gaps, interior);
            BuildSide(walls, Side.West, zMin, zMax, xMin, gaps, interior);
            BuildSide(walls, Side.East, zMin, zMax, xMax, gaps, interior);
        }

        private static void BuildSide(Transform parent, Side side, float from, float to, float fixedCoord,
                                      Gap[] gaps, bool interior)
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
                WallSegment(parent, side, cursor, Mathf.Max(cursor, o.x), fixedCoord, interior);
                cursor = Mathf.Max(cursor, o.y);
            }
            WallSegment(parent, side, cursor, to, fixedCoord, interior);
        }

        private static void WallSegment(Transform parent, Side side, float a, float b, float fixedCoord, bool interior)
        {
            float length = b - a;
            if (length < 0.05f) return;

            float mid = (a + b) * 0.5f;
            bool alongX = side == Side.South || side == Side.North;
            float wallH = interior ? WallHeight : 1.6f; // la valla del patio es baja

            Vector3 pos = alongX ? new Vector3(mid, wallH * 0.5f, fixedCoord) : new Vector3(fixedCoord, wallH * 0.5f, mid);
            Vector3 size = alongX
                ? new Vector3(length + WallThickness, wallH, WallThickness)
                : new Vector3(WallThickness, wallH, length + WallThickness);

            GameObject wall = PropFactory.Box("Pared_" + side, parent, pos, size,
                                              interior ? MaterialLibrary.Wallpaper : MaterialLibrary.DarkWood);
            PropFactory.Tile(wall, AutoTiling.Mode.Wall, interior ? 2f : 1.5f);

            if (!interior) return;

            // Zócalo de madera en la parte baja de la pared
            const float trimH = 1.1f;
            Vector3 trimPos = new Vector3(pos.x, trimH * 0.5f, pos.z);
            Vector3 trimSize = alongX
                ? new Vector3(size.x, trimH, WallThickness + 0.06f)
                : new Vector3(WallThickness + 0.06f, trimH, size.z);
            GameObject trim = PropFactory.Box("Zocalo_" + side, parent, trimPos, trimSize, MaterialLibrary.Wood, false);
            PropFactory.Tile(trim, AutoTiling.Mode.Wall, 1.5f);
        }

        // ==================================================================
        // Utilería: que parezca una juguetería de verdad
        // ==================================================================

        private static void BuildProps(Transform level)
        {
            Transform props = PropFactory.Child(level, "Utileria");
            BuildWrappingRoom(PropFactory.Child(props, "Taller_Envoltura"));
            BuildNorthCorridor(PropFactory.Child(props, "Pasillo_Norte"));
            BuildToyStore(PropFactory.Child(props, "Jugueteria"));
            BuildSideCorridors(PropFactory.Child(props, "Pasillos_Laterales"));
            BuildStorage(PropFactory.Child(props, "Almacen"));
            BuildCheckout(PropFactory.Child(props, "Caja_Salida"));
            BuildYard(PropFactory.Child(props, "Patio"));
        }

        /// <summary>Sala inicial: mesas de envolver, estantes con regalos, montaña de regalos.</summary>
        private static void BuildWrappingRoom(Transform p)
        {
            Vector3[] tables = { new Vector3(-5f, 0f, 5f), new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 10f) };
            for (int i = 0; i < tables.Length; i++)
            {
                Transform table = PropFactory.Child(p, "Mesa_" + (i + 1), tables[i]);
                GameObject top = PropFactory.Box("Cubierta", table, new Vector3(0f, 0.85f, 0f), new Vector3(3f, 0.1f, 1.4f), MaterialLibrary.Wood);
                PropFactory.Tile(top, AutoTiling.Mode.Floor, 1.5f);
                for (int l = 0; l < 4; l++)
                {
                    float x = l % 2 == 0 ? -1.35f : 1.35f;
                    float z = l < 2 ? -0.55f : 0.55f;
                    PropFactory.Box("Pata_" + l, table, new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.8f, 0.1f), MaterialLibrary.DarkWood, l == 0);
                }
                // Regalos a medio envolver y rollos de papel encima
                PropFactory.Gift(table, "Regalo_A", new Vector3(-0.7f, 0.9f, 0.1f), new Vector3(0.45f, 0.35f, 0.4f), 12f, false);
                PropFactory.Gift(table, "Regalo_B", new Vector3(0.2f, 0.9f, -0.2f), new Vector3(0.3f, 0.25f, 0.3f), -20f, false);
                GameObject roll = PropFactory.Cylinder("RolloPapel", table, new Vector3(0.9f, 0.96f, 0.2f), new Vector3(0.12f, 0.5f, 0.12f),
                                                       PropFactory.Pick(MaterialLibrary.Wraps), false);
                roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Estantes pegados a las paredes (el fondo -Z local va contra la pared)
            PropFactory.ShelfUnit(p, "Estante_Oeste", new Vector3(-7.45f, 0f, 4.5f), 90f, 6f, 2.6f, 0.8f, 4, PropFactory.ShelfFill.Gifts);
            PropFactory.ShelfUnit(p, "Estante_Este", new Vector3(7.45f, 0f, 11f), -90f, 5f, 2.6f, 0.8f, 4, PropFactory.ShelfFill.Mixed);

            PropFactory.GiftPile(p, "MontanaRegalos", new Vector3(5.3f, 0f, 10.5f), 0.2f, 1.1f, 9);
            PropFactory.RockingHorse(p, "Caballito", new Vector3(3.3f, 0f, 12f), 210f);
            PropFactory.CandyCane(p, "Baston_Izq", new Vector3(-2.7f, 0f, 13.4f), 2.2f, 180f);
            PropFactory.CandyCane(p, "Baston_Der", new Vector3(2.7f, 0f, 13.4f), 2.2f, 180f);

            // Rollos de papel de regalo parados en la esquina
            for (int i = 0; i < 5; i++)
            {
                PropFactory.Cylinder("Rollo_" + i, p, new Vector3(-4.5f + i * 0.22f, 0.6f, 12.9f), new Vector3(0.14f, 0.6f, 0.14f),
                                     PropFactory.Pick(MaterialLibrary.Wraps), i == 2);
            }
            PropFactory.Blocks(p, "BloquesTirados", new Vector3(1.5f, 0f, 2f), 20f);
            PropFactory.Ball(p, "PelotaTirada", new Vector3(-1.8f, 0f, 7.5f), 0.18f);
        }

        private static void BuildNorthCorridor(Transform p)
        {
            PropFactory.Gift(p, "Regalo_1", new Vector3(-1.55f, 0f, 18f), new Vector3(0.5f, 0.45f, 0.5f), 10f);
            PropFactory.Gift(p, "Regalo_2", new Vector3(1.55f, 0f, 22.5f), new Vector3(0.45f, 0.6f, 0.45f), -15f);
            PropFactory.Gift(p, "Regalo_3", new Vector3(-1.5f, 0f, 26.5f), new Vector3(0.4f, 0.35f, 0.4f), 30f);
            PropFactory.ToyCar(p, "CarritoVolcado", new Vector3(0.9f, 0f, 19.5f), 35f);
            PropFactory.TeddyBear(p, "OsitoTirado", new Vector3(1.45f, 0f, 27.5f), 0.6f, -100f, MaterialLibrary.FurColors[2]);
        }

        /// <summary>Sala central: cintas, estantes, árbol gigante y montaña de regalos.</summary>
        private static void BuildToyStore(Transform p)
        {
            // Cintas transportadoras con juguetes encima
            BuildConveyor(p, "Cinta_SurOeste", new Vector3(-7f, 0f, 35f), 8f);
            BuildConveyor(p, "Cinta_SurEste", new Vector3(7f, 0f, 35f), 8f);
            BuildConveyor(p, "Cinta_Norte", new Vector3(-2.5f, 0f, 45f), 17f);

            BuildMachine(p, "Maquina_1", new Vector3(-12f, 0f, 45f));
            BuildMachine(p, "Maquina_2", new Vector3(12f, 0f, 35f));

            // Estantes contra la pared sur (dejando libre la puerta del pasillo)
            PropFactory.ShelfUnit(p, "Estante_Sur_1", new Vector3(-10.5f, 0f, 30.55f), 0f, 5f, 3f, 0.8f, 4, PropFactory.ShelfFill.Teddies);
            PropFactory.ShelfUnit(p, "Estante_Sur_2", new Vector3(-5.2f, 0f, 30.55f), 0f, 4.5f, 3f, 0.8f, 4, PropFactory.ShelfFill.Mixed);
            PropFactory.ShelfUnit(p, "Estante_Sur_3", new Vector3(5.2f, 0f, 30.55f), 0f, 4.5f, 3f, 0.8f, 4, PropFactory.ShelfFill.Mixed);
            PropFactory.ShelfUnit(p, "Estante_Sur_4", new Vector3(10.5f, 0f, 30.55f), 0f, 5f, 3f, 0.8f, 4, PropFactory.ShelfFill.Gifts);
            // Pared norte (oeste), lejos de la ruta del juguete
            PropFactory.ShelfUnit(p, "Estante_Norte", new Vector3(-10.5f, 0f, 49.45f), 180f, 5f, 3f, 0.8f, 4, PropFactory.ShelfFill.Teddies);

            PropFactory.ChristmasTree(p, "ArbolNavidad", new Vector3(10f, 0f, 47.5f));
            PropFactory.GiftPile(p, "RegalosArbol", new Vector3(10f, 0f, 47.5f), 1.0f, 1.6f, 10);
            PropFactory.GiftPile(p, "MontanaRegalos", new Vector3(-12.3f, 0f, 33f), 0.2f, 0.9f, 8);
            PropFactory.CandyCane(p, "Baston_Entrada_Izq", new Vector3(-2.6f, 0f, 30.8f), 2.4f, 0f);
            PropFactory.CandyCane(p, "Baston_Entrada_Der", new Vector3(2.6f, 0f, 30.8f), 2.4f, 0f);
        }

        private static void BuildConveyor(Transform parent, string name, Vector3 center, float length)
        {
            Transform root = PropFactory.Child(parent, name, center);
            PropFactory.Box("Banda", root, new Vector3(0f, 0.75f, 0f), new Vector3(length, 0.1f, 1.4f), MaterialLibrary.Black);
            PropFactory.Box("Base", root, new Vector3(0f, 0.35f, 0f), new Vector3(length, 0.7f, 1.6f), MaterialLibrary.Metal, false);
            PropFactory.Box("Borde_Izq", root, new Vector3(0f, 0.85f, -0.75f), new Vector3(length, 0.12f, 0.06f), MaterialLibrary.Gold, false);
            PropFactory.Box("Borde_Der", root, new Vector3(0f, 0.85f, 0.75f), new Vector3(length, 0.12f, 0.06f), MaterialLibrary.Gold, false);

            // Juguetes a medio fabricar sobre la banda
            float x = -length * 0.5f + 0.7f;
            int i = 0;
            while (x < length * 0.5f - 0.5f)
            {
                var pos = new Vector3(x, 0.8f, PropFactory.Range(-0.3f, 0.3f));
                switch (i % 4)
                {
                    case 0: PropFactory.Gift(root, "Regalo_" + i, pos, new Vector3(0.4f, 0.35f, 0.4f), PropFactory.Range(0f, 45f), false); break;
                    case 1: PropFactory.TeddyBear(root, "Peluche_" + i, pos, 0.5f, PropFactory.Range(-40f, 40f), PropFactory.Pick(MaterialLibrary.FurColors)); break;
                    case 2: PropFactory.ToyCar(root, "Carrito_" + i, pos, PropFactory.Range(0f, 180f)); break;
                    default: PropFactory.Blocks(root, "Bloques_" + i, pos, PropFactory.Range(0f, 90f)); break;
                }
                x += PropFactory.Range(1.1f, 1.8f);
                i++;
            }
        }

        private static void BuildMachine(Transform parent, string name, Vector3 pos)
        {
            Transform root = PropFactory.Child(parent, name, pos);
            PropFactory.Box("Cuerpo", root, new Vector3(0f, 1.25f, 0f), new Vector3(2f, 2.5f, 2.5f), MaterialLibrary.Metal);
            PropFactory.Box("Panel", root, new Vector3(0f, 1.5f, -1.26f), new Vector3(1.2f, 0.6f, 0.02f), MaterialLibrary.Black, false);
            PropFactory.Box("Pantalla", root, new Vector3(-0.25f, 1.55f, -1.28f), new Vector3(0.5f, 0.3f, 0.02f), MaterialLibrary.Screen, false);
            PropFactory.Sphere("Boton_Rojo", root, new Vector3(0.35f, 1.55f, -1.28f), Vector3.one * 0.1f, MaterialLibrary.EyesRed, false);
            PropFactory.Cylinder("Chimenea", root, new Vector3(0.5f, 2.9f, 0.5f), new Vector3(0.4f, 0.4f, 0.4f), MaterialLibrary.Metal, false);
        }

        private static void BuildSideCorridors(Transform p)
        {
            // Pasillo oeste: bloques y un peluche abandonado
            PropFactory.Blocks(p, "Bloques_1", new Vector3(-26f, 0f, 38.6f), 10f);
            PropFactory.Blocks(p, "Bloques_2", new Vector3(-18f, 0f, 41.4f), -30f);
            PropFactory.TeddyBear(p, "Peluche_Roto", new Vector3(-24f, 0f, 41.4f), 0.7f, 200f, MaterialLibrary.FurDirty);
            PropFactory.CardboardBox(p, "Caja_1", new Vector3(-16f, 0f, 38.7f), new Vector3(0.6f, 0.5f, 0.6f), 8f);
            // Pasillo este: regalos apilados contra la pared
            PropFactory.Gift(p, "Regalo_1", new Vector3(17f, 0f, 38.6f), new Vector3(0.6f, 0.5f, 0.6f), 5f);
            PropFactory.Gift(p, "Regalo_2", new Vector3(17.6f, 0f, 38.6f), new Vector3(0.45f, 0.4f, 0.45f), -10f);
            PropFactory.Gift(p, "Regalo_3", new Vector3(25f, 0f, 41.4f), new Vector3(0.5f, 0.7f, 0.5f), 20f);
            PropFactory.Ball(p, "Pelota", new Vector3(22.5f, 0f, 38.8f), 0.2f);
        }

        /// <summary>Almacén de muñecos: estanterías altas llenas, tarimas con cajas.</summary>
        private static void BuildStorage(Transform p)
        {
            // Tres estanterías largas (a lo largo de Z) con pasillos entre ellas
            for (int i = 0; i < 3; i++)
            {
                float x = -43f + i * 5f;
                PropFactory.ShelfUnit(p, "Estanteria_" + (i + 1), new Vector3(x, 0f, 36f), 90f, 6f, 3.2f, 1.2f, 4,
                                      i == 1 ? PropFactory.ShelfFill.Teddies : PropFactory.ShelfFill.Boxes);
            }
            PropFactory.ShelfUnit(p, "Estanteria_Norte", new Vector3(-40f, 0f, 46f), 180f, 8f, 3.2f, 1.2f, 4, PropFactory.ShelfFill.Teddies);

            PropFactory.Pallet(p, "Tarima_1", new Vector3(-31.2f, 0f, 32f), 10f);
            PropFactory.Pallet(p, "Tarima_2", new Vector3(-33.5f, 0f, 44.5f), -15f);
            PropFactory.CardboardBox(p, "Caja_Suelta_1", new Vector3(-35.5f, 0f, 31f), new Vector3(0.7f, 0.6f, 0.7f), 20f);
            PropFactory.TeddyBear(p, "Peluche_Suelo", new Vector3(-36f, 0f, 41.5f), 0.8f, 160f, MaterialLibrary.FurColors[1]);
        }

        /// <summary>Caja registradora y salida.</summary>
        private static void BuildCheckout(Transform p)
        {
            PropFactory.CheckoutCounter(p, "Mostrador", new Vector3(33.5f, 0f, 45.4f), 180f);
            PropFactory.CardboardBox(p, "Caja_1", new Vector3(29.2f, 0f, 46f), new Vector3(1.2f, 1.2f, 1.2f), 0f);
            PropFactory.CardboardBox(p, "Caja_2", new Vector3(37f, 0f, 35f), new Vector3(1f, 1f, 1f), 12f);
            PropFactory.CandyCane(p, "Baston_Salida_Izq", new Vector3(39.4f, 0f, 37.4f), 2.4f, -90f);
            PropFactory.CandyCane(p, "Baston_Salida_Der", new Vector3(39.4f, 0f, 42.6f), 2.4f, -90f);
            PropFactory.GiftPile(p, "RegalosSinEntregar", new Vector3(37.5f, 0f, 45.5f), 0.2f, 0.6f, 5);
        }

        /// <summary>Patio exterior: muñeco de nieve que te observa.</summary>
        private static void BuildYard(Transform p)
        {
            PropFactory.Snowman(p, "MunecoDeNieve", new Vector3(47.5f, 0f, 43.5f), -120f);
        }

        // ==================================================================
        // Objetos animados (fuera del NavMesh)
        // ==================================================================

        private static void BuildAnimatedProps()
        {
            Transform root = new GameObject("Animados").transform;
            PropFactory.ToyTrain(root, "TrenNavideno", new Vector3(10f, 0f, 47.5f), 2.0f);

            // Globos pegados al techo
            Vector3[] balloons =
            {
                new Vector3(-0.8f, 3.1f, 17f), new Vector3(0.6f, 3.2f, 20.5f), new Vector3(-0.4f, 3.0f, 25f),
                new Vector3(-6f, 3.2f, 40f), new Vector3(4f, 3.1f, 38.5f), new Vector3(7f, 3.3f, 42f),
                new Vector3(-3.5f, 3.2f, 8f), new Vector3(33f, 3.1f, 38f),
            };
            for (int i = 0; i < balloons.Length; i++)
            {
                PropFactory.Balloon(root, "Globo_" + i, balloons[i]);
            }
        }

        // ==================================================================
        // Iluminación: muy oscura, los focos brillan pero casi no alumbran
        // ==================================================================

        private static void BuildLighting()
        {
            Transform root = new GameObject("Iluminacion").transform;
            Transform strings = PropFactory.Child(root, "Guirnaldas");
            const float h = 3.65f;
            var chase = ChristmasLightString.Pattern.Chase;
            var alternate = ChristmasLightString.Pattern.Alternate;
            var random = ChristmasLightString.Pattern.Random;

            EnvironmentFactory.LightString(strings, "Guirnalda_Envoltura_Sur", new Vector3(-7.6f, h, 0.35f), new Vector3(7.6f, h, 0.35f), 16, 1, chase);
            EnvironmentFactory.LightString(strings, "Guirnalda_Envoltura_Norte", new Vector3(-7.6f, h, 13.65f), new Vector3(7.6f, h, 13.65f), 16, 1, alternate);
            EnvironmentFactory.LightString(strings, "Guirnalda_PasilloNorte", new Vector3(-1.65f, h, 15f), new Vector3(-1.65f, h, 29f), 14, 1, random);
            EnvironmentFactory.LightString(strings, "Guirnalda_Jugueteria_Sur", new Vector3(-13.6f, h, 30.35f), new Vector3(13.6f, h, 30.35f), 24, 2, chase);
            EnvironmentFactory.LightString(strings, "Guirnalda_Jugueteria_Norte", new Vector3(-13.6f, h, 49.65f), new Vector3(13.6f, h, 49.65f), 24, 2, alternate);
            EnvironmentFactory.LightString(strings, "Guirnalda_Jugueteria_Oeste", new Vector3(-13.65f, h, 31f), new Vector3(-13.65f, h, 49f), 16, 1, chase);
            EnvironmentFactory.LightString(strings, "Guirnalda_Jugueteria_Este", new Vector3(13.65f, h, 31f), new Vector3(13.65f, h, 49f), 16, 1, random);
            EnvironmentFactory.LightString(strings, "Guirnalda_PasilloOeste", new Vector3(-29f, h, 38.35f), new Vector3(-15f, h, 38.35f), 12, 1, random);
            EnvironmentFactory.LightString(strings, "Guirnalda_PasilloEste", new Vector3(15f, h, 41.65f), new Vector3(27f, h, 41.65f), 12, 1, chase);
            EnvironmentFactory.LightString(strings, "Guirnalda_Almacen", new Vector3(-45.6f, h, 49.65f), new Vector3(-30.4f, h, 49.65f), 16, 1, random);
            EnvironmentFactory.LightString(strings, "Guirnalda_Salida", new Vector3(28.4f, h, 46.65f), new Vector3(39.6f, h, 46.65f), 12, 1, alternate, true);
            EnvironmentFactory.TreeLights(strings, "Guirnalda_Arbol", new Vector3(10f, 0f, 47.5f));

            // Solo tres lámparas, débiles y fallando (el almacén queda totalmente a oscuras)
            Transform lamps = PropFactory.Child(root, "Lamparas");
            EnvironmentFactory.BrokenLamp(lamps, "Lampara_Envoltura", new Vector3(0f, 3.95f, 7f), new Color(1f, 0.78f, 0.55f), 1.0f, 7f);
            EnvironmentFactory.BrokenLamp(lamps, "Lampara_Jugueteria", new Vector3(0f, 3.95f, 40f), new Color(1f, 0.7f, 0.45f), 1.2f, 9f);
            EnvironmentFactory.BrokenLamp(lamps, "Lampara_Salida", new Vector3(34f, 3.95f, 40f), new Color(1f, 0.2f, 0.12f), 1.2f, 7f);
        }

        /// <summary>Letreros de neón: ayudan a orientarse en la oscuridad.</summary>
        private static void BuildSigns()
        {
            Transform root = new GameObject("Letreros").transform;
            var pink = new Color(1f, 0.35f, 0.55f);
            var red = new Color(1f, 0.12f, 0.1f);
            var blue = new Color(0.35f, 0.6f, 1f);

            EnvironmentFactory.NeonSign(root, "Neon_Taller", new Vector3(0f, 3.3f, 13.8f), 0f, "TALLER DE ENVOLTURA", new Color(1f, 0.8f, 0.35f), 120, 6f, false);
            EnvironmentFactory.NeonSign(root, "Neon_Jugueteria", new Vector3(0f, 3.2f, 49.8f), 0f, "JUGUETERÍA DE SANTA", pink, 180, 9f, true);
            EnvironmentFactory.NeonSign(root, "Neon_Almacen", new Vector3(-29.8f, 3.4f, 40f), -90f, "ALMACÉN", blue, 160, 4f, true);
            EnvironmentFactory.NeonSign(root, "Neon_Salida", new Vector3(39.8f, 3.4f, 40f), 90f, "SALIDA", red, 180, 3.5f, false);
            EnvironmentFactory.NeonSign(root, "Neon_NoTeVayas", new Vector3(34f, 2.4f, 33.2f), 180f, "NO TE VAYAS...", red, 140, 5f, true);
            EnvironmentFactory.NeonSign(root, "Neon_Juega", new Vector3(21f, 2.5f, 41.8f), 0f, "JUEGA CON NOSOTROS", red, 120, 6f, true);
        }

        // ==================================================================
        // Pilas, llaves y puerta
        // ==================================================================

        private static void BuildBatteries()
        {
            Transform parent = new GameObject("Pilas").transform;
            CreateBattery(parent, "Pila_1", new Vector3(5f, 1.3f, 4.5f));      // sobre una mesa del taller
            CreateBattery(parent, "Pila_2", new Vector3(-12f, 0.6f, 48f));     // esquina noroeste de la juguetería
            CreateBattery(parent, "Pila_3", new Vector3(-44.5f, 0.6f, 32f));   // fondo del almacén
            CreateBattery(parent, "Pila_4", new Vector3(21f, 0.6f, 40.6f));    // pasillo este
            CreateBattery(parent, "Pila_5", new Vector3(-22f, 0.6f, 41.2f));   // pasillo oeste
            CreateBattery(parent, "Pila_6", new Vector3(35f, 1.4f, 45.4f));    // sobre el mostrador
        }

        private static void CreateBattery(Transform parent, string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;

            BatteryPickup pickup = root.AddComponent<BatteryPickup>();
            UIFactory.Wire(pickup, "pickupSound", Clip("sfx_pila"));
            root.AddComponent<Bobbing>();

            PropFactory.Cylinder("Visual", root.transform, Vector3.zero, new Vector3(0.18f, 0.2f, 0.18f), MaterialLibrary.Battery, false, false);
            PropFactory.Cylinder("Polo", root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.03f, 0.08f), MaterialLibrary.Metal, false, false);
        }

        private static void BuildKeys()
        {
            Transform parent = new GameObject("Llaves").transform;
            CreateKey(parent, "Llave_1", new Vector3(-7f, 0.8f, 13f));      // esquina del taller de envoltura
            CreateKey(parent, "Llave_2", new Vector3(12.5f, 0.8f, 48.5f));  // junto al árbol de la juguetería
            CreateKey(parent, "Llave_3", new Vector3(-45f, 0.8f, 48.5f));   // fondo del almacén (a oscuras)
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

            Transform visual = PropFactory.Child(root.transform, "Visual");
            visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject ring = PropFactory.Cylinder("Anillo", visual, new Vector3(0f, 0.25f, 0f), new Vector3(0.25f, 0.03f, 0.25f), MaterialLibrary.Key, false, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            PropFactory.Box("Vastago", visual, new Vector3(0f, -0.05f, 0f), new Vector3(0.05f, 0.45f, 0.05f), MaterialLibrary.Key, false, false);
            PropFactory.Box("Diente_1", visual, new Vector3(0.06f, -0.22f, 0f), new Vector3(0.1f, 0.05f, 0.04f), MaterialLibrary.Key, false, false);
            PropFactory.Box("Diente_2", visual, new Vector3(0.05f, -0.12f, 0f), new Vector3(0.08f, 0.04f, 0.04f), MaterialLibrary.Key, false, false);

            // Brillo muy pequeño: hay que acercarse o alumbrar para encontrarla
            EnvironmentFactory.PointLight(root.transform, "Brillo", pos, new Color(1f, 0.8f, 0.3f), 0.5f, 1.6f);
        }

        /// <summary>Puerta tipo cortina metálica y zona de escape en el patio.</summary>
        private static void BuildExitDoor(Transform level)
        {
            Transform exitRoot = PropFactory.Child(level, "Salida");

            var door = new GameObject("PuertaSalida");
            door.transform.SetParent(exitRoot, false);
            door.transform.localPosition = new Vector3(40f, 0f, 40f);

            var zone = door.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(-1.5f, 1f, 0f);
            zone.size = new Vector3(3f, 2f, DoorWidth);

            ExitDoor exitDoor = door.AddComponent<ExitDoor>();
            GameObject panel = PropFactory.Box("Hoja", door.transform, new Vector3(0f, WallHeight * 0.5f, 0f),
                                               new Vector3(0.25f, WallHeight, DoorWidth), MaterialLibrary.Door, true, false);

            var so = new SerializedObject(exitDoor);
            so.FindProperty("doorPanel").objectReferenceValue = panel.transform;
            so.FindProperty("openSound").objectReferenceValue = Clip("sfx_puerta_abre");
            so.FindProperty("lockedSound").objectReferenceValue = Clip("sfx_puerta_cerrada");
            so.ApplyModifiedPropertiesWithoutUndo();

            var escape = new GameObject("ZonaEscape");
            escape.transform.SetParent(exitRoot, false);
            escape.transform.localPosition = new Vector3(45.5f, 1f, 40f);
            var escapeCol = escape.AddComponent<BoxCollider>();
            escapeCol.isTrigger = true;
            escapeCol.size = new Vector3(8f, 2f, 11f);
            escape.AddComponent<EscapeZone>();
        }

        // ==================================================================
        // NavMesh
        // ==================================================================

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

            MaterialLibrary.EnsureFolder(GeneratedFolder);
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        // ==================================================================
        // Juguetes poseídos (enemigo) y decorativos
        // ==================================================================

        private static EnemyAI[] BuildEnemies()
        {
            Transform parent = new GameObject("Enemigos").transform;

            // Cascanueces: recorre la juguetería y a veces baja por el pasillo norte
            EnemyAI first = CreateToy(parent, "Cascanueces_Poseido", ToyKind.Nutcracker, new[]
            {
                new Vector3(0f, 0f, 40f),
                new Vector3(11f, 0f, 40f),
                new Vector3(6f, 0f, 48.5f),
                new Vector3(-6f, 0f, 48.5f),
                new Vector3(-11f, 0f, 40f),
                new Vector3(0f, 0f, 22f),
            });

            // Oso de peluche: vigila el almacén y el pasillo oeste
            EnemyAI second = CreateToy(parent, "Oso_Poseido", ToyKind.Teddy, new[]
            {
                new Vector3(-38f, 0f, 40.5f),
                new Vector3(-40.5f, 0f, 34f),
                new Vector3(-45f, 0f, 41f),
                new Vector3(-31f, 0f, 48f),
                new Vector3(-22f, 0f, 40f),
            });

            return new[] { first, second };
        }

        private static EnemyAI CreateToy(Transform parent, string name, ToyKind kind, Vector3[] route)
        {
            var toy = new GameObject(name);
            toy.transform.SetParent(parent, false);
            toy.transform.position = route[0];

            var col = toy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.4f;

            var agent = toy.AddComponent<NavMeshAgent>();
            agent.height = 2f;
            agent.radius = 0.4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;

            EnemyPerception perception = toy.AddComponent<EnemyPerception>();
            EnemyAI ai = toy.AddComponent<EnemyAI>();

            ToyParts parts = kind == ToyKind.Nutcracker
                ? PropFactory.BuildNutcracker(toy.transform)
                : PropFactory.BuildPossessedTeddy(toy.transform);
            PropFactory.AddEyeGlow(parts);

            var perceptionSo = new SerializedObject(perception);
            perceptionSo.FindProperty("eyeHeight").floatValue = parts.EyeHeight;
            perceptionSo.ApplyModifiedPropertiesWithoutUndo();

            ToyAnimator animator = toy.AddComponent<ToyAnimator>();
            var animSo = new SerializedObject(animator);
            animSo.FindProperty("body").objectReferenceValue = parts.Body;
            animSo.FindProperty("leftArm").objectReferenceValue = parts.LeftArm;
            animSo.FindProperty("rightArm").objectReferenceValue = parts.RightArm;
            animSo.FindProperty("leftLeg").objectReferenceValue = parts.LeftLeg;
            animSo.FindProperty("rightLeg").objectReferenceValue = parts.RightLeg;
            animSo.FindProperty("jaw").objectReferenceValue = parts.Jaw;
            animSo.ApplyModifiedPropertiesWithoutUndo();

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

            // Ruta de patrulla en un objeto aparte, editable en la escena
            Transform routeRoot = PropFactory.Child(parent, "Ruta_" + name);
            var points = new Object[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                Transform wp = PropFactory.Child(routeRoot, "Waypoint_" + i);
                wp.position = route[i];
                points[i] = wp;
            }
            var aiSo = new SerializedObject(ai);
            EnvironmentFactory.SetObjectArray(aiSo.FindProperty("waypoints"), points);
            aiSo.ApplyModifiedPropertiesWithoutUndo();
            return ai;
        }

        /// <summary>Juguetes quietos que giran para mirarte (solo ambientación).</summary>
        private static void BuildWatchingToys()
        {
            Transform parent = new GameObject("JuguetesDecorativos").transform;
            DecorToy(parent, "Cascanueces_Taller", ToyKind.Nutcracker, new Vector3(7f, 0f, 1f), -45f);
            DecorToy(parent, "Oso_Almacen", ToyKind.Teddy, new Vector3(-45.2f, 0f, 46.8f), 90f);
            DecorToy(parent, "Cascanueces_Salida", ToyKind.Nutcracker, new Vector3(30f, 0f, 34.2f), 30f);
        }

        private static void DecorToy(Transform parent, string name, ToyKind kind, Vector3 pos, float yaw)
        {
            var toy = new GameObject(name);
            toy.transform.SetParent(parent, false);
            toy.transform.position = pos;
            toy.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var col = toy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.35f;

            ToyParts parts = kind == ToyKind.Nutcracker
                ? PropFactory.BuildNutcracker(toy.transform)
                : PropFactory.BuildPossessedTeddy(toy.transform);

            var watch = toy.AddComponent<WatchingToy>();
            UIFactory.Wire(watch, "head", parts.Visual);
        }

        // ==================================================================
        // Jugador (elfo) con cámara, linterna, pasos y balanceo
        // ==================================================================

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

            Transform cameraRoot = PropFactory.Child(player.transform, "CameraRoot", new Vector3(0f, 1.45f, 0f));

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

            // Linterna: la única luz fuerte del juego
            var flashlightGo = new GameObject("Flashlight");
            flashlightGo.transform.SetParent(cameraRoot, false);
            flashlightGo.transform.localPosition = new Vector3(0.25f, -0.2f, 0.2f);
            var spot = flashlightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.93f, 0.8f);
            spot.intensity = 14f;
            spot.range = 22f;
            spot.spotAngle = 50f;
            spot.innerSpotAngle = 20f;
            spot.shadows = LightShadows.Soft;
            var flashlightAudio = flashlightGo.AddComponent<AudioSource>();
            flashlightAudio.playOnAwake = false;
            flashlightAudio.spatialBlend = 0f;
            Flashlight flashlight = flashlightGo.AddComponent<Flashlight>();
            UIFactory.Wire(flashlight, "toggleSound", Clip("sfx_linterna_click"));

            var feet = new GameObject("Pasos");
            feet.transform.SetParent(player.transform, false);
            var feetAudio = feet.AddComponent<AudioSource>();
            feetAudio.playOnAwake = false;
            feetAudio.spatialBlend = 0f;
            FootstepAudio footsteps = feet.AddComponent<FootstepAudio>();
            var feetSo = new SerializedObject(footsteps);
            feetSo.FindProperty("movement").objectReferenceValue = movement;
            EnvironmentFactory.SetObjectArray(feetSo.FindProperty("steps"),
                new Object[] { Clip("sfx_paso_1"), Clip("sfx_paso_2"), Clip("sfx_paso_3") });
            feetSo.ApplyModifiedPropertiesWithoutUndo();

            UIFactory.Wire(look, "cameraRoot", cameraRoot);
            return player;
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

        // ==================================================================
        // Audio
        // ==================================================================

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
            EnvironmentFactory.SetObjectArray(so.FindProperty("enemies"), enemies);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
