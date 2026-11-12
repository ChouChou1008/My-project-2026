using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 플레이어를 옆에서 따라가는 원근 카메라.
    /// 가로는 빠르게, 세로는 착지한 높이 기준으로 천천히 따라가서 점프할 때 화면이 출렁이지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow25D : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private float _heightOffset = 2.2f;

        [Header("Horizontal Settings")]
        [SerializeField] private float _lookAhead = 2.2f;
        [SerializeField] private float _lookAheadTime = 0.6f;
        [SerializeField] private float _deadZone = 0.4f;
        [SerializeField] private float _smoothTimeX = 0.18f;

        [Header("Vertical Settings")]
        [SerializeField] private float _smoothTimeY = 0.35f;
        [SerializeField] private float _jumpAllowance = 2.5f;
        [SerializeField] private float _fallCatchUp = 0.12f;

        [Header("View Settings")]
        [SerializeField] private float _distance = 25f;
        [SerializeField] private float _viewHeight = 10f;
        [SerializeField] private float _mapWidth = 104f;
        [SerializeField] private float _minY = 5f;
        [SerializeField] private float _maxY = 9.5f;
        [SerializeField] private float _pitch = 2f;   // 0에 가까울수록 레퍼런스 그림과 정확히 겹친다

        private Transform _transform;
        private Camera _camera;
        private PlayerController2D _player;
        private float _velocityX;
        private float _velocityY;
        private float _lookAheadVelocity;
        private float _currentLookAhead;
        private float _anchorX;
        private float _groundY = float.NaN;
        private float _lastTargetX = float.NaN;

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
            if (_target == null)
            {
                return;
            }

            UpdateLookAhead();
            UpdateGroundY();
            Follow();
        }

        private void CacheComponent()
        {
            _transform = transform;
            _camera = GetComponent<Camera>();
            _player = _target != null ? _target.GetComponent<PlayerController2D>() : null;
        }

        private void ApplyLens()
        {
            _camera.fieldOfView = 2f * Mathf.Atan(_viewHeight * 0.5f / _distance) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>내려다보는 각도만큼 카메라를 올려서 화면 중앙이 원하는 높이에 오게 한다.</summary>
        private float GetPitchLift()
        {
            return _distance * Mathf.Tan(_pitch * Mathf.Deg2Rad);
        }

        private void SnapToTarget()
        {
            if (_target == null)
            {
                return;
            }

            _player = _target.GetComponent<PlayerController2D>();
            _anchorX = _target.position.x;
            _groundY = _target.position.y;
            _lastTargetX = _target.position.x;
            _transform.position = new Vector3(ClampX(_anchorX), ClampY(_groundY + _heightOffset) + GetPitchLift(), -_distance);
        }

        /// <summary>달리는 방향 앞쪽을 더 보여주되, 방향을 바꿔도 화면이 튀지 않게 천천히 넘어간다.</summary>
        private void UpdateLookAhead()
        {
            float targetX = _target.position.x;
            float speed = (targetX - _lastTargetX) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastTargetX = targetX;

            float desired = Mathf.Clamp(speed * 0.25f, -1f, 1f) * _lookAhead;
            _currentLookAhead = Mathf.SmoothDamp(_currentLookAhead, desired, ref _lookAheadVelocity, _lookAheadTime);

            float offset = targetX - _anchorX;
            if (Mathf.Abs(offset) <= _deadZone)
            {
                return;
            }

            _anchorX = targetX - Mathf.Sign(offset) * _deadZone;
        }

        /// <summary>
        /// 세로 기준은 마지막으로 착지한 높이. 점프 중에는 움직이지 않다가
        /// 화면 밖으로 너무 올라가거나 아래로 떨어질 때만 따라간다.
        /// </summary>
        private void UpdateGroundY()
        {
            float y = _target.position.y;
            if (float.IsNaN(_groundY) || IsGrounded())
            {
                _groundY = y;
                return;
            }

            if (y > _groundY + _jumpAllowance)
            {
                _groundY = y - _jumpAllowance;
                return;
            }

            if (y >= _groundY)
            {
                return;
            }

            _groundY = Mathf.Lerp(_groundY, y, 1f - Mathf.Exp(-Time.deltaTime / _fallCatchUp));
        }

        private bool IsGrounded()
        {
            return _player != null && _player.IsGrounded;
        }

        private void Follow()
        {
            Vector3 position = _transform.position;
            float desiredX = ClampX(_anchorX + _currentLookAhead);
            float desiredY = ClampY(_groundY + _heightOffset) + GetPitchLift();
            position.x = Mathf.SmoothDamp(position.x, desiredX, ref _velocityX, _smoothTimeX);
            position.y = Mathf.SmoothDamp(position.y, desiredY, ref _velocityY, _smoothTimeY);
            position.z = -_distance;
            _transform.position = position;
        }

        private float ClampX(float x)
        {
            float halfWidth = _viewHeight * 0.5f * _camera.aspect;
            return Mathf.Clamp(x, halfWidth, _mapWidth - halfWidth);
        }

        private float ClampY(float y)
        {
            return Mathf.Clamp(y, _minY, _maxY);
        }
    }
}
