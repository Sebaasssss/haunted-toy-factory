using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HauntedFactory.EditorTools
{
    /// <summary>
    /// Utilidades compartidas por los constructores de escenas:
    /// confirmación antes de sobrescribir y configuración de Build Settings.
    /// </summary>
    internal static class SceneBuildUtility
    {
        public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        public const string FactoryPath = "Assets/Scenes/Factory.unity";

        /// <summary>Pregunta si se puede sobrescribir una escena que ya existe.</summary>
        public static bool ConfirmOverwrite(string scenePath)
        {
            if (!File.Exists(scenePath)) return true;
            return EditorUtility.DisplayDialog(
                "Reconstruir escena",
                "Ya existe " + scenePath + ".\nSe va a regenerar y se perderán los cambios hechos a mano en esa escena.\n\n¿Continuar?",
                "Sobrescribir", "Cancelar");
        }

        [MenuItem("Tools/Haunted Factory/Construir todo (menú + fábrica)", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!ConfirmOverwrite(FactoryPath) || !ConfirmOverwrite(MainMenuPath)) return;

            FactorySceneBuilder.Build();
            MainMenuSceneBuilder.Build();

            // Dejar abierto el menú para que Play empiece desde ahí
            EditorSceneManager.OpenScene(MainMenuPath);
            Debug.Log("[Haunted Factory] Todo construido. Dale Play para empezar desde el menú principal.");
        }

        /// <summary>
        /// Build Settings: MainMenu (0) y Factory (1) activadas.
        /// Las demás escenas que ya estaban se conservan, pero desactivadas.
        /// </summary>
        [MenuItem("Tools/Haunted Factory/Configurar Build Settings", priority = 20)]
        public static void ConfigureBuildSettings()
        {
            var result = new List<EditorBuildSettingsScene>();
            foreach (string path in new[] { MainMenuPath, FactoryPath })
            {
                if (File.Exists(path)) result.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == MainMenuPath || existing.path == FactoryPath) continue;
                result.Add(new EditorBuildSettingsScene(existing.path, false));
            }

            EditorBuildSettings.scenes = result.ToArray();
            Debug.Log("[Haunted Factory] Build Settings configurado (" + result.Count + " escenas en la lista).");
        }
    }
}
