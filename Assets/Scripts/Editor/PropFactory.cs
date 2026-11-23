using System.Collections.Generic;
using HauntedFactory.Items;
using HauntedFactory.Lighting;
using HauntedFactory.World;
using UnityEditor;
using UnityEngine;

namespace HauntedFactory.EditorTools
{
    /// <summary>Referencias a las partes animables de un juguete construido.</summary>
    internal class ToyParts
    {
        public Transform Visual, Body, LeftArm, RightArm, LeftLeg, RightLeg, Jaw;
        /// <summary>Posición local (respecto a Body) donde va el brillo de los ojos.</summary>
        public Vector3 EyeLightPos;
        public float EyeHeight;
    }

    /// <summary>
    /// Fábrica de utilería con primitivas: regalos, estanterías llenas, peluches,
    /// bloques, carritos, caballito, bastones de caramelo, globos, árbol, tren,
    /// mostrador, muñeco de nieve y los juguetes poseídos.
    /// Todo queda agrupado bajo un objeto raíz para reemplazarlo luego por modelos.
    /// </summary>
    internal static class PropFactory
    {
        private static System.Random random = new System.Random(1225);

        /// <summary>Semilla fija: cada reconstrucción genera exactamente la misma decoración.</summary>
        public static void ResetRandom(int seed) => random = new System.Random(seed);
        public static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
        public static T Pick<T>(T[] items) => items[random.Next(items.Length)];

        // ==================================================================
        // Primitivas
        // ==================================================================

        public static Transform Child(Transform parent, string name, Vector3 localPos = default, float yaw = 0f)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                                           Material mat, bool collider = true, bool isStatic = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (isStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(go,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            return go;
        }

        public static GameObject Box(string n, Transform p, Vector3 pos, Vector3 size, Material m, bool col = true, bool st = true)
            => Primitive(PrimitiveType.Cube, n, p, pos, size, m, col, st);
        public static GameObject Cylinder(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = true, bool st = true)
            => Primitive(PrimitiveType.Cylinder, n, p, pos, scale, m, col, st);
        public static GameObject Sphere(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = true, bool st = true)
            => Primitive(PrimitiveType.Sphere, n, p, pos, scale, m, col, st);
        public static GameObject Capsule(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = true, bool st = true)
            => Primitive(PrimitiveType.Capsule, n, p, pos, scale, m, col, st);

        /// <summary>Agrega AutoTiling para que la textura se repita según el tamaño.</summary>
        public static void Tile(GameObject go, AutoTiling.Mode mode, float tileSize)
        {
            var tiling = go.AddComponent<AutoTiling>();
            var so = new SerializedObject(tiling);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("tileSize").floatValue = tileSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            tiling.Apply();
        }

        private static void Rotate(GameObject go, float x, float y, float z) => go.transform.localRotation = Quaternion.Euler(x, y, z);

        // ==================================================================
        // Regalos y cajas
        // ==================================================================

        /// <summary>Regalo envuelto con listón en cruz y moño. pos = centro de la base.</summary>
        public static Transform Gift(Transform parent, string name, Vector3 pos, Vector3 size, float yaw,
                                     bool collider = true, bool isStatic = true)
        {
            Transform root = Child(parent, name, pos, yaw);
            Material wrap = Pick(MaterialLibrary.Wraps);
            Material ribbon = Pick(MaterialLibrary.Ribbons);
            float h = size.y * 0.5f;

            Box("Caja", root, new Vector3(0f, h, 0f), size, wrap, collider, isStatic);
            Box("Liston_A", root, new Vector3(0f, h, 0f),
                new Vector3(size.x + 0.01f, size.y + 0.01f, Mathf.Min(size.z * 0.14f, 0.08f)), ribbon, false, isStatic);
            Box("Liston_B", root, new Vector3(0f, h, 0f),
                new Vector3(Mathf.Min(size.x * 0.14f, 0.08f), size.y + 0.01f, size.z + 0.01f), ribbon, false, isStatic);

            float bow = Mathf.Min(size.x, size.z) * 0.3f;
            GameObject a = Sphere("Moño_A", root, new Vector3(-bow * 0.35f, size.y + bow * 0.15f, 0f),
                                  new Vector3(bow, bow * 0.45f, bow * 0.5f), ribbon, false, isStatic);
            GameObject b = Sphere("Moño_B", root, new Vector3(bow * 0.35f, size.y + bow * 0.15f, 0f),
                                  new Vector3(bow, bow * 0.45f, bow * 0.5f), ribbon, false, isStatic);
            Rotate(a, 0f, 0f, 25f);
            Rotate(b, 0f, 0f, -25f);
            return root;
        }

        /// <summary>Montaña de regalos: una capa en círculo y otra encima.</summary>
        public static Transform GiftPile(Transform parent, string name, Vector3 center, float minRadius, float maxRadius, int count)
        {
            Transform root = Child(parent, name, center);
            var bases = new List<KeyValuePair<Vector3, Vector3>>();
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + Range(-0.3f, 0.3f);
                float r = Range(minRadius, maxRadius);
                var size = new Vector3(Range(0.35f, 0.8f), Range(0.3f, 0.7f), Range(0.35f, 0.8f));
                var pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                Gift(root, "Regalo_" + i, pos, size, Range(0f, 90f));
                bases.Add(new KeyValuePair<Vector3, Vector3>(pos, size));
            }
            for (int j = 0; j < count / 2; j++)
            {
                KeyValuePair<Vector3, Vector3> baseGift = bases[(j * 2) % bases.Count];
                Vector3 size = baseGift.Value * Range(0.5f, 0.75f);
                Gift(root, "RegaloArriba_" + j, baseGift.Key + Vector3.up * baseGift.Value.y, size, Range(0f, 90f), false);
            }
            return root;
        }

