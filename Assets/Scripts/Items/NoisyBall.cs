using HauntedFactory.Core;
using UnityEngine;

namespace HauntedFactory.Items
{
    /// <summary>
    /// Pelota con física: rueda al empujarla y al chocar rebota con sonido.
    /// Cada golpe fuerte hace ruido que los juguetes pueden oír.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NoisyBall : MonoBehaviour
    {
        [SerializeField] private AudioClip bounceClip;
        [Tooltip("Velocidad mínima del golpe para que suene.")]
        [SerializeField] private float minImpactSpeed = 1.2f;
        [SerializeField] private float noiseRadiusMin = 6f;
        [SerializeField] private float noiseRadiusMax = 15f;
        [SerializeField] private float cooldown = 0.2f;
        [Tooltip("Segundos después de que el jugador la toca en que sus golpes atraen juguetes.")]
        [SerializeField] private float playerBlameTime = 6f;

        private AudioSource source;
        private float lastSoundTime = -10f;
        private float spawnTime;
        private float lastPlayerTouch = -100f;

        /// <summary>Lo llama PlayerPush al empujarla.</summary>
        public void MarkTouchedByPlayer() => lastPlayerTouch = Time.time;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            spawnTime = Time.time;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Ignorar el primer segundo (al acomodarse en el suelo)
            if (Time.time - spawnTime < 1f) return;

            float speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed || Time.time - lastSoundTime < cooldown) return;
            lastSoundTime = Time.time;

            float strength = Mathf.InverseLerp(minImpactSpeed, 8f, speed);
            if (source != null && bounceClip != null)
            {
                source.pitch = Random.Range(0.85f, 1.2f);
                source.PlayOneShot(bounceClip, Mathf.Lerp(0.35f, 1f, strength));
            }
            // Si la movió un juguete, suena pero no atrae a los demás
            if (Time.time - lastPlayerTouch < playerBlameTime)
            {
                NoiseSystem.Emit(transform.position, Mathf.Lerp(noiseRadiusMin, noiseRadiusMax, strength));
            }
        }
    }
}
