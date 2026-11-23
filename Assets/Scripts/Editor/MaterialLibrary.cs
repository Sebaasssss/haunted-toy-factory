using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Catálogo de materiales URP del juego. Cada material se crea una sola vez
    /// en Assets/Materials (si ya existe se reutiliza y no se sobrescribe),
    /// así puedes editarlos a mano sin que el constructor los pise.
    /// Las texturas vienen de Assets/Art/Textures.
    /// </summary>
    internal static class MaterialLibrary
    {
        private const string MaterialsFolder = "Assets/Materials";
        private const string TexturesFolder = "Assets/Art/Textures/";

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        // ---------- Superficies ----------
        public static Material Floor => Get("MT_SueloDamero", Color.white, 0.25f, texture: "T_Suelo_Damero");
        public static Material Wallpaper => Get("MT_PapelPared", Color.white, 0.1f, texture: "T_PapelPared");
        public static Material Wood => Get("MT_Madera", Color.white, 0.25f, texture: "T_Madera");
        public static Material DarkWood => Get("MT_MaderaOscura", new Color(0.45f, 0.38f, 0.35f), 0.2f, texture: "T_Madera");
        public static Material Snow => Get("MT_Nieve", Color.white, 0.3f, texture: "T_Nieve");
        public static Material Cardboard => Get("MT_Carton", Color.white, 0.1f, texture: "T_Carton");
        public static Material CandyCane => Get("MT_Baston", Color.white, 0.7f, texture: "T_Baston");

        // ---------- Regalos ----------
        public static Material[] Wraps => new[]
        {
            Get("MT_Regalo_Rojo", Color.white, 0.45f, texture: "T_Regalo_Rojo"),
            Get("MT_Regalo_Verde", Color.white, 0.45f, texture: "T_Regalo_Verde"),
            Get("MT_Regalo_Azul", Color.white, 0.45f, texture: "T_Regalo_Azul"),
            Get("MT_Regalo_Dorado", Color.white, 0.55f, texture: "T_Regalo_Dorado"),
            Get("MT_Regalo_Morado", Color.white, 0.45f, texture: "T_Regalo_Morado"),
        };

        public static Material[] Ribbons => new[]
        {
            Gold,
            Get("M_Liston_Rojo", new Color(0.75f, 0.05f, 0.08f), 0.7f),
            Get("M_Liston_Blanco", new Color(0.92f, 0.9f, 0.88f), 0.7f),
            Get("M_Liston_Verde", new Color(0.05f, 0.45f, 0.15f), 0.7f),
        };

        // ---------- Colores básicos ----------
        public static Material Black => Get("M_Negro", new Color(0.03f, 0.03f, 0.03f), 0.4f);
        public static Material White => Get("M_Blanco", new Color(0.9f, 0.88f, 0.85f), 0.4f);
        public static Material Gold => Get("M_Dorado", new Color(1f, 0.75f, 0.25f), 0.8f, metallic: 1f);
        public static Material Metal => Get("M_Metal", new Color(0.4f, 0.42f, 0.45f), 0.6f, metallic: 0.8f);
        public static Material NutcrackerRed => Get("M_RojoCascanueces", new Color(0.55f, 0.04f, 0.06f), 0.5f);
        public static Material Skin => Get("M_Juguete_Cara", new Color(0.85f, 0.75f, 0.65f), 0.7f);
        public static Material Cheek => Get("M_Mejilla", new Color(0.85f, 0.35f, 0.35f), 0.5f);
        public static Material Carrot => Get("M_Zanahoria", new Color(0.95f, 0.45f, 0.1f), 0.4f);
        public static Material TreeGreen => Get("M_ArbolPino", new Color(0.08f, 0.22f, 0.12f), 0.1f);

        public static Material[] ToyPlastics => new[]
        {
            Get("M_Plastico_Rojo", new Color(0.8f, 0.1f, 0.1f), 0.8f),
            Get("M_Plastico_Azul", new Color(0.1f, 0.3f, 0.85f), 0.8f),
            Get("M_Plastico_Amarillo", new Color(0.95f, 0.8f, 0.1f), 0.8f),
            Get("M_Plastico_Verde", new Color(0.1f, 0.65f, 0.25f), 0.8f),
            Get("M_Plastico_Morado", new Color(0.5f, 0.2f, 0.7f), 0.8f),
        };

        // ---------- Peluches ----------
        public static Material Fur => Get("MT_Peluche_Cafe", new Color(0.5f, 0.33f, 0.2f), 0.05f, texture: "T_Peluche");
        public static Material FurLight => Get("MT_Peluche_Claro", new Color(0.85f, 0.72f, 0.55f), 0.05f, texture: "T_Peluche");
        public static Material FurDirty => Get("MT_Peluche_Sucio", new Color(0.36f, 0.3f, 0.27f), 0.05f, texture: "T_Peluche");
        public static Material Patch => Get("M_Parche", new Color(0.25f, 0.3f, 0.55f), 0.2f);

        public static Material[] FurColors => new[]
        {
            Fur, FurLight,
            Get("MT_Peluche_Gris", new Color(0.55f, 0.55f, 0.6f), 0.05f, texture: "T_Peluche"),
            Get("MT_Peluche_Rosa", new Color(0.8f, 0.5f, 0.55f), 0.05f, texture: "T_Peluche"),
        };

        // ---------- Emisivos ----------
        public static Material EyesRed => Get("M_Juguete_Ojos", Color.red, 0.9f, emission: new Color(4f, 0.1f, 0.1f));
        public static Material Star => Get("M_Estrella", new Color(1f, 0.85f, 0.3f), 0.5f, emission: new Color(2f, 1.6f, 0.4f));
        public static Material Bulb => Get("M_FocoNavideno", Color.white, 0.9f, emission: Color.white);
        public static Material Lamp => Get("M_TuboLampara", new Color(1f, 0.9f, 0.7f), 0.5f, emission: new Color(2f, 1.6f, 1.1f));
        public static Material Battery => Get("M_Pila", new Color(0.2f, 0.9f, 0.3f), 0.6f, emission: new Color(0.3f, 1.5f, 0.4f));
        public static Material Key => Get("M_Llave", new Color(1f, 0.78f, 0.2f), 0.8f, metallic: 1f, emission: new Color(1.2f, 0.9f, 0.2f));
        public static Material Door => Get("M_PuertaSalida", new Color(0.35f, 0.05f, 0.05f), 0.5f, metallic: 0.6f);
        public static Material Screen => Get("M_PantallaVerde", new Color(0.1f, 0.6f, 0.2f), 0.8f, emission: new Color(0.2f, 1.2f, 0.3f));
        public static Material Fire => Get("M_Fuego", new Color(1f, 0.45f, 0.1f), 0.2f, emission: new Color(4f, 1.4f, 0.2f));
        public static Material Sack => Get("MT_Costal", new Color(0.55f, 0.15f, 0.12f), 0.05f, texture: "T_Peluche");
        public static Material SnowFlake => GetParticle("M_CopoNieve", new Color(1f, 1f, 1f, 0.9f));

        /// <summary>Material URP Lit: lo carga si existe o lo crea.</summary>
        public static Material Get(string name, Color color, float smoothness, float metallic = 0f,
                                   Color? emission = null, string texture = null)
        {
            if (Cache.TryGetValue(name, out Material cached) && cached != null) return cached;

            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_Color", color);
                mat.SetFloat("_Smoothness", smoothness);
                mat.SetFloat("_Metallic", metallic);

                if (texture != null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesFolder + texture + ".png");
                    if (tex != null)
                    {
                        mat.SetTexture("_BaseMap", tex);
                        mat.SetTexture("_MainTex", tex);
                    }
                    else
                    {
                        Debug.LogWarning("[MaterialLibrary] No se encontró la textura " + texture + ".png");
                    }
                }

                if (emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission.Value);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                AssetDatabase.CreateAsset(mat, path);
            }

            Cache[name] = mat;
            return mat;
        }

        /// <summary>Material para partículas (shader Particles/Unlit de URP).</summary>
        public static Material GetParticle(string name, Color color)
        {
            if (Cache.TryGetValue(name, out Material cached) && cached != null) return cached;

            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            Cache[name] = mat;
            return mat;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
