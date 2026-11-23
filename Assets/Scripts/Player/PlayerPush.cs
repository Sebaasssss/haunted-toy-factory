using HauntedFactory.Items;
using UnityEngine;

namespace HauntedFactory.Player
{
    /// <summary>
    /// Permite que el jugador empuje objetos con física (pelotas) al chocar con ellos.
    /// El CharacterController no empuja Rigidbodies por sí solo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPush : MonoBehaviour
    {
        [SerializeField] private float extraSpeed = 1.5f;
        [SerializeField] private float maxSpeed = 8f;

        private CharacterController controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return; // pisándolo, no empujando

            NoisyBall ball = body.GetComponent<NoisyBall>();
            if (ball != null) ball.MarkTouchedByPlayer();

            var dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
            float desired = Mathf.Min(maxSpeed, controller.velocity.magnitude * 1.3f + extraSpeed);
            float current = Vector3.Dot(body.linearVelocity, dir);
            if (current < desired)
            {
                body.AddForce(dir * (desired - current), ForceMode.VelocityChange);
            }
        }
    }
}
