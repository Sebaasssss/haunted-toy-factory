using HauntedFactory.Core;
using UnityEngine;

namespace HauntedFactory.Items
{
    /// <summary>
    /// Llave recolectable. Al tocarla se suma al GameManager y desaparece.
    /// Requiere un Collider marcado como Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KeyPickup : MonoBehaviour
    {
        private bool collected;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player")) return;

            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[KeyPickup] No hay GameManager en la escena.");
                return;
            }

            collected = true;
            gm.AddKey();
            Destroy(gameObject);
        }
    }
}
