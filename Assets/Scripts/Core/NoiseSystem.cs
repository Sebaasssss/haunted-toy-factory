using System;
using UnityEngine;

namespace HauntedFactory.Core
{
    /// <summary>
    /// Canal global de ruidos: pelotas que rebotan, aterrizajes de salto...
    /// Los juguetes escuchan y van a investigar si el ruido les llega.
    /// </summary>
    public static class NoiseSystem
    {
        /// <summary>(posición del ruido, radio en metros hasta donde se oye)</summary>
        public static event Action<Vector3, float> NoiseMade;

        public static void Emit(Vector3 position, float radius)
        {
            NoiseMade?.Invoke(position, radius);
        }

        // El proyecto tiene desactivado el "Domain Reload" al entrar a Play:
        // se limpia el evento para no arrastrar suscriptores de la partida anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            NoiseMade = null;
        }
    }
}
