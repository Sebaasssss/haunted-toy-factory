using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HauntedFactory.Audio
{
    /// <summary>
    /// En el navegador el audio está bloqueado hasta que el jugador hace clic o
    /// presiona una tecla. Este script muestra un aviso y, en esa primera interacción,
    /// vuelve a arrancar la música (si empezó antes, se quedaba en silencio).
    /// Fuera de la versión web no hace nada y oculta el aviso.
    /// </summary>
    public class WebAudioUnlock : MonoBehaviour
    {
        [SerializeField] private AudioSource[] sources;
        [Tooltip("Texto \"Haz clic para activar el sonido\" (solo se muestra en la web).")]
        [SerializeField] private GameObject hint;

        private bool unlocked;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (hint != null) hint.SetActive(true);
#else
            unlocked = true;
            if (hint != null) hint.SetActive(false);
#endif
        }

        private void Update()
        {
            if (unlocked) return;

            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;
            bool interacted = (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                              || (kb != null && kb.anyKey.wasPressedThisFrame);
            if (!interacted) return;

            unlocked = true;
            if (hint != null) hint.SetActive(false);
            AudioListener.pause = false;
            if (sources == null) return;
            foreach (AudioSource s in sources)
            {
                if (s == null || s.clip == null) continue;
                s.Stop();
                s.Play();
            }
        }
    }
}
