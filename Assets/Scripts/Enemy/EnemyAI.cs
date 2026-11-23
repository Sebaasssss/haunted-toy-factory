using System;
using HauntedFactory.Core;
using UnityEngine;
using UnityEngine.AI;

namespace HauntedFactory.Enemy
{
    /// <summary>Estados del juguete poseído.</summary>
    public enum EnemyState { Patrol, Chase, Search }

    /// <summary>
    /// IA del juguete poseído con NavMeshAgent:
    /// - Patrulla: recorre sus waypoints en bucle, con una pausa en cada uno.
    /// - Persecución: corre hacia el jugador mientras lo detecta.
    /// - Búsqueda: si lo pierde, va a la última posición conocida, mira alrededor
    ///   y luego vuelve a patrullar.
    /// - Congelado: mientras la linterna lo ilumina no se mueve ni puede atrapar
    ///   (como los juguetes que se quedan quietos cuando los miran).
    /// Si llega a tocar al jugador, avisa al GameManager (game over).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Ruta de patrulla")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointWaitTime = 1.5f;

        [Header("Velocidades")]
        [SerializeField] private float patrolSpeed = 2f;
        [Tooltip("Más rápido que caminar (3.5) pero más lento que correr (6): si corres, escapas.")]
        [SerializeField] private float chaseSpeed = 4.6f;

        [Header("Persecución y búsqueda")]
        [Tooltip("Segundos sin detectar al jugador antes de pasar a buscarlo.")]
        [SerializeField] private float loseSightTime = 3f;
        [SerializeField] private float searchDuration = 4f;
        [SerializeField] private float repathInterval = 0.2f;

        [Header("Linterna")]
        [Tooltip("Si está activo, el juguete se congela mientras lo ilumina la linterna.")]
        [SerializeField] private bool freezeWhenLit = true;

        [Header("Atrapar")]
        [SerializeField] private float catchDistance = 1.2f;

        private NavMeshAgent agent;
        private EnemyPerception perception;
        private EnemyState state;
        private int waypointIndex;
        private float waitTimer;
        private float lastDetectedTime;
        private float searchTimer;
        private float repathTimer;

        public EnemyState State => state;

        /// <summary>True mientras la linterna lo tiene congelado.</summary>
        public bool IsFrozen { get; private set; }

        /// <summary>Se dispara al congelarse (true) o liberarse (false).</summary>
        public event Action<bool> FrozenChanged;

        /// <summary>Se dispara al cambiar de estado (útil para música de persecución).</summary>
        public event Action<EnemyState> StateChanged;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<EnemyPerception>();
        }

        private void Start()
        {
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("[EnemyAI] " + name + " no está sobre el NavMesh. ¿Se horneó el NavMesh?");
            }
            SetState(EnemyState.Patrol);
            GoToWaypoint(0);
        }

        private void Update()
        {
            if (!agent.isOnNavMesh) return;

            // Si la partida terminó, quedarse quieto
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing)
            {
                agent.isStopped = true;
                return;
            }

            bool detected = perception.CanDetectPlayer();
            if (detected)
            {
                lastDetectedTime = Time.time;
                if (state != EnemyState.Chase) SetState(EnemyState.Chase);
            }

            // Congelado por la linterna: quieto y sin poder atrapar
            bool lit = freezeWhenLit && perception.IsLitByFlashlight();
            if (lit != IsFrozen)
            {
                IsFrozen = lit;
                FrozenChanged?.Invoke(lit);
            }
            if (IsFrozen)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                return;
            }
            if (agent.isStopped) agent.isStopped = false;

            switch (state)
            {
                case EnemyState.Patrol: UpdatePatrol(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Search: UpdateSearch(); break;
            }

            CheckCatch();
        }

        // ---------------- Estados ----------------

        private void UpdatePatrol()
        {
            if (waypoints == null || waypoints.Length == 0 || !HasArrived()) return;

            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                GoToWaypoint((waypointIndex + 1) % waypoints.Length);
            }
        }

        private void UpdateChase()
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                agent.SetDestination(perception.LastKnownPosition);
            }

            if (Time.time - lastDetectedTime > loseSightTime)
            {
                SetState(EnemyState.Search);
            }
        }

        private void UpdateSearch()
        {
            if (!HasArrived()) return;

            // Mirar alrededor en la última posición conocida
            searchTimer += Time.deltaTime;
            transform.Rotate(0f, 120f * Time.deltaTime, 0f);

            if (searchTimer >= searchDuration)
            {
                SetState(EnemyState.Patrol);
                GoToWaypoint(NearestWaypointIndex());
            }
        }

        private void SetState(EnemyState newState)
        {
            state = newState;
            agent.isStopped = false;

            switch (newState)
            {
                case EnemyState.Patrol:
                    agent.speed = patrolSpeed;
                    break;
                case EnemyState.Chase:
                    agent.speed = chaseSpeed;
                    repathTimer = 0f;
                    break;
                case EnemyState.Search:
                    agent.speed = patrolSpeed * 1.3f;
                    searchTimer = 0f;
                    agent.SetDestination(perception.LastKnownPosition);
                    break;
            }

            StateChanged?.Invoke(newState);
        }

        // ---------------- Utilidades ----------------

        private void GoToWaypoint(int index)
        {
            if (waypoints == null || waypoints.Length == 0) return;
            waypointIndex = index;
            waitTimer = 0f;
            if (waypoints[index] != null) agent.SetDestination(waypoints[index].position);
        }

        private int NearestWaypointIndex()
        {
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = (waypoints[i].position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        private bool HasArrived()
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        }

        /// <summary>Si el jugador está al alcance, termina la partida.</summary>
        private void CheckCatch()
        {
            Transform player = perception.Player;
            if (player == null) return;

            Vector3 a = transform.position;
            Vector3 b = player.position;
            if (Mathf.Abs(a.y - b.y) > 2f) return;
            a.y = 0f;
            b.y = 0f;

            if ((a - b).sqrMagnitude <= catchDistance * catchDistance && GameManager.Instance != null)
            {
                GameManager.Instance.Lose();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform a = waypoints[i];
                Transform b = waypoints[(i + 1) % waypoints.Length];
                if (a == null || b == null) continue;
                Gizmos.DrawSphere(a.position, 0.2f);
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
}
