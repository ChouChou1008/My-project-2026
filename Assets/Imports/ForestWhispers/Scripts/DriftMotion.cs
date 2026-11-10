using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>안개는 옆으로 천천히 흐르게, 나비와 룬은 위아래로 떠다니게 한다.</summary>
    public class DriftMotion : MonoBehaviour
    {
        [Header("Drift Settings")]
        [SerializeField] private float _driftSpeed;
        [SerializeField] private float _driftRange = 6f;
        [SerializeField] private float _bobHeight;
        [SerializeField] private float _bobPeriod = 3.5f;

        private Transform _transform;
        private Vector3 _origin;
        private float _phase;

        public void Setup(float driftSpeed, float bobHeight)
        {
            _driftSpeed = driftSpeed;
            _bobHeight = bobHeight;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            _origin = _transform.position;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            Drift();
        }

        private void CacheComponent()
        {
            _transform = transform;
        }

        private void Drift()
        {
            float time = Time.time + _phase;
            float x = 0f;
            if (_driftSpeed > 0f)
            {
                x = Mathf.Sin(time * _driftSpeed / _driftRange) * _driftRange;
            }

            float y = Mathf.Sin(time * Mathf.PI * 2f / _bobPeriod) * _bobHeight;
            _transform.position = _origin + new Vector3(x, y, 0f);
        }
    }
}
