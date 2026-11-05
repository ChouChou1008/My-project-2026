using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction _moveAction;     // Inspector에서 1D Axis(A/D, ←/→) 바인딩
    [SerializeField] private InputAction _jumpAction;
    
    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 3.0f;     // 달리기 클립에 맞는 속도

    private Animator _animator;
    private Rigidbody _rigidbody;
    private Transform _transform;
    private Vector2 _moveInput;

    private readonly int _speed = Animator.StringToHash("Speed");
    private readonly int _jump = Animator.StringToHash("VerticalSpeed");

    private void Awake() => CacheComponent();

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
    }

    private void Update()
    {
        Turn();
        Animate();
    }

    private void FixedUpdate()
    {
        Move();
        OnJump();
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

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();          // 왼쪽 -1, 오른쪽 1, 뗐을 때 0
    }
    private void OnJump()
    {
        if (_jumpAction.WasPressedThisFrame())
        {
            _rigidbody.AddForce(Vector3.up * _moveSpeed, ForceMode.VelocityChange);
        }
    }

    private void Move()
    {
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = _moveInput.x * _moveSpeed;
        _rigidbody.linearVelocity = velocity;             // y는 그대로 → 중력 유지
    }

    private void Turn()
    {
        if (Mathf.Abs(_moveInput.x) < 0.01f)
        {
            return;
        }

        float yaw = _moveInput.x > 0f ? 90f : -90f;         // 오른쪽 90, 왼쪽 -90
        _transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void Animate()
    {
        _animator.SetFloat(_speed, Mathf.Abs(_rigidbody.linearVelocity.x));
        _animator.SetFloat(_jump, Mathf.Abs(_rigidbody.linearVelocity.y));
    }

    private void CacheComponent()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
    }
}
