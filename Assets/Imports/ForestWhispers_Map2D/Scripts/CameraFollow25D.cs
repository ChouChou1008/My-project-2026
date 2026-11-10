using UnityEngine;

namespace ForestWhispers.Map2D
{
    /// <summary>
    /// 플레이어를 옆에서 따라가는 원근 카메라. 물체의 실제 z 깊이가 그대로 시차가 된다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow25D : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private float _heightOffset = 2.6f;
        [SerializeField] private float _lookAhead = 1.6f;
        [SerializeField] private float _smoothTime = 0.22f;

        [Header("View Settings")]
        [SerializeField] private float _distance = 25f;
        [SerializeField] private float _viewHeight = 10f;
        [SerializeField] private float _mapWidth = 104f;
        [SerializeField] private float _minY = 3.3f;
        [SerializeField] private float _maxY = 9.5f;

        private Transform _transform;
        private Camera _camera;
        private Vector3 _velocity;
        private float _lastTargetX = float.NaN;
        private float _facing = 1f;

        public void Setup(Transform target, float distance, float viewHeight, float mapWidth, float minY, float maxY)
        {
            _target = target;
            _distance = distance;
            _viewHeight = viewHeight;
            _mapWidth = mapWidth;
            _minY = minY;
            _maxY = maxY;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            ApplyLens();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            ApplyLens();
            Follow();
        }

        private void CacheComponent()
        {
            _transform = transform;
            _camera = GetComponent<Camera>();
        }

        private void ApplyLens()
        {
            _camera.fieldOfView = 2f * Mathf.Atan(_viewHeight * 0.5f / _distance) * Mathf.Rad2Deg;
        }

        private void SnapToTarget()
        {
            if (_target == null)
            {
                return;
            }

            _transform.position = GetDesiredPosition();
        }

        private void Follow()
        {
            if (_target == null)
            {
                return;
            }

            UpdateFacing();
            Vector3 desired = GetDesiredPosition();
            _transform.position = Vector3.SmoothDamp(_transform.position, desired, ref _velocity, _smoothTime);
        }

        private void UpdateFacing()
        {
            float targetX = _target.position.x;
            if (!float.IsNaN(_lastTargetX) && Mathf.Abs(targetX - _lastTargetX) > 0.001f)
            {
                _facing = Mathf.Sign(targetX - _lastTargetX);
            }

            _lastTargetX = targetX;
        }

        private Vector3 GetDesiredPosition()
        {
            float halfWidth = _viewHeight * 0.5f * _camera.aspect;
            float x = Mathf.Clamp(_target.position.x + _facing * _lookAhead, halfWidth, _mapWidth - halfWidth);
            float y = Mathf.Clamp(_target.position.y + _heightOffset, _minY, _maxY);
            return new Vector3(x, y, -_distance);
        }
    }
}
