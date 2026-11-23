using UnityEngine;
using UnityEngine.AI;

namespace HauntedFactory.Enemy
{
    /// <summary>
    /// Animación procedural del juguete (sin Animator ni clips):
    /// - Camina balanceándose como juguete de cuerda.
    /// - Al perseguir, estira los brazos y castañetea la mandíbula.
    /// - Congelado por la linterna, se queda tieso en la pose que tenía.
    /// </summary>
    public class ToyAnimator : MonoBehaviour
    {
        [Header("Partes (las asigna el constructor)")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform jaw;

        [Header("Ajustes")]
        [SerializeField] private float stepSpeed = 6f;
        [SerializeField] private float waddleAngle = 9f;
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float armSwing = 25f;
        [SerializeField] private float legSwing = 22f;

        private NavMeshAgent agent;
        private EnemyAI ai;
        private Vector3 bodyStartPos;
        private float phase;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<EnemyAI>();
            if (body != null) bodyStartPos = body.localPosition;
        }

        private void Update()
        {
            // Congelado: no se toca nada, queda tieso
            if (ai != null && ai.IsFrozen) return;

            float speed = agent != null ? agent.velocity.magnitude : 0f;
            float move01 = Mathf.Clamp01(speed / 2f);
            bool chasing = ai != null && ai.State == EnemyState.Chase;

            phase += Time.deltaTime * stepSpeed * Mathf.Max(0.3f, speed * 0.6f);
            float s = Mathf.Sin(phase);

            if (body != null)
            {
                body.localRotation = Quaternion.Euler(0f, 0f, s * waddleAngle * move01);
                body.localPosition = bodyStartPos + Vector3.up * Mathf.Abs(s) * bobHeight * move01;
            }

            // Brazos: estirados hacia ti al perseguir, balanceo al patrullar
            float armBase = chasing ? -80f : 0f;
            float armAmount = chasing ? 8f : armSwing * move01;
            SetPitch(leftArm, armBase + s * armAmount);
            SetPitch(rightArm, armBase - s * armAmount);

            SetPitch(leftLeg, -s * legSwing * move01);
            SetPitch(rightLeg, s * legSwing * move01);

            // Mandíbula: castañetea al perseguir
            float jawOpen = chasing ? Mathf.Abs(Mathf.Sin(Time.time * 22f)) * 28f : Mathf.Abs(Mathf.Sin(Time.time * 1.5f)) * 4f;
            SetPitch(jaw, jawOpen);
        }

        private static void SetPitch(Transform t, float angle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
