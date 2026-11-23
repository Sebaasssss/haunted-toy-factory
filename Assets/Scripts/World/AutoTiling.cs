using UnityEngine;

namespace HauntedFactory.World
{
    /// <summary>
    /// Repite la textura según el tamaño del objeto para que no se estire:
    /// un piso de 20 m y uno de 4 m muestran casillas del mismo tamaño.
    /// Usa un MaterialPropertyBlock, así todos comparten el mismo material.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class AutoTiling : MonoBehaviour
    {
        public enum Mode { Floor, Wall }

        [SerializeField] private Mode mode = Mode.Floor;
        [Tooltip("Metros que ocupa una repetición de la textura.")]
        [SerializeField] private float tileSize = 2f;

        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null || tileSize <= 0f) return;

            Vector3 s = transform.lossyScale;
            Vector2 tiling = mode == Mode.Floor
                ? new Vector2(s.x / tileSize, s.z / tileSize)
                : new Vector2(Mathf.Max(s.x, s.z) / tileSize, s.y / tileSize);

            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetVector(BaseMapStId, new Vector4(tiling.x, tiling.y, 0f, 0f));
            r.SetPropertyBlock(block);
        }
    }
}
