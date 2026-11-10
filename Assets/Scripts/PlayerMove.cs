using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMove : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputAction _moveAction;
    [SerializeField] private InputAction _jumpAction;

    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _jumpSpeed = 5.0f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _checkGroundDistance = 0.1f;   // 발 아래 여유 거리

    private const float VelocityDeadZone = 0.05f;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _verticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");

    private Animator _animator;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private Vector2 _moveInput;
    private bool _isGround;
    private bool _isJumpRequested;

    private void Awake()
    {
        CacheComponent();
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        BindInputEvent();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        UnbindInputEvent();
        ResetInput();                       // 채집 등으로 꺼질 때 남은 입력 제거
    }

    private void Update()
    {
        CheckGround();
        ReadJumpInput();
        Animate();
    }

    private void FixedUpdate()
    {
        if (!_isGround)
        {
            _isJumpRequested = false;
            return;
        }

        Move();
        Turn();
        Jump();
    }

    private void CacheComponent()
    {
        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
    }

    private void BindInputEvent()
    {
        _moveAction.performed += OnMove;
        _moveAction.canceled += OnMove;
    }

    private void UnbindInputEvent()
    {
        _moveAction.performed -= OnMove;
        _moveAction.canceled -= OnMove;
    }

    private void ResetInput()
    {
        _moveInput = Vector2.zero;
        _isJumpRequested = false;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void ReadJumpInput()
    {
        if (!_jumpAction.WasPressedThisFrame())
        {
            return;
        }

        _isJumpRequested = true;
    }

    private void CheckGround()
    {
        Vector3 origin = _collider.bounds.center;
        float distance = _collider.bounds.extents.y + _checkGroundDistance;   // 중심 → 발 + 여유

        _isGround = Physics.Raycast(origin, Vector3.down, distance, _groundLayer.value);
        
    }

    private void Move()
    {
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = _moveInput.x * _moveSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    private void Turn()
    {
        if (Mathf.Abs(_moveInput.x) < 0.01f)
        {
            return;
        }

        float yaw = _moveInput.x > 0f ? 0f: 180f;
        _rigidbody.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void Jump()
    {
        if (!_isJumpRequested)              // 버그 1 수정: 입력이 있을 때만
        {
            return;
        }

        _isJumpRequested = false;

        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.y = _jumpSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    private void Animate()
    {
        Vector3 velocity = _rigidbody.linearVelocity;

        float speedX = Mathf.Abs(velocity.x);
        float speedY = velocity.y;

        if (speedX < VelocityDeadZone)
        {
            speedX = 0f;
        }

        if (Mathf.Abs(speedY) < VelocityDeadZone)
        {
            speedY = 0f;
        }

        _animator.SetFloat(_speedHash, speedX);
        _animator.SetFloat(_verticalSpeedHash, speedY);
        _animator.SetBool(_isGroundedHash, _isGround);
    }
}