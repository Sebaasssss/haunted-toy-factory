using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.Enemy
{
    /// <summary>
    /// Sentidos del juguete poseído: vista (cono + línea de visión) y oído.
    /// - Con la linterna encendida te ve desde más lejos (la luz te delata).
    /// - Si corres te oye a través de las paredes; caminando solo de cerca.
    /// Solo detecta; las decisiones las toma EnemyAI.
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        [Header("Vista")]
        [Tooltip("Distancia de visión si el jugador tiene la linterna encendida.")]
        [SerializeField] private float viewDistanceLit = 14f;
        [Tooltip("Distancia de visión si el jugador va a oscuras.")]
        [SerializeField] private float viewDistanceDark = 7f;
        [Tooltip("Ángulo total del cono de visión, en grados.")]
        [SerializeField] private float viewAngle = 110f;
        [SerializeField] private float eyeHeight = 1.35f;
        [Tooltip("Capas que bloquean la vista (paredes, muebles...).")]
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Oído")]
        [SerializeField] private float hearRunningDistance = 9f;
        [SerializeField] private float hearWalkingDistance = 3f;
        [Tooltip("A esta distancia te detecta siempre, aunque estés quieto.")]
        [SerializeField] private float closeSenseDistance = 1.8f;

        private Transform player;
        private Transform playerEye;
        private PlayerMovement playerMovement;
        private Flashlight playerFlashlight;

        public Transform Player => player;

        /// <summary>Última posición donde se detectó al jugador.</summary>
        public Vector3 LastKnownPosition { get; private set; }

        private void Start()
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go == null)
            {
                Debug.LogWarning("[EnemyPerception] No se encontró un objeto con tag Player.");
                return;
            }

            player = go.transform;
            playerMovement = go.GetComponent<PlayerMovement>();
            playerFlashlight = go.GetComponentInChildren<Flashlight>();
            Camera cam = go.GetComponentInChildren<Camera>();
            playerEye = cam != null ? cam.transform : player;
        }

        /// <summary>True si este frame ve u oye al jugador. Actualiza LastKnownPosition.</summary>
        public bool CanDetectPlayer()
        {
            if (player == null) return false;

            bool detected = CanSeePlayer() || CanHearPlayer();
            if (detected) LastKnownPosition = player.position;
            return detected;
        }

        private float CurrentViewDistance =>
            playerFlashlight != null && playerFlashlight.IsOn ? viewDistanceLit : viewDistanceDark;

        private bool CanSeePlayer()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = playerEye.position - eye;
            float distance = toPlayer.magnitude;

            if (distance > CurrentViewDistance) return false;
            if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f) return false;

            // Línea de visión: lo primero que toque el rayo debe ser el jugador
            if (Physics.Raycast(eye, toPlayer / distance, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            return true;
        }

        private bool CanHearPlayer()
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= closeSenseDistance) return true;
            if (playerMovement == null || !playerMovement.IsMoving) return false;

            float hearing = playerMovement.IsRunning ? hearRunningDistance : hearWalkingDistance;
            return distance <= hearing;
        }

        /// <summary>Dibuja los rangos en la vista de escena al seleccionar al enemigo.</summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(eye, eye + left * viewDistanceLit);
            Gizmos.DrawLine(eye, eye + right * viewDistanceLit);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, hearRunningDistance);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, hearWalkingDistance);
        }
    }
}
