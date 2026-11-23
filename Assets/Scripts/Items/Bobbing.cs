using UnityEngine;

namespace HauntedFactory.Items
{
    /// <summary>
    /// Animación sencilla para objetos recolectables: gira y flota arriba/abajo
    /// para que el jugador los distinga en la oscuridad.
    /// </summary>
    public class Bobbing : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 startPosition;
        private float phase;

        private void Start()
        {
            startPosition = transform.localPosition;
            // Desfase aleatorio para que no todos floten sincronizados
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
            float offset = Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
            transform.localPosition = startPosition + Vector3.up * offset;
        }
    }
}
