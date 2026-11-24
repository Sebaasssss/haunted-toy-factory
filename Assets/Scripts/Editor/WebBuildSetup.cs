using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Ajusta la versión Web (la que se sube a GitHub Pages) sin tocar la de PC.
    /// La web usa el nivel de calidad "Mobile" (Mobile_RPAsset + Mobile_Renderer).
    /// </summary>
    public static class WebBuildSetup
    {
        private const string MobileAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";

        [MenuItem("Tools/Haunted Factory/Optimizar versión Web", priority = 30)]
        public static void Apply()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MobileRendererPath);
            if (asset == null || renderer == null)
            {
                Debug.LogError("[Haunted Factory] No se encontraron " + MobileAssetPath + " o " + MobileRendererPath);
                return;
            }

            // Imagen nítida: dibujar al 100 % de la resolución (antes 80 %)
            asset.renderScale = 1f;
            EditorUtility.SetDirty(asset);

            // Forward+: todas las guirnaldas y la linterna se iluminan bien (Forward solo admite 4 luces por objeto).
            // Este asset no tiene sombras de luces adicionales, que es lo que congela Forward+ en WebGL.
            renderer.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(renderer);

            // Recuadro del juego más grande en la página y sin compresión (GitHub Pages no la sirve bien)
            PlayerSettings.defaultWebScreenWidth = 1600;
            PlayerSettings.defaultWebScreenHeight = 900;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            AssetDatabase.SaveAssets();
            Debug.Log("[Haunted Factory] Versión Web optimizada: resolución 100 %, Forward+, recuadro 1600x900, sin compresión. " +
                      "Vuelve a compilar la build Web en la carpeta docs.");
        }
    }
}
