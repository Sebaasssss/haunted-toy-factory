using HauntedFactory.Core;
using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.Items
{
    /// <summary>
    /// Pila recolectable: al tocarla el jugador recarga la linterna.
    /// Si la batería ya está llena, la pila se queda en su lugar.
    /// Requiere un Collider marcado como Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BatteryPickup : MonoBehaviour
    {
        [Tooltip("Cuánta batería recarga (de 100).")]
        [SerializeField] private float amount = 35f;
        [SerializeField] private AudioClip pickupSound;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            Flashlight flashlight = other.GetComponentInChildren<Flashlight>();
            if (flashlight == null || !flashlight.AddBattery(amount)) return;

            if (GameManager.Instance != null) GameManager.Instance.NotifyBatteryCollected();

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            Destroy(gameObject);
        }
    }
}
