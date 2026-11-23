using HauntedFactory.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HauntedFactory.UI
{
    /// <summary>
    /// Susto aleatorio: al recoger una llave o una pila, a veces un juguete
    /// aparece de golpe frente a la cámara con un grito y un destello rojo.
    /// También da el destello al ser atrapado.
    /// </summary>
    public class JumpScare : MonoBehaviour
    {
        [Tooltip("Modelos hijos de la cámara (inactivos) que aparecen en el susto.")]
        [SerializeField] private GameObject[] scareModels;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip scareClip;

        [Header("Probabilidad")]
        [Range(0f, 1f)]
        [SerializeField] private float chance = 0.35f;
        [Tooltip("Segundos mínimos entre dos sustos.")]
        [SerializeField] private float cooldown = 25f;
        [SerializeField] private float duration = 0.75f;

        private float lastScareTime = -999f;
        private float scareTimer;
        private float flashTimer;
        private float flashDuration = 0.6f;
        private GameObject activeModel;
        private Vector3 activeBasePos;

        private void Start()
        {
            if (scareModels != null)
            {
                foreach (GameObject m in scareModels) if (m != null) m.SetActive(false);
            }
            SetOverlayAlpha(0f);

            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.KeyCollected += TryScare;
            gm.BatteryCollected += TryScare;
            gm.GameLost += OnLost;
        }

        private void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.KeyCollected -= TryScare;
            gm.BatteryCollected -= TryScare;
            gm.GameLost -= OnLost;
        }

        private void TryScare()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing) return;
            if (Time.time - lastScareTime < cooldown) return;
            if (Random.value > chance) return;
            Play();
        }

        /// <summary>Lanza el susto ya (sin probabilidad).</summary>
        public void Play()
        {
            lastScareTime = Time.time;
            scareTimer = duration;
            Flash(duration);

            if (scareModels != null && scareModels.Length > 0)
            {
                activeModel = scareModels[Random.Range(0, scareModels.Length)];
                if (activeModel != null)
                {
                    activeBasePos = activeModel.transform.localPosition;
                    activeModel.SetActive(true);
                }
            }
            if (audioSource != null && scareClip != null) audioSource.PlayOneShot(scareClip, 1f);
        }

        private void OnLost()
        {
            Flash(0.9f);
            if (audioSource != null && scareClip != null) audioSource.PlayOneShot(scareClip, 0.8f);
        }

        private void Flash(float seconds)
        {
            flashDuration = seconds;
            flashTimer = seconds;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (scareTimer > 0f)
            {
                scareTimer -= dt;
                if (activeModel != null)
                {
                    // Tiembla y se acerca un poco
                    float k = 1f - scareTimer / duration;
                    activeModel.transform.localPosition = activeBasePos + Random.insideUnitSphere * 0.025f + Vector3.back * k * 0.15f;
                }
                if (scareTimer <= 0f && activeModel != null)
                {
                    activeModel.transform.localPosition = activeBasePos;
                    activeModel.SetActive(false);
                    activeModel = null;
                }
            }

            if (flashTimer > 0f)
            {
                flashTimer = Mathf.Max(0f, flashTimer - dt);
                float a = flashTimer / flashDuration;
                SetOverlayAlpha(a * (Random.value > 0.3f ? 0.55f : 0.2f));
            }
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (flashOverlay == null) return;
            Color c = flashOverlay.color;
            c.a = alpha;
            flashOverlay.color = c;
        }
    }
}
