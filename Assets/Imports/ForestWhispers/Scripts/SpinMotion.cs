using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>유적 문양처럼 제자리에서 천천히 회전한다.</summary>
    public class SpinMotion : MonoBehaviour
    {
        [Header("Spin Settings")]
        [SerializeField] private float _degreesPerSecond = 12f;

        private Transform _transform;

        public void Setup(float degreesPerSecond)
        {
            _degreesPerSecond = degreesPerSecond;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Update()
        {
            Spin();
        }

        private void CacheComponent()
        {
            _transform = transform;
        }

        private void Spin()
        {
            _transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);
        }
    }
}
