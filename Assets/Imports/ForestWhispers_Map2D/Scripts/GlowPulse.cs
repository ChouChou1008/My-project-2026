using UnityEngine;

namespace ForestWhispers.Map2D
{
    /// <summary>빛 번짐과 빛내림의 밝기를 천천히 숨 쉬듯 바꾼다.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GlowPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float _amount = 0.3f;
        [SerializeField] private float _period = 4f;

        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _phase;

        public void Setup(float amount, float period)
        {
            _amount = amount;
            _period = period;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            _baseColor = _renderer.color;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            Pulse();
        }

        private void CacheComponent()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Pulse()
        {
            float wave = Mathf.Sin(Time.time * Mathf.PI * 2f / _period + _phase) * 0.5f + 0.5f;
            Color color = _baseColor;
            color.a = _baseColor.a * (1f - _amount * wave);
            _renderer.color = color;
        }
    }
}
