using HauntedFactory.Core;
using HauntedFactory.Enemy;
using UnityEngine;

namespace HauntedFactory.Audio
{
    /// <summary>
    /// Sonido 3D del juguete poseído: el tic-tac de su mecanismo de cuerda
    /// (se acelera al perseguir) y un chillido de alerta cuando te detecta.
    /// Sirve como pista: si oyes el tic-tac, hay un juguete cerca.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAudio : MonoBehaviour
    {
        [SerializeField] private EnemyAI enemy;
        [SerializeField] private AudioClip windUpLoop;
        [SerializeField] private AudioClip alertClip;
        [SerializeField] private float patrolPitch = 0.9f;
        [SerializeField] private float chasePitch = 1.5f;

        private AudioSource loopSource;

        private void Awake()
        {
            loopSource = GetComponent<AudioSource>();
            if (enemy == null) enemy = GetComponentInParent<EnemyAI>();
        }

        private void OnEnable()
        {
            if (enemy != null)
            {
                enemy.StateChanged += OnStateChanged;
                enemy.FrozenChanged += OnFrozenChanged;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.StateChanged -= OnStateChanged;
                enemy.FrozenChanged -= OnFrozenChanged;
            }
        }

        /// <summary>Iluminado por la linterna, el mecanismo se detiene en seco.</summary>
        private void OnFrozenChanged(bool frozen)
        {
            if (frozen) loopSource.Pause();
            else loopSource.UnPause();
        }

        private void Start()
        {
            if (windUpLoop == null) return;
            loopSource.clip = windUpLoop;
            loopSource.loop = true;
            loopSource.pitch = patrolPitch;
            loopSource.Play();
        }

        private void Update()
        {
            // Al terminar la partida, silencio
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        private void OnStateChanged(EnemyState state)
        {
            loopSource.pitch = state == EnemyState.Chase ? chasePitch : patrolPitch;
            if (state == EnemyState.Chase && alertClip != null)
            {
                loopSource.PlayOneShot(alertClip, 1f);
            }
        }
    }
}
