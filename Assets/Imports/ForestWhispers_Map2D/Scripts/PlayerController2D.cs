using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace ForestWhispers.Map2D
{
    /// <summary>
    /// 2D 평면(z=0)에서만 움직이는 횡스크롤 플레이어. A/D 이동, Shift 달리기, Space 점프(2단).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Move Settings")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _runMultiplier = 1.5f;
        [SerializeField] private float _acceleration = 70f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpSpeed = 13.5f;
        [SerializeField] private float _secondJumpSpeed = 12f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.12f;
        [SerializeField] private float _gravityScale = 3f;

        [Header("Ground Settings")]
        [SerializeField] private LayerMask _groundMask = ~(1 << 2);
        [SerializeField] private float _groundCheckRadius = 0.22f;

        [Header("Map Settings")]
        [SerializeField] private float _killY = -4f;
        [SerializeField] private Vector2 _startPoint;
        [SerializeField] private Vector2 _endPoint;
        [SerializeField] private Transform _visual;

        private Transform _transform;
        private Rigidbody2D _rigidbody;
        private float _inputX;
        private bool _isRunning;
        private bool _isGrounded;
        private bool _hasSecondJump;
        private float _coyoteTimer = -1f;
        private float _jumpBufferTimer = -1f;
        private Vector2 _lastSafePoint;

        public void Setup(float killY, Vector2 startPoint, Vector2 endPoint, Transform visual)
        {
            _killY = killY;
            _startPoint = startPoint;
            _endPoint = endPoint;
            _visual = visual;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            PlaceAtSpawn();
        }

        private void Update()
        {
            ReadInput();
            UpdateTimers();
            CheckFall();
            FaceDirection();
        }

        private void FixedUpdate()
        {
            CheckGround();
            Move();
            Jump();
        }

        private void CacheComponent()
        {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void PlaceAtSpawn()
        {
            Vector2 point = MapGate.ArrivedFromRight ? _endPoint : _startPoint;
            _transform.position = new Vector3(point.x, point.y, 0f);
            _lastSafePoint = point;
        }

        private void ReadInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            _inputX = Input.GetAxisRaw("Horizontal");
            _isRunning = Input.GetKey(KeyCode.LeftShift);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpBufferTimer = _jumpBufferTime;
            }
#elif ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            _inputX = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            _isRunning = keyboard.leftShiftKey.isPressed;
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _jumpBufferTimer = _jumpBufferTime;
            }
#endif
        }

        private void UpdateTimers()
        {
            _coyoteTimer -= Time.deltaTime;
            _jumpBufferTimer -= Time.deltaTime;
        }

        private void CheckFall()
        {
            if (_transform.position.y >= _killY)
            {
                return;
            }

            Respawn();
        }

        private void Respawn()
        {
            _transform.position = new Vector3(_lastSafePoint.x, _lastSafePoint.y + 0.5f, 0f);
            SetVelocity(Vector2.zero);
        }

        private void FaceDirection()
        {
            if (_visual == null || Mathf.Abs(_inputX) < 0.01f)
            {
                return;
            }

            Vector3 scale = _visual.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(_inputX);
            _visual.localScale = scale;
        }

        private void CheckGround()
        {
            Vector2 feet = (Vector2)_transform.position + Vector2.down * 0.05f;
            bool isFalling = GetVelocity().y <= 0.05f;
            _isGrounded = isFalling &&
                Physics2D.OverlapCircle(feet, _groundCheckRadius, _groundMask) != null;

            if (!_isGrounded)
            {
                return;
            }

            _coyoteTimer = _coyoteTime;
            _hasSecondJump = true;
            _lastSafePoint = _transform.position;
        }

        private void Move()
        {
            Vector2 velocity = GetVelocity();
            float speed = _moveSpeed * (_isRunning ? _runMultiplier : 1f);
            velocity.x = Mathf.MoveTowards(velocity.x, _inputX * speed, _acceleration * Time.fixedDeltaTime);
            SetVelocity(velocity);
            StickToSlope();
        }

        private void StickToSlope()
        {
            bool isIdleOnGround = _isGrounded && Mathf.Abs(_inputX) < 0.01f;
            _rigidbody.gravityScale = isIdleOnGround ? 0f : _gravityScale;
            if (!isIdleOnGround)
            {
                return;
            }

            SetVelocity(Vector2.zero);
        }

        private void Jump()
        {
            if (_jumpBufferTimer <= 0f)
            {
                return;
            }

            if (_coyoteTimer > 0f)
            {
                ApplyJump(_jumpSpeed);
                return;
            }

            if (!_hasSecondJump)
            {
                return;
            }

            _hasSecondJump = false;
            ApplyJump(_secondJumpSpeed);
        }

        private void ApplyJump(float speed)
        {
            Vector2 velocity = GetVelocity();
            velocity.y = speed;
            SetVelocity(velocity);
            _jumpBufferTimer = -1f;
            _coyoteTimer = -1f;
        }

        private Vector2 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rigidbody.linearVelocity;
#else
            return _rigidbody.velocity;
#endif
        }

        private void SetVelocity(Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = velocity;
#else
            _rigidbody.velocity = velocity;
#endif
        }
    }
}
