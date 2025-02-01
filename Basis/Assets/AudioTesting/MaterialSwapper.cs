using JetBrains.Annotations;
using UnityEngine;

namespace AudioTesting
{
    [RequireComponent(typeof(Renderer))]
    public class MaterialSwapper : MonoBehaviour
    {
        [SerializeField] private Material matA;
        [SerializeField] private Material matB;

        private Renderer _renderer;
        private bool _swap;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _renderer.material = matA;
        }

        [UsedImplicitly]
        public void SwapMaterial()
        {
            _swap = !_swap;
            _renderer.material = _swap ? matB : matA;
        }
    }
}
