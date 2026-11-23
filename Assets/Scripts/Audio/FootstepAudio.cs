using HauntedFactory.Player;
using UnityEngine;

namespace HauntedFactory.Audio
{
    /// <summary>Pasos del jugador: más rápidos y fuertes al correr.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private AudioClip[] steps;
        [SerializeField] private float walkInterval = 0.52f;
        [SerializeField] private float runInterval = 0.32f;
        [SerializeField] private float walkVolume = 0.35f;
        [SerializeField] private float runVolume = 0.7f;

        private AudioSource source;
        private CharacterController controller;
        private float timer;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (movement != null) controller = movement.GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (movement == null || steps == null || steps.Length == 0) return;

            bool grounded = controller == null || controller.isGrounded;
            if (!movement.IsMoving || !movement.CanMove || !grounded)
            {
                timer = 0f;
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            bool running = movement.IsRunning;
            timer = running ? runInterval : walkInterval;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(steps[Random.Range(0, steps.Length)], running ? runVolume : walkVolume);
        }
    }
}
