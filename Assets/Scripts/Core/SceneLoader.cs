using UnityEngine;
using UnityEngine.SceneManagement;

namespace HauntedFactory.Core
{
    /// <summary>
    /// Punto único para cambiar de escena. Restaura el tiempo y el audio
    /// por si se sale desde una pausa o una pantalla final.
    /// Los nombres deben coincidir con las escenas de Build Settings.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Factory";

        public static void LoadMainMenu() => Load(MainMenuScene);

        public static void LoadGame() => Load(GameScene);

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneLoader] La escena '" + sceneName + "' no está en Build Settings. " +
                               "Usa Tools > Haunted Factory > Construir todo.");
                return;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
}
