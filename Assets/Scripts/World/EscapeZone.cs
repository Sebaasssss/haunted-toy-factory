using HauntedFactory.Core;
using UnityEngine;

namespace HauntedFactory.World
{
    /// <summary>
    /// Zona afuera de la fábrica: si el jugador entra, gana la partida.
    /// Requiere un Collider marcado como Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EscapeZone : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameManager.Instance != null) GameManager.Instance.Win();
        }
    }
}
