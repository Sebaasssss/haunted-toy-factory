using UnityEngine;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Movimiento lento de la cámara del menú (como si alguien observara
    /// la escena) para que el fondo 3D no se vea estático.
    /// </summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private float positionAmount = 0.15f;
        [SerializeField] private float rotationAmount = 1.5f;
        [SerializeField] private float speed = 0.25f;

        private Vector3 startPos;
        private Quaternion startRot;

        private void Start()
        {
            startPos = transform.position;
            startRot = transform.rotation;
        }

        private void Update()
        {
            float t = Time.unscaledTime * speed;
            transform.position = startPos + new Vector3(Mathf.Sin(t) * positionAmount, Mathf.Sin(t * 1.3f) * positionAmount * 0.5f, 0f);
            transform.rotation = startRot * Quaternion.Euler(Mathf.Sin(t * 0.7f) * rotationAmount, Mathf.Sin(t) * rotationAmount, 0f);
        }
    }
}
