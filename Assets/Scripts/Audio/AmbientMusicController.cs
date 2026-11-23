using HauntedFactory.Core;
using HauntedFactory.Enemy;
using UnityEngine;

namespace HauntedFactory.Audio
{
    /// <summary>
    /// Ambiente sonoro global (2D):
    /// - zumbido de la fábrica en bucle,
    /// - cajita musical navideña desafinada,
    /// - música de persecución que entra cuando algún juguete te persigue.
    /// Todo se desvanece al ganar o perder.
    /// </summary>
    public class AmbientMusicController : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip ambientLoop;
        [SerializeField] private AudioClip musicBoxLoop;
        [SerializeField] private AudioClip chaseLoop;

        [Header("Volúmenes")]
        [SerializeField] private float ambientVolume = 0.45f;
        [SerializeField] private float musicBoxVolume = 0.25f;
        [SerializeField] private float chaseVolume = 0.7f;
        [SerializeField] private float fadeSpeed = 1.5f;

        [Header("Enemigos que activan la música de persecución")]
        [SerializeField] private EnemyAI[] enemies;

        private AudioSource ambientSource;
        private AudioSource musicBoxSource;
        private AudioSource chaseSource;
        private int chasingCount;
        private bool gameEnded;

        private void Awake()
        {
            ambientSource = CreateSource(ambientLoop);
            musicBoxSource = CreateSource(musicBoxLoop);
            chaseSource = CreateSource(chaseLoop);
            chaseSource.volume = 0f;
        }

        private void OnEnable()
        {
            if (enemies == null) return;
            foreach (EnemyAI e in enemies)
            {
                if (e != null) e.StateChanged += OnEnemyStateChanged;
            }
        }

        private void OnDisable()
        {
            if (enemies == null) return;
            foreach (EnemyAI e in enemies)
            {
                if (e != null) e.StateChanged -= OnEnemyStateChanged;
            }
        }

        private void Start()
        {
            ambientSource.volume = ambientVolume;
            musicBoxSource.volume = musicBoxVolume;
            PlayIfAssigned(ambientSource);
            PlayIfAssigned(musicBoxSource);
            PlayIfAssigned(chaseSource);
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing) gameEnded = true;

            RecountChasers();
            bool chasing = chasingCount > 0 && !gameEnded;

            // Tiempo sin escalar: el desvanecimiento funciona aunque el juego esté congelado
            float dt = Time.unscaledDeltaTime * fadeSpeed;
            Fade(chaseSource, chasing ? chaseVolume : 0f, dt);
            Fade(musicBoxSource, gameEnded || chasing ? 0f : musicBoxVolume, dt);
            Fade(ambientSource, gameEnded ? 0f : ambientVolume, dt);
        }

        private void OnEnemyStateChanged(EnemyState state)
        {
            RecountChasers();
        }

        private void RecountChasers()
        {
            chasingCount = 0;
            if (enemies == null) return;
            foreach (EnemyAI e in enemies)
            {
                if (e != null && e.State == EnemyState.Chase) chasingCount++;
            }
        }

        private AudioSource CreateSource(AudioClip clip)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        private static void PlayIfAssigned(AudioSource s)
        {
            if (s.clip != null) s.Play();
        }

        private static void Fade(AudioSource s, float target, float step)
        {
            s.volume = Mathf.MoveTowards(s.volume, target, step);
        }
    }
}
