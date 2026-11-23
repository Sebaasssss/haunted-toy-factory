using System.Collections;
using HauntedFactory.Core;
using UnityEngine;

namespace HauntedFactory.World
{
    /// <summary>
    /// Puerta final tipo cortina metálica. Tiene un trigger delante:
    /// si el jugador llega con todas las llaves, la hoja sube y deja pasar;
    /// si no, muestra un aviso de cuántas llaves faltan.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitDoor : MonoBehaviour
    {
        [Tooltip("Parte móvil de la puerta (con su collider sólido).")]
        [SerializeField] private Transform doorPanel;
        [SerializeField] private float openHeight = 3.9f;
        [SerializeField] private float openDuration = 1.5f;
        [Tooltip("Segundos entre avisos de 'puerta cerrada' para no saturar la pantalla.")]
        [SerializeField] private float lockedMessageCooldown = 3f;

        [Header("Sonidos (opcionales)")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;

        private bool playerInside;
        private bool isOpen;
        private float lastLockedMessageTime = -100f;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        // Se usa Enter/Exit + Update en lugar de OnTriggerStay
        // porque el CharacterController no siempre lo dispara estando quieto.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) playerInside = false;
        }

        private void Update()
        {
            if (isOpen || !playerInside) return;

            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            if (gm.HasAllKeys)
            {
                Open();
            }
            else if (Time.time - lastLockedMessageTime > lockedMessageCooldown)
            {
                lastLockedMessageTime = Time.time;
                int missing = gm.RequiredKeys - gm.KeysCollected;
                gm.ShowMessage("La puerta está cerrada. Te faltan " + missing + (missing == 1 ? " llave." : " llaves."));
                PlaySound(lockedSound);
            }
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            PlaySound(openSound);
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage("¡La puerta se abrió! ¡Escapa!");
            if (doorPanel != null) StartCoroutine(SlideUp());
        }

        private IEnumerator SlideUp()
        {
            Vector3 start = doorPanel.localPosition;
            Vector3 end = start + Vector3.up * openHeight;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, openDuration);
                doorPanel.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}