        public static Transform CardboardBox(Transform parent, string name, Vector3 pos, Vector3 size, float yaw,
                                             bool collider = true)
        {
            Transform root = Child(parent, name, pos, yaw);
            Box("Caja", root, new Vector3(0f, size.y * 0.5f, 0f), size, MaterialLibrary.Cardboard, collider);
            return root;
        }

        /// <summary>Tarima con cajas de cartón apiladas.</summary>
        public static Transform Pallet(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            Box("Tarima", root, new Vector3(0f, 0.07f, 0f), new Vector3(1.2f, 0.14f, 1.2f), MaterialLibrary.Wood);
            CardboardBox(root, "Caja_1", new Vector3(-0.3f, 0.14f, -0.3f), new Vector3(0.55f, 0.5f, 0.55f), 0f);
            CardboardBox(root, "Caja_2", new Vector3(0.3f, 0.14f, -0.3f), new Vector3(0.55f, 0.45f, 0.55f), 5f);
            CardboardBox(root, "Caja_3", new Vector3(-0.25f, 0.14f, 0.3f), new Vector3(0.6f, 0.55f, 0.55f), -4f);
            CardboardBox(root, "Caja_4", new Vector3(0f, 0.64f, -0.25f), new Vector3(0.6f, 0.45f, 0.6f), 12f, false);
            return root;
        }

        // ==================================================================
        // Juguetes pequeños (decoración, sin collider)
        // ==================================================================

        public static Transform TeddyBear(Transform parent, string name, Vector3 pos, float scale, float yaw, Material fur)
        {
            Transform root = Child(parent, name, pos, yaw);
            root.localScale = Vector3.one * scale;
            Material light = MaterialLibrary.FurLight;
            Material black = MaterialLibrary.Black;
            Sphere("Cuerpo", root, new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.55f, 0.42f), fur, false);
            Sphere("Panza", root, new Vector3(0f, 0.3f, 0.15f), new Vector3(0.32f, 0.36f, 0.16f), light, false);
            Sphere("Cabeza", root, new Vector3(0f, 0.75f, 0.02f), new Vector3(0.42f, 0.4f, 0.38f), fur, false);
            Sphere("Oreja_Izq", root, new Vector3(-0.15f, 0.93f, 0f), Vector3.one * 0.15f, fur, false);
            Sphere("Oreja_Der", root, new Vector3(0.15f, 0.93f, 0f), Vector3.one * 0.15f, fur, false);
            Sphere("Hocico", root, new Vector3(0f, 0.7f, 0.18f), new Vector3(0.17f, 0.13f, 0.12f), light, false);
            Sphere("Nariz", root, new Vector3(0f, 0.73f, 0.24f), Vector3.one * 0.05f, black, false);
            Sphere("Ojo_Izq", root, new Vector3(-0.08f, 0.8f, 0.17f), Vector3.one * 0.05f, black, false);
            Sphere("Ojo_Der", root, new Vector3(0.08f, 0.8f, 0.17f), Vector3.one * 0.05f, black, false);
            Sphere("Pata_Izq", root, new Vector3(-0.15f, 0.08f, 0.15f), new Vector3(0.17f, 0.15f, 0.25f), fur, false);
            Sphere("Pata_Der", root, new Vector3(0.15f, 0.08f, 0.15f), new Vector3(0.17f, 0.15f, 0.25f), fur, false);
            Sphere("Brazo_Izq", root, new Vector3(-0.26f, 0.38f, 0.06f), new Vector3(0.14f, 0.24f, 0.14f), fur, false);
            Sphere("Brazo_Der", root, new Vector3(0.26f, 0.38f, 0.06f), new Vector3(0.14f, 0.24f, 0.14f), fur, false);
            return root;
        }

        public static Transform Ball(Transform parent, string name, Vector3 pos, float radius)
        {
            Transform root = Child(parent, name, pos);
            Sphere("Pelota", root, new Vector3(0f, radius, 0f), Vector3.one * radius * 2f, Pick(MaterialLibrary.ToyPlastics), false);
            return root;
        }

