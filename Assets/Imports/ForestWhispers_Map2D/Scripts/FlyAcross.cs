using UnityEngine;

namespace ForestWhispers.Map2D
{
    /// <summary>새 떼와 구름처럼 맵을 가로질러 움직이고, 끝에 닿으면 반대편에서 다시 나온다.</summary>
    public class FlyAcross : MonoBehaviour
    {
        [Header("Fly Settings")]
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _mapWidth = 104f;
        [SerializeField] private float _margin = 12f;

        private Transform _transform;
        private float _minX;
        private float _maxX;

        public void Setup(float speed, float mapWidth)
        {
            _speed = speed;
            _mapWidth = mapWidth;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            CacheRange();
        }

        private void Update()
        {
            Fly();
        }

        private void CacheComponent()
        {
            _transform = transform;
        }

        private void CacheRange()
        {
            float parallax = 1f + _transform.position.z / 25f;
            float center = _mapWidth * 0.5f;
            float halfWidth = (center + _margin) * parallax;
            _minX = center - halfWidth;
            _maxX = center + halfWidth;
        }

        private void Fly()
        {
            Vector3 position = _transform.position;
            position.x += _speed * Time.deltaTime;
            if (position.x > _maxX)
            {
                position.x = _minX;
            }
            else if (position.x < _minX)
            {
                position.x = _maxX;
            }

            _transform.position = position;
        }
    }
}
