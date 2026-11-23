using UnityEngine;

namespace HauntedFactory.Lighting
{
    /// <summary>
    /// Gira el objeto sobre su eje Y a velocidad constante.
    /// Se usa para el trenecito que da vueltas al árbol de Navidad
    /// (los vagones son hijos colocados sobre el círculo).
    /// </summary>
    public class CircularMover : MonoBehaviour
    {
        [Tooltip("Grados por segundo (negativo = sentido contrario).")]
        [SerializeField] private float degreesPerSecond = 25f;
        [Tooltip("Pausas aleatorias, como si al tren se le acabara la cuerda.")]
        [SerializeField] private bool randomStops = true;

        private float stopTimer;
        private float nextStopCheck = 3f;

        private void Update()
        {
            if (randomStops)
            {
                if (stopTimer > 0f)
                {
                    stopTimer -= Time.deltaTime;
                    return;
                }
                nextStopCheck -= Time.deltaTime;
                if (nextStopCheck <= 0f)
                {
                    nextStopCheck = Random.Range(4f, 10f);
                    if (Random.value < 0.35f) stopTimer = Random.Range(1f, 3f);
                }
            }
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