        public static Transform Blocks(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            const float s = 0.16f;
            Box("Bloque_1", root, new Vector3(-0.09f, s * 0.5f, 0f), Vector3.one * s, Pick(MaterialLibrary.ToyPlastics), false);
            Box("Bloque_2", root, new Vector3(0.09f, s * 0.5f, 0.02f), Vector3.one * s, Pick(MaterialLibrary.ToyPlastics), false);
            GameObject top = Box("Bloque_3", root, new Vector3(0f, s * 1.5f, 0f), Vector3.one * s, Pick(MaterialLibrary.ToyPlastics), false);
            Rotate(top, 0f, 25f, 0f);
            return root;
        }

        public static Transform ToyCar(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            Material paint = Pick(MaterialLibrary.ToyPlastics);
            Box("Carroceria", root, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.09f, 0.38f), paint, false);
            Box("Cabina", root, new Vector3(0f, 0.18f, -0.03f), new Vector3(0.17f, 0.08f, 0.18f), paint, false);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.11f : 0.11f;
                float z = i < 2 ? 0.12f : -0.12f;
                GameObject w = Cylinder("Rueda_" + i, root, new Vector3(x, 0.05f, z), new Vector3(0.09f, 0.02f, 0.09f), MaterialLibrary.Black, false);
                Rotate(w, 0f, 0f, 90f);
            }
            return root;
        }

        /// <summary>Caballito de madera blanco con montura roja.</summary>
        public static Transform RockingHorse(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            Material body = MaterialLibrary.White;
            Box("Balancin_Izq", root, new Vector3(-0.18f, 0.04f, 0f), new Vector3(0.06f, 0.08f, 1.3f), MaterialLibrary.NutcrackerRed);
            Box("Balancin_Der", root, new Vector3(0.18f, 0.04f, 0f), new Vector3(0.06f, 0.08f, 1.3f), MaterialLibrary.NutcrackerRed, false);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.18f : 0.18f;
                float z = i < 2 ? 0.38f : -0.38f;
                Cylinder("Pata_" + i, root, new Vector3(x, 0.3f, z), new Vector3(0.08f, 0.22f, 0.08f), body, false);
            }
            GameObject torso = Capsule("Cuerpo", root, new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.46f, 0.42f), body, false);
            Rotate(torso, 90f, 0f, 0f);
            GameObject neck = Box("Cuello", root, new Vector3(0f, 0.92f, 0.4f), new Vector3(0.18f, 0.42f, 0.2f), body, false);
            Rotate(neck, -25f, 0f, 0f);
            Box("Cabeza", root, new Vector3(0f, 1.12f, 0.57f), new Vector3(0.2f, 0.2f, 0.4f), body, false);
            Box("Crin", root, new Vector3(0f, 1.0f, 0.3f), new Vector3(0.06f, 0.38f, 0.12f), MaterialLibrary.Black, false);
            Box("Montura", root, new Vector3(0f, 0.85f, -0.05f), new Vector3(0.46f, 0.06f, 0.4f), MaterialLibrary.NutcrackerRed, false);
            Sphere("Ojo_Izq", root, new Vector3(-0.1f, 1.17f, 0.62f), Vector3.one * 0.05f, MaterialLibrary.Black, false);
            Sphere("Ojo_Der", root, new Vector3(0.1f, 1.17f, 0.62f), Vector3.one * 0.05f, MaterialLibrary.Black, false);
            return root;
        }

        /// <summary>Bastón de caramelo gigante con gancho curvo.</summary>
        public static Transform CandyCane(Transform parent, string name, Vector3 pos, float height, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            Material mat = MaterialLibrary.CandyCane;
            Cylinder("Baston", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.18f, height * 0.5f, 0.18f), mat);

            // Gancho: segmentos sobre un semicírculo en el plano YZ
            const float r = 0.25f;
            const int segments = 6;
            var center = new Vector3(0f, height, r);
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.PI - i * Mathf.PI / segments;
                float a1 = Mathf.PI - (i + 1) * Mathf.PI / segments;
                Vector3 p0 = center + new Vector3(0f, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r);
                Vector3 p1 = center + new Vector3(0f, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);
                Vector3 dir = p1 - p0;
                GameObject seg = Cylinder("Gancho_" + i, root, (p0 + p1) * 0.5f,
                                          new Vector3(0.18f, dir.magnitude * 0.5f + 0.03f, 0.18f), mat, false);
                seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
            return root;
        }

        /// <summary>Globo flotante con hilo (se mece con Bobbing).</summary>
        public static Transform Balloon(Transform parent, string name, Vector3 pos)
        {
            Transform root = Child(parent, name, pos);
            Sphere("Globo", root, Vector3.zero, new Vector3(0.4f, 0.5f, 0.4f), Pick(MaterialLibrary.ToyPlastics), false, false);
            Cylinder("Hilo", root, new Vector3(0f, -0.85f, 0f), new Vector3(0.01f, 0.6f, 0.01f), MaterialLibrary.White, false, false);
            root.gameObject.AddComponent<Bobbing>();
            return root;
        }

        public static Transform CheckoutCounter(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            GameObject counter = Box("Mostrador", root, new Vector3(0f, 0.5f, 0f), new Vector3(5f, 1f, 0.8f), MaterialLibrary.Wood);
            Tile(counter, AutoTiling.Mode.Wall, 1.5f);
            Box("Cubierta", root, new Vector3(0f, 1.03f, 0f), new Vector3(5.1f, 0.06f, 0.9f), MaterialLibrary.DarkWood, false);
            Box("CajaRegistradora", root, new Vector3(1.2f, 1.21f, 0f), new Vector3(0.5f, 0.3f, 0.4f), MaterialLibrary.Black, false);
            GameObject screen = Box("Pantalla", root, new Vector3(1.2f, 1.45f, -0.1f), new Vector3(0.35f, 0.2f, 0.04f), MaterialLibrary.Screen, false);
            Rotate(screen, -20f, 0f, 0f);
            Gift(root, "RegaloOlvidado", new Vector3(-1.4f, 1.06f, 0f), new Vector3(0.4f, 0.3f, 0.35f), 15f, false);
            TeddyBear(root, "OsitoMostrador", new Vector3(-0.4f, 1.06f, 0.05f), 0.45f, 160f, MaterialLibrary.FurColors[3]);
            return root;
        }

        /// <summary>Muñeco de nieve con ojos rojos brillantes (Navidad "distinta").</summary>
        public static Transform Snowman(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform root = Child(parent, name, pos, yaw);
            Material snow = MaterialLibrary.Snow;
            Sphere("Base", root, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.9f, snow);
            Sphere("Torso", root, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.65f, snow, false);
            Sphere("Cabeza", root, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.45f, snow, false);
            Sphere("Ojo_Izq", root, new Vector3(-0.08f, 1.66f, 0.2f), Vector3.one * 0.06f, MaterialLibrary.EyesRed, false);
            Sphere("Ojo_Der", root, new Vector3(0.08f, 1.66f, 0.2f), Vector3.one * 0.06f, MaterialLibrary.EyesRed, false);
            GameObject nose = Cylinder("Nariz", root, new Vector3(0f, 1.6f, 0.3f), new Vector3(0.05f, 0.1f, 0.05f), MaterialLibrary.Carrot, false);
            Rotate(nose, 90f, 0f, 0f);
            Cylinder("Bufanda", root, new Vector3(0f, 1.38f, 0f), new Vector3(0.48f, 0.05f, 0.48f), MaterialLibrary.NutcrackerRed, false);
            Cylinder("Sombrero", root, new Vector3(0f, 1.95f, 0f), new Vector3(0.3f, 0.14f, 0.3f), MaterialLibrary.Black, false);
            Cylinder("Ala", root, new Vector3(0f, 1.81f, 0f), new Vector3(0.45f, 0.01f, 0.45f), MaterialLibrary.Black, false);
            return root;
        }

        // ==================================================================
        // Estanterías llenas de juguetes
        // ==================================================================

        public enum ShelfFill { Mixed, Gifts, Teddies, Boxes }

        /// <summary>
        /// Estantería con repisas llenas. El fondo queda hacia -Z local:
        /// para pegarla a una pared, gira la estantería para que -Z apunte a la pared.
        /// </summary>
        public static Transform ShelfUnit(Transform parent, string name, Vector3 pos, float yaw, float width,
                                          float height, float depth, int levels, ShelfFill fill)
        {
            Transform root = Child(parent, name, pos, yaw);
            Box("Lado_Izq", root, new Vector3(-width * 0.5f + 0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, depth), MaterialLibrary.Wood);
            Box("Lado_Der", root, new Vector3(width * 0.5f - 0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, depth), MaterialLibrary.Wood);
            GameObject back = Box("Fondo", root, new Vector3(0f, height * 0.5f, -depth * 0.5f + 0.02f), new Vector3(width, height, 0.04f), MaterialLibrary.DarkWood);
            Tile(back, AutoTiling.Mode.Wall, 1.5f);
            Box("Repisa_Techo", root, new Vector3(0f, height - 0.025f, 0f), new Vector3(width, 0.05f, depth), MaterialLibrary.Wood, false);

            float gap = height / levels;
            for (int l = 0; l < levels; l++)
            {
                float y = l * gap + 0.04f;
                Box("Repisa_" + l, root, new Vector3(0f, y, 0f), new Vector3(width, 0.05f, depth), MaterialLibrary.Wood, l == 0);
                FillShelf(root, "Nivel_" + l, y + 0.025f, gap - 0.12f, width, depth, fill);
            }
            return root;
        }

        private static void FillShelf(Transform root, string name, float baseY, float maxHeight, float width, float depth, ShelfFill fill)
        {
            Transform level = Child(root, name);
            float x = -width * 0.5f + 0.15f;
            int i = 0;
            while (x < width * 0.5f - 0.3f)
            {
                int kind = ChooseKind(fill);
                float w;
                Vector3 p = new Vector3(0f, baseY, Range(-0.05f, 0.05f) * depth);
                switch (kind)
                {
                    case 0: // regalo
                    {
                        var size = new Vector3(Range(0.22f, 0.4f), Range(0.15f, Mathf.Min(maxHeight, 0.42f)), Mathf.Min(depth - 0.15f, Range(0.22f, 0.4f)));
                        w = size.x;
                        p.x = x + w * 0.5f;
                        Gift(level, "Regalo_" + i, p, size, Range(-8f, 8f), false);
                        break;
                    }
                    case 1: // peluche
                    {
                        float s = Mathf.Min(maxHeight / 1.0f, Range(0.35f, 0.55f));
                        w = 0.5f * s;
                        p.x = x + w * 0.5f;
                        TeddyBear(level, "Peluche_" + i, p, s, Range(-20f, 20f), Pick(MaterialLibrary.FurColors));
                        break;
                    }
                    case 2: // pelota
                    {
                        float r = Mathf.Min(maxHeight * 0.5f, Range(0.1f, 0.16f));
                        w = r * 2f;
                        p.x = x + r;
                        Ball(level, "Pelota_" + i, p, r);
                        break;
                    }
                    case 3: // bloques
                        w = 0.36f;
                        p.x = x + w * 0.5f;
                        Blocks(level, "Bloques_" + i, p, Range(-15f, 15f));
                        break;
                    case 4: // carrito
                        w = 0.28f;
                        p.x = x + w * 0.5f;
                        ToyCar(level, "Carrito_" + i, p, Range(70f, 110f));
                        break;
                    default: // caja de cartón
                    {
                        var size = new Vector3(Range(0.3f, 0.5f), Range(0.2f, Mathf.Min(maxHeight, 0.45f)), Mathf.Min(depth - 0.1f, 0.4f));
                        w = size.x;
                        p.x = x + w * 0.5f;
                        CardboardBox(level, "Caja_" + i, p, size, Range(-6f, 6f), false);
                        break;
                    }
                }
                x += w + Range(0.04f, 0.18f);
                i++;
            }
        }

        private static int ChooseKind(ShelfFill fill)
        {
            float r = Range(0f, 1f);
            switch (fill)
            {
                case ShelfFill.Gifts: return r < 0.8f ? 0 : 1;
                case ShelfFill.Teddies: return r < 0.65f ? 1 : (r < 0.85f ? 0 : 2);
                case ShelfFill.Boxes: return r < 0.6f ? 5 : (r < 0.85f ? 1 : 0);
                default: return random.Next(0, 6);
            }
        }

        // ==================================================================
        // Árbol de Navidad y trenecito
        // ==================================================================

        public static readonly float[] TreeLightHeights = { 0.8f, 1.3f, 1.8f, 2.3f };
        public static readonly float[] TreeLightRadii = { 1.32f, 1.12f, 0.87f, 0.62f };

        /// <summary>Árbol en capas con esferas y estrella. pos = base del tronco.</summary>
        public static Transform ChristmasTree(Transform parent, string name, Vector3 pos)
        {
            Transform root = Child(parent, name, pos);
            Cylinder("Tronco", root, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0.3f), MaterialLibrary.Wood);
            float[] diam = { 2.6f, 2.2f, 1.7f, 1.2f, 0.7f };
            float[] ys = { 0.7f, 1.2f, 1.7f, 2.2f, 2.65f };
            for (int i = 0; i < diam.Length; i++)
            {
                Cylinder("Capa_" + i, root, new Vector3(0f, ys[i], 0f), new Vector3(diam[i], 0.3f, diam[i]), MaterialLibrary.TreeGreen, i == 0);
            }

            Material[] ornaments = { MaterialLibrary.NutcrackerRed, MaterialLibrary.Gold, MaterialLibrary.ToyPlastics[1] };
            for (int i = 0; i < 22; i++)
            {
                int layer = i % 4;
                float angle = Range(0f, Mathf.PI * 2f);
                float r = diam[layer] * 0.5f + 0.03f;
                Sphere("Esfera_" + i, root, new Vector3(Mathf.Cos(angle) * r, ys[layer] - 0.12f, Mathf.Sin(angle) * r),
                       Vector3.one * 0.13f, ornaments[i % ornaments.Length], false);
            }
            Sphere("Estrella", root, new Vector3(0f, 3.05f, 0f), new Vector3(0.32f, 0.32f, 0.12f), MaterialLibrary.Star, false);
            return root;
        }

        /// <summary>
        /// Trenecito de juguete que da vueltas (CircularMover) sobre una vía circular.
        /// No tiene colliders. Va fuera de "Nivel" para no afectar al NavMesh.
        /// </summary>
        public static Transform ToyTrain(Transform parent, string name, Vector3 center, float radius)
        {
            Transform root = Child(parent, name, center);

            Transform track = Child(root, "Via");
            const int ties = 32;
            for (int i = 0; i < ties; i++)
            {
                float a = i / (float)ties * Mathf.PI * 2f;
                GameObject tie = Box("Durmiente_" + i, track, new Vector3(Mathf.Cos(a) * radius, 0.015f, Mathf.Sin(a) * radius),
                                     new Vector3(0.36f, 0.03f, 0.07f), MaterialLibrary.DarkWood, false);
                tie.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }

            Transform train = Child(root, "Tren");
            // Gira con los valores por defecto del script (25°/s con paradas aleatorias)
            train.gameObject.AddComponent<CircularMover>();

            for (int k = 0; k < 3; k++)
            {
                float a = -k * (0.6f / radius); // separación de ~0.6 m entre vagones
                var p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                // Rotate(+Y) gira en sentido horario visto desde arriba: tangente = (sen a, 0, -cos a)
                float yaw = Mathf.Atan2(Mathf.Sin(a), -Mathf.Cos(a)) * Mathf.Rad2Deg;
                Transform car = Child(train, k == 0 ? "Locomotora" : "Vagon_" + k, p, yaw);
                if (k == 0) BuildLocomotive(car);
                else BuildWagon(car);
            }
            return root;
        }

        private static void BuildLocomotive(Transform car)
        {
            Box("Base", car, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.1f, 0.5f), MaterialLibrary.NutcrackerRed, false, false);
            GameObject boiler = Cylinder("Caldera", car, new Vector3(0f, 0.28f, 0.08f), new Vector3(0.24f, 0.18f, 0.24f), MaterialLibrary.Black, false, false);
            Rotate(boiler, 90f, 0f, 0f);
            Cylinder("Chimenea", car, new Vector3(0f, 0.45f, 0.18f), new Vector3(0.08f, 0.08f, 0.08f), MaterialLibrary.Black, false, false);
            Box("Cabina", car, new Vector3(0f, 0.35f, -0.16f), new Vector3(0.3f, 0.3f, 0.2f), MaterialLibrary.NutcrackerRed, false, false);
            Box("Techo", car, new Vector3(0f, 0.51f, -0.16f), new Vector3(0.34f, 0.03f, 0.26f), MaterialLibrary.Gold, false, false);
            Sphere("Farol", car, new Vector3(0f, 0.28f, 0.27f), Vector3.one * 0.08f, MaterialLibrary.Star, false, false);
            AddWheels(car);

            var lamp = new GameObject("LuzFarol");
            lamp.transform.SetParent(car, false);
            lamp.transform.localPosition = new Vector3(0f, 0.3f, 0.45f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.5f);
            light.intensity = 0.6f;
            light.range = 2.5f;
            light.shadows = LightShadows.None;
        }

        private static void BuildWagon(Transform car)
        {
            Box("Vagon", car, new Vector3(0f, 0.18f, 0f), new Vector3(0.28f, 0.18f, 0.45f), Pick(MaterialLibrary.ToyPlastics), false, false);
            Gift(car, "Carga", new Vector3(0f, 0.27f, 0f), new Vector3(0.18f, 0.16f, 0.2f), Range(0f, 40f), false, false);
            AddWheels(car);
        }

        private static void AddWheels(Transform car)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.16f : 0.16f;
                float z = i < 2 ? 0.15f : -0.15f;
                GameObject w = Cylinder("Rueda_" + i, car, new Vector3(x, 0.06f, z), new Vector3(0.11f, 0.02f, 0.11f), MaterialLibrary.Black, false, false);
                Rotate(w, 0f, 0f, 90f);
            }
        }

        // ==================================================================
        // Juguetes poseídos (enemigo) y su variante de peluche
        // ==================================================================

        /// <summary>Cascanueces poseído (~2.3 m): chaqueta roja, sombrero alto, mandíbula móvil.</summary>
        public static ToyParts BuildNutcracker(Transform toy)
        {
            var parts = new ToyParts();
            parts.Visual = Child(toy, "Visual");
            Transform body = parts.Body = Child(parts.Visual, "Cuerpo");
            Material red = MaterialLibrary.NutcrackerRed, gold = MaterialLibrary.Gold, white = MaterialLibrary.White;
            Material black = MaterialLibrary.Black, skin = MaterialLibrary.Skin;

            parts.LeftLeg = Child(body, "Pierna_Izq", new Vector3(-0.12f, 0.62f, 0f));
            parts.RightLeg = Child(body, "Pierna_Der", new Vector3(0.12f, 0.62f, 0f));
            foreach (Transform leg in new[] { parts.LeftLeg, parts.RightLeg })
            {
                Cylinder("Muslo", leg, new Vector3(0f, -0.25f, 0f), new Vector3(0.15f, 0.25f, 0.15f), white, false, false);
                Box("Bota", leg, new Vector3(0f, -0.52f, 0.03f), new Vector3(0.18f, 0.2f, 0.24f), black, false, false);
            }

            Box("Chaqueta", body, new Vector3(0f, 0.95f, 0f), new Vector3(0.56f, 0.6f, 0.36f), red, false, false);
            Box("Cinturon", body, new Vector3(0f, 0.72f, 0f), new Vector3(0.58f, 0.08f, 0.38f), black, false, false);
            Box("Hebilla", body, new Vector3(0f, 0.72f, 0.19f), new Vector3(0.1f, 0.07f, 0.02f), gold, false, false);
            for (int i = 0; i < 3; i++)
            {
                Sphere("Boton_" + i, body, new Vector3(0f, 0.88f + i * 0.12f, 0.18f), Vector3.one * 0.05f, gold, false, false);
            }
            Sphere("Charretera_Izq", body, new Vector3(-0.3f, 1.24f, 0f), new Vector3(0.2f, 0.07f, 0.22f), gold, false, false);
            Sphere("Charretera_Der", body, new Vector3(0.3f, 1.24f, 0f), new Vector3(0.2f, 0.07f, 0.22f), gold, false, false);

            parts.LeftArm = Child(body, "Brazo_Izq", new Vector3(-0.35f, 1.2f, 0f));
            parts.RightArm = Child(body, "Brazo_Der", new Vector3(0.35f, 1.2f, 0f));
            foreach (Transform arm in new[] { parts.LeftArm, parts.RightArm })
            {
                Box("Manga", arm, new Vector3(0f, -0.22f, 0f), new Vector3(0.13f, 0.44f, 0.13f), red, false, false);
                Box("Puño", arm, new Vector3(0f, -0.44f, 0f), new Vector3(0.15f, 0.05f, 0.15f), gold, false, false);
                Sphere("Mano", arm, new Vector3(0f, -0.52f, 0f), Vector3.one * 0.12f, white, false, false);
            }

            Box("Cabeza", body, new Vector3(0f, 1.46f, 0f), new Vector3(0.36f, 0.4f, 0.33f), skin, false, false);
            Sphere("Mejilla_Izq", body, new Vector3(-0.13f, 1.43f, 0.16f), Vector3.one * 0.07f, MaterialLibrary.Cheek, false, false);
            Sphere("Mejilla_Der", body, new Vector3(0.13f, 1.43f, 0.16f), Vector3.one * 0.07f, MaterialLibrary.Cheek, false, false);
            Sphere("Ojo_Izq", body, new Vector3(-0.08f, 1.52f, 0.168f), new Vector3(0.07f, 0.07f, 0.03f), MaterialLibrary.EyesRed, false, false);
            Sphere("Ojo_Der", body, new Vector3(0.08f, 1.52f, 0.168f), new Vector3(0.07f, 0.07f, 0.03f), MaterialLibrary.EyesRed, false, false);
            GameObject browL = Box("Ceja_Izq", body, new Vector3(-0.08f, 1.59f, 0.172f), new Vector3(0.11f, 0.025f, 0.02f), black, false, false);
            GameObject browR = Box("Ceja_Der", body, new Vector3(0.08f, 1.59f, 0.172f), new Vector3(0.11f, 0.025f, 0.02f), black, false, false);
            Rotate(browL, 0f, 0f, -22f);
            Rotate(browR, 0f, 0f, 22f);
            Box("Nariz", body, new Vector3(0f, 1.47f, 0.19f), new Vector3(0.06f, 0.1f, 0.06f), skin, false, false);
            Box("Bigote", body, new Vector3(0f, 1.4f, 0.175f), new Vector3(0.24f, 0.05f, 0.04f), white, false, false);

            parts.Jaw = Child(body, "Mandibula", new Vector3(0f, 1.36f, 0.05f));
            Box("Boca", parts.Jaw, new Vector3(0f, -0.06f, 0.08f), new Vector3(0.26f, 0.09f, 0.12f), black, false, false);
            Box("Dientes", parts.Jaw, new Vector3(0f, -0.02f, 0.145f), new Vector3(0.2f, 0.025f, 0.01f), white, false, false);
            Box("Barba", parts.Jaw, new Vector3(0f, -0.13f, 0.1f), new Vector3(0.3f, 0.12f, 0.1f), white, false, false);

            Cylinder("Sombrero", body, new Vector3(0f, 1.9f, 0f), new Vector3(0.38f, 0.24f, 0.38f), black, false, false);
            Cylinder("Banda", body, new Vector3(0f, 1.7f, 0f), new Vector3(0.39f, 0.03f, 0.39f), gold, false, false);
            Sphere("Insignia", body, new Vector3(0f, 1.86f, 0.19f), new Vector3(0.1f, 0.12f, 0.03f), gold, false, false);
            Sphere("Penacho", body, new Vector3(0f, 2.24f, 0f), new Vector3(0.14f, 0.22f, 0.14f), white, false, false);

            parts.EyeLightPos = new Vector3(0f, 1.52f, 0.4f);
            parts.EyeHeight = 1.52f;
            return parts;
        }

        /// <summary>Oso de peluche poseído (~1.8 m): sucio, con parche, un ojo de botón rojo y otro cosido.</summary>
        public static ToyParts BuildPossessedTeddy(Transform toy)
        {
            var parts = new ToyParts();
            parts.Visual = Child(toy, "Visual");
            Transform body = parts.Body = Child(parts.Visual, "Cuerpo");
            Material fur = MaterialLibrary.FurDirty, furLight = MaterialLibrary.FurLight, black = MaterialLibrary.Black;

            parts.LeftLeg = Child(body, "Pierna_Izq", new Vector3(-0.22f, 0.36f, 0f));
            parts.RightLeg = Child(body, "Pierna_Der", new Vector3(0.22f, 0.36f, 0f));
            foreach (Transform leg in new[] { parts.LeftLeg, parts.RightLeg })
            {
                Sphere("Pata", leg, new Vector3(0f, -0.18f, 0.05f), new Vector3(0.34f, 0.36f, 0.42f), fur, false, false);
            }

            Sphere("Torso", body, new Vector3(0f, 0.8f, 0f), new Vector3(0.85f, 0.95f, 0.72f), fur, false, false);
            Sphere("Panza", body, new Vector3(0f, 0.75f, 0.25f), new Vector3(0.55f, 0.6f, 0.25f), furLight, false, false);
            GameObject patch = Box("Parche", body, new Vector3(0.2f, 0.98f, 0.31f), new Vector3(0.18f, 0.18f, 0.03f), MaterialLibrary.Patch, false, false);
            Rotate(patch, 0f, 15f, 15f);
            Box("Costura", body, new Vector3(-0.05f, 0.75f, 0.375f), new Vector3(0.015f, 0.32f, 0.01f), black, false, false);
            for (int i = 0; i < 4; i++)
            {
                Box("Puntada_" + i, body, new Vector3(-0.05f, 0.63f + i * 0.08f, 0.38f), new Vector3(0.08f, 0.012f, 0.01f), black, false, false);
            }

            parts.LeftArm = Child(body, "Brazo_Izq", new Vector3(-0.42f, 1.12f, 0f));
            parts.RightArm = Child(body, "Brazo_Der", new Vector3(0.42f, 1.12f, 0f));
            foreach (Transform arm in new[] { parts.LeftArm, parts.RightArm })
            {
                Capsule("Brazo", arm, new Vector3(0f, -0.28f, 0f), new Vector3(0.24f, 0.3f, 0.24f), fur, false, false);
            }

            Sphere("Cabeza", body, new Vector3(0f, 1.48f, 0f), new Vector3(0.62f, 0.58f, 0.56f), fur, false, false);
            Sphere("Oreja_Izq", body, new Vector3(-0.25f, 1.74f, 0f), new Vector3(0.22f, 0.22f, 0.12f), fur, false, false);
            Sphere("Oreja_Der_Rota", body, new Vector3(0.24f, 1.7f, 0f), new Vector3(0.16f, 0.13f, 0.1f), fur, false, false);
            Sphere("Hocico", body, new Vector3(0f, 1.42f, 0.24f), new Vector3(0.26f, 0.2f, 0.18f), furLight, false, false);
            Sphere("Nariz", body, new Vector3(0f, 1.47f, 0.33f), new Vector3(0.08f, 0.06f, 0.05f), black, false, false);

            GameObject button = Cylinder("Ojo_Boton", body, new Vector3(-0.12f, 1.56f, 0.26f), new Vector3(0.1f, 0.01f, 0.1f), MaterialLibrary.EyesRed, false, false);
            Rotate(button, 90f, 0f, 0f);
            GameObject x1 = Box("Ojo_Cosido_A", body, new Vector3(0.12f, 1.56f, 0.27f), new Vector3(0.1f, 0.015f, 0.01f), black, false, false);
            GameObject x2 = Box("Ojo_Cosido_B", body, new Vector3(0.12f, 1.56f, 0.27f), new Vector3(0.1f, 0.015f, 0.01f), black, false, false);
            Rotate(x1, 0f, 0f, 45f);
            Rotate(x2, 0f, 0f, -45f);

            parts.Jaw = Child(body, "Mandibula", new Vector3(0f, 1.35f, 0.22f));
            Box("Boca", parts.Jaw, new Vector3(0f, -0.02f, 0.07f), new Vector3(0.18f, 0.03f, 0.02f), black, false, false);

            Box("Moño_Centro", body, new Vector3(0f, 1.2f, 0.29f), new Vector3(0.08f, 0.07f, 0.05f), MaterialLibrary.NutcrackerRed, false, false);
            Sphere("Moño_Izq", body, new Vector3(-0.08f, 1.2f, 0.28f), new Vector3(0.12f, 0.1f, 0.05f), MaterialLibrary.NutcrackerRed, false, false);
            Sphere("Moño_Der", body, new Vector3(0.08f, 1.2f, 0.28f), new Vector3(0.12f, 0.1f, 0.05f), MaterialLibrary.NutcrackerRed, false, false);

            parts.EyeLightPos = new Vector3(-0.12f, 1.56f, 0.45f);
            parts.EyeHeight = 1.56f;
            return parts;
        }

        /// <summary>Brillo rojo de los ojos (luz pequeña que se mueve con el cuerpo).</summary>
        public static Light AddEyeGlow(ToyParts parts, float intensity = 1.5f, float range = 3f)
        {
            var glow = new GameObject("BrilloOjos");
            glow.transform.SetParent(parts.Body, false);
            glow.transform.localPosition = parts.EyeLightPos;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.1f, 0.05f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
