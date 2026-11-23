using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.World
{
    /// <summary>
    /// Escondite (armario o regalo gigante). Va en una zona trigger delante del mueble:
    /// si el jugador está ahí, puede presionar E para meterse.
    /// Dentro, la cámara queda en viewPoint (mirando por la rendija).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HidingSpot : MonoBehaviour
    {
        [SerializeField] private Transform viewPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private AudioClip doorSound;
        [SerializeField] private string displayName = "armario";

        public Transform ViewPoint => viewPoint;
        public Transform ExitPoint => exitPoint;
        public string DisplayName => displayName;
        public bool Occupied { get; set; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerHiding hiding = other.GetComponent<PlayerHiding>();
            if (hiding != null) hiding.SetNearby(this);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerHiding hiding = other.GetComponent<PlayerHiding>();
            if (hiding != null) hiding.ClearNearby(this);
        }

        public void PlayDoorSound()
        {
            if (doorSound != null) AudioSource.PlayClipAtPoint(doorSound, transform.position, 0.8f);
        }
    }
}
