using UnityEngine;

namespace HauntedFactory.Lighting
{
    /// <summary>
    /// Juguete decorativo inmóvil que gira lentamente la cabeza para seguir
    /// al jugador cuando está cerca. Solo ambientación: no ataca.
    /// </summary>
    public class WatchingToy : MonoBehaviour
    {
        [Tooltip("Parte que gira (normalmente la cabeza o todo el modelo).")]
        [SerializeField] private Transform head;
        [SerializeField] private float watchDistance = 9f;
        [SerializeField] private float turnSpeed = 40f;

        private Transform player;
        private Quaternion restRotation;

        private void Start()
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
            if (head == null) head = transform;
            restRotation = head.rotation;
        }

        private void Update()
        {
            if (player == null) return;

            Quaternion target = restRotation;
            Vector3 toPlayer = player.position - head.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < watchDistance * watchDistance && toPlayer.sqrMagnitude > 0.01f)
            {
                target = Quaternion.LookRotation(toPlayer);
            }
            head.rotation = Quaternion.RotateTowards(head.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
