using System.Collections.Generic;
using HauntedFactory.Lighting;
using HauntedFactory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Ambiente compartido por el menú y la fábrica: niebla, luces navideñas,
    /// lámparas dañadas, letreros de neón, post-procesado y nieve.
    /// </summary>
    internal static class EnvironmentFactory
    {
        /// <summary>Oscuridad casi total: solo se ve lo que ilumina algo.</summary>
        public static void ApplyDarkEnvironment(Color fogColor, float fogDensity, Color ambient)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }

        public static Light PointLight(Transform parent, string name, Vector3 worldPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // ------------------------------------------------------------------
        // Guirnaldas navideñas
        // ------------------------------------------------------------------

        /// <summary>
        /// Guirnalda recta entre dos puntos (en coordenadas de mundo) con caída entre focos.
        /// Las luces reales son débiles y de poco alcance: los focos brillan
        /// pero casi no iluminan el cuarto.
        /// </summary>
        public static void LightString(Transform parent, string name, Vector3 from, Vector3 to, int bulbCount, int lightCount,
                                       ChristmasLightString.Pattern pattern, bool redOnly = false,
                                       float lightIntensity = 0.5f, float lightRange = 3.5f)
        {
            Transform root = PropFactory.Child(parent, name);
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
                lights[j] = PointLight(root, "Luz_" + j, p, Color.white, lightIntensity, lightRange);
            }

            AddLightStringComponent(root.gameObject, bulbs, lights, pattern, redOnly);
        }

        /// <summary>Anillos de focos alrededor de las capas del árbol de Navidad.</summary>
        public static void TreeLights(Transform parent, string name, Vector3 treeBase)
        {
            Transform root = PropFactory.Child(parent, name);
            var bulbs = new List<Renderer>();
            int[] counts = { 12, 10, 8, 6 };
            for (int ring = 0; ring < counts.Length; ring++)
            {
                for (int i = 0; i < counts[ring]; i++)
                {
                    float angle = i / (float)counts[ring] * Mathf.PI * 2f + ring * 0.4f;
                    float r = PropFactory.TreeLightRadii[ring];
                    Vector3 p = treeBase + new Vector3(Mathf.Cos(angle) * r, PropFactory.TreeLightHeights[ring], Mathf.Sin(angle) * r);
                    bulbs.Add(CreateBulb(root, "Foco_" + ring + "_" + i, p));
                }
            }
            Light glow = PointLight(root, "Luz_Arbol", treeBase + new Vector3(0f, 1.6f, -1.6f), Color.white, 0.9f, 5f);
            AddLightStringComponent(root.gameObject, bulbs.ToArray(), new[] { glow }, ChristmasLightString.Pattern.Chase, false);
        }

        private static Renderer CreateBulb(Transform parent, string name, Vector3 worldPos)
        {
            GameObject bulb = PropFactory.Sphere(name, parent, Vector3.zero, new Vector3(0.09f, 0.13f, 0.09f), MaterialLibrary.Bulb, false, false);
            bulb.transform.position = worldPos;
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

        public static void SetObjectArray(SerializedProperty prop, Object[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // ------------------------------------------------------------------
        // Lámparas y letreros
        // ------------------------------------------------------------------

        /// <summary>Lámpara de techo dañada que parpadea (sin sombras, para no saturar el atlas).</summary>
        public static void BrokenLamp(Transform parent, string name, Vector3 worldPos, Color color, float intensity, float range)
        {
            Transform root = PropFactory.Child(parent, name);
            root.position = worldPos;
            PropFactory.Box("Carcasa", root, Vector3.zero, new Vector3(0.7f, 0.08f, 0.3f), MaterialLibrary.Metal, false, false);
            GameObject tube = PropFactory.Box("Tubo", root, Vector3.down * 0.06f, new Vector3(0.6f, 0.04f, 0.12f), MaterialLibrary.Lamp, false, false);

            Light light = PointLight(root, "Luz", worldPos + Vector3.down * 0.35f, color, intensity, range);
            var flicker = light.gameObject.AddComponent<FlickeringLight>();
            var so = new SerializedObject(flicker);
            so.FindProperty("lampRenderer").objectReferenceValue = tube.GetComponent<MeshRenderer>();
            so.FindProperty("lampEmission").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Letrero de neón en un Canvas en el mundo. El texto se lee desde el lado
        /// opuesto a su eje +Z: para un letrero pegado a una pared, +Z apunta a la pared.
        /// </summary>
        public static void NeonSign(Transform parent, string name, Vector3 worldPos, float yaw, string text, Color color,
                                    int fontSize, float widthMeters, bool flicker, bool doubleSided = false)
        {
            // Tablero oscuro detrás del texto (tapa la cara trasera del canvas)
            Transform board = PropFactory.Child(parent, name + "_Tablero");
            board.position = worldPos;
            board.rotation = Quaternion.Euler(0f, yaw, 0f);
            PropFactory.Box("Tablero", board, Vector3.zero, new Vector3(widthMeters + 0.2f, 0.75f, 0.03f), MaterialLibrary.Black, false, false);

            CreateNeonCanvas(parent, name, worldPos + board.rotation * Vector3.back * 0.03f, yaw, text, color, fontSize, widthMeters, flicker);
            if (doubleSided)
            {
                CreateNeonCanvas(parent, name + "_Reverso", worldPos + board.rotation * Vector3.forward * 0.03f, yaw + 180f,
                                 text, color, fontSize, widthMeters, flicker);
            }
        }

        private static void CreateNeonCanvas(Transform parent, string name, Vector3 worldPos, float yaw, string text, Color color,
                                             int fontSize, float widthMeters, bool flicker)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            // 400 unidades de canvas = 1 metro (fuente grande y escala pequeña = texto nítido).
            // fontSize está en esas unidades: 160 ≈ letras de 40 cm.
            const float unitsPerMeter = 400f;
            go.transform.localScale = Vector3.one / unitsPerMeter;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            var size = new Vector2(widthMeters * unitsPerMeter, 600f);
            rt.sizeDelta = size;

            Text label = UIFactory.CreateCenteredText("Texto", go.transform, text, fontSize, color,
                                                      Vector2.zero, size, FontStyle.Bold);
            var glow = label.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(color.r, color.g, color.b, 0.45f);
            glow.effectDistance = new Vector2(6f, 6f);
            if (flicker) label.gameObject.AddComponent<TextFlicker>();

            // Un poco de luz del color del neón sobre la pared
            Vector3 front = go.transform.rotation * Vector3.back;
            PointLight(parent, name + "_Luz", worldPos + front * 0.5f, color, 0.5f, 3f);
        }

        // ------------------------------------------------------------------
        // Post-procesado y nieve
        // ------------------------------------------------------------------

        /// <summary>
        /// Crea (o reemplaza) un perfil de post-procesado y un Volume global:
        /// bloom, viñeta fuerte, grano, colores fríos y algo de aberración cromática.
        /// </summary>
        public static Volume CreatePostFxVolume(string assetPath, float vignetteIntensity)
        {
            MaterialLibrary.EnsureFolder(System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, assetPath);

            Bloom bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(vignetteIntensity);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = AddOverride<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);

            ColorAdjustments color = AddOverride<ColorAdjustments>(profile);
            color.contrast.Override(15f);
            color.saturation.Override(-15f);
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
            return volume;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        /// <summary>Nieve cayendo dentro de una caja (área en X y Z, en metros).</summary>
        public static ParticleSystem Snowfall(string name, Vector3 worldPos, Vector2 area, float rate, float speed)
        {
            var go = new GameObject(name);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // emitir hacia abajo

            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 10f;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = 0.03f;
            main.maxParticles = 2000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, area.y, 0.1f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialLibrary.SnowFlake;
            return ps;
        }
    }
}
