using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction _moveAction;    
    [SerializeField] private InputAction _jumpAction;
    [SerializeField] private float _jumpSpeed = 5.0f;
    [SerializeField] private LayerMask _groundLayer;

    private const float VelocityDeadZone = 0.05f;
    private bool _jumpRequested;
    
    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 3.0f;    

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
        Animate();
    }

    private void FixedUpdate()
    {
        Move();
        Turn();
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
        _moveInput = context.ReadValue<Vector2>();        
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
        _rigidbody.linearVelocity = velocity;           
    }

    private void Turn()
    {
        if (Mathf.Abs(_moveInput.x) < 0.01f) // 회전
        {
            return;
        }

        float yaw = _moveInput.x > 0f ? 90f : -90f;
        _rigidbody.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    private void Animate()
    {
        Vector3 velocity = _rigidbody.linearVelocity;

        float speedX = Mathf.Abs(velocity.x);
        float speedY = velocity.y;

        if (speedX < VelocityDeadZone) speedX = 0f;
        if (Mathf.Abs(speedY) < VelocityDeadZone) speedY = 0f;

        _animator.SetFloat(_speed, speedX);
        _animator.SetFloat(_jump, speedY);
    }

    private void CacheComponent()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _transform = transform;
    }
}
