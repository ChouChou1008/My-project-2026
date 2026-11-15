using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputAction _moveAction;
    [SerializeField] private InputAction _jumpAction;
    [SerializeField] private InputAction _gatherAction;
    [SerializeField] private InputAction _speedUpAction;
    [SerializeField] private InputAction _openInventoryAction;
    
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.0f;
    [SerializeField] private float _jumpSpeed = 5.0f;
    [SerializeField] private Transform _model;                   // 좌우로 돌릴 모델 (Visual 또는 Model)


    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _checkGroundDistance = 0.1f;


    [SerializeField] private float _gatherTimer = 1.8f;          // 채집하는 시간
    
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _inventoryMaxcanvas;
    //[SerializeField] private Canvas _inventoryMaxcanvas;
    [SerializeField] private Image _image;

    private const float VelocityDeadZone = 0.05f;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _verticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _gatherHash = Animator.StringToHash("Gather");

    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private Vector2 _moveInput;
    private ItemDetector _itemDetector;                        // ItemDetector → ItemDetector2D
    private Coroutine _fullMessageCoroutine;

    private float _gatherTime = -1f;
    private bool IsGathering => _gatherTime > 0f;
    private bool _isGround;
    private bool _isJumpRequested;

    private void Awake()
    {
        CacheComponent();
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _speedUpAction.Enable();
        _jumpAction.Enable();
        _openInventoryAction.Enable();
        ItemDetector.OnCheckGather += OnItemCheckGather;       // ItemDetector → ItemDetector2D
        BindInputEvent();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _speedUpAction.Disable();
        _jumpAction.Disable();
        _openInventoryAction.Disable();
        _gatherAction.Disable();
        ItemDetector.OnCheckGather -= OnItemCheckGather;       // ItemDetector → ItemDetector2D
        UnbindInputEvent();
        ResetInput();
    }

    private void Update()
    {
        CheckGround();
        ReadGather();
        UpdateGather();
        ReadJumpInput();
        OpenInventory();
        Animate();
    }

    private void FixedUpdate()
    {
        if (IsGathering)
        {
            StopHorizontal();
            return;
        }

        if (!_isGround)
        {
            _isJumpRequested = false;
            return;
        }

        Move();
        Turn();
        Jump();
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
    

    private void OpenInventory()
    {
        if (!_openInventoryAction.WasPressedThisFrame())
        {
            return;
        }

        _canvas.SetActive(!_canvas.activeSelf);
    }

    private void FullMessage()
    {
        if (_fullMessageCoroutine != null)
        {
            StopCoroutine(_fullMessageCoroutine);
        }

        _fullMessageCoroutine = StartCoroutine(StartCanvas());
    }

    private IEnumerator StartCanvas()
    {
        _inventoryMaxcanvas.gameObject.SetActive(true);
        float t = Time.time + 3f;
        Color color = _image.color;
        color.a = 1f;
        _image.color = color;

        while (Time.time <= t)
        {
            yield return null;
            color.a = Mathf.Clamp01(t - Time.time);
            _image.color = color;
        }

        _inventoryMaxcanvas.gameObject.SetActive(false);
        _fullMessageCoroutine = null;
    }
    

    private void OnItemCheckGather(ItemDetector item)        
    {
        if (IsGathering)
        {
            return;
        }

        if (item.IsCheckGather)
        {
            _itemDetector = item;
            _gatherAction.Enable();
            return;
        }

        if (_itemDetector != item)
        {
            return;
        }

        ClearGatherTarget();
    }

    private void ReadGather()
    {
        if (!_gatherAction.WasPressedThisFrame())
        {
            return;
        }

        if (IsGathering ||
            !_isGround)
        {
            return;
        }

        if (_inventory.IsFull)
        {
            FullMessage();
            return;
        }

        StartGather();
    }

    private void StartGather()
    {
        FaceGatherTarget();
        _gatherTime = _gatherTimer;
        _isJumpRequested = false;
        _animator.SetTrigger(_gatherHash);
        StopHorizontal();
    }

    private void UpdateGather()
    {
        if (!IsGathering)
        {
            return;
        }

        _gatherTime -= Time.deltaTime;

        if (IsGathering ||
            _itemDetector == null)
        {
            return;
        }

        Item item = _itemDetector.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning($"{_itemDetector.name}에 Item 컴포넌트가 없음");
            ClearGatherTarget();
            return;
        }

        if (!_inventory.TryAdd(item.Data))
        {
            FullMessage();
            ClearGatherTarget();
            return;
        }

        ReturnGatheredItem();
        ClearGatherTarget();
    }

    private void ReturnGatheredItem()
    {
        Items poolItem = _itemDetector.GetComponent<Items>();
        if (poolItem == null)
        {
            Destroy(_itemDetector.gameObject);
            return;
        }

        poolItem.OnReturnToPool();
    }

    private void ClearGatherTarget()
    {
        _itemDetector = null;
        _gatherAction.Disable();
    }
    

    private void ReadJumpInput()
    {
        if (IsGathering)
        {
            return;
        }

        if (!_jumpAction.WasPressedThisFrame())
        {
            return;
        }

        _isJumpRequested = true;
    }

    private void CheckGround()
    {
        Vector2 origin = _collider.bounds.center;
        float distance = _collider.bounds.extents.y + _checkGroundDistance;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, _groundLayer);
        _isGround = hit.collider != null;                       
    }

    private void Move()
    {
        float maxSpeed = _speedUpAction.IsPressed() ? _runSpeed : _moveSpeed;

        Vector2 velocity = _rigidbody.linearVelocity;
        velocity.x = _moveInput.x * maxSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    private void StopHorizontal()
    {
        Vector2 velocity = _rigidbody.linearVelocity;
        velocity.x = 0f;
        _rigidbody.linearVelocity = velocity;
    }

    private void Turn()
    {
        if (Mathf.Abs(_moveInput.x) < 0.01f)
        {
            return;
        }

        FaceDirection(_moveInput.x);
    }

    private void FaceDirection(float directionX)
    {
        if (_model == null)
        {
            return;
        }

        float yaw = directionX > 0f ? 90f : -90f;
        _model.localRotation = Quaternion.Euler(0f, yaw, 0f);    // Rigidbody2D 대신 모델을 회전
    }

    private void FaceGatherTarget()
    {
        if (_itemDetector == null)
        {
            return;
        }

        float directionX = _itemDetector.transform.position.x - _rigidbody.position.x;

        if (Mathf.Abs(directionX) < 0.05f)
        {
            return;
        }

        FaceDirection(directionX);
    }

    private void Jump()
    {
        if (!_isJumpRequested)
        {
            return;
        }

        _isJumpRequested = false;

        Vector2 velocity = _rigidbody.linearVelocity;
        velocity.y = _jumpSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    private void Animate()
    {
        Vector2 velocity = _rigidbody.linearVelocity;

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
    private void CacheComponent()
    {
        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CapsuleCollider2D>();
        _rigidbody.freezeRotation = true;                       
    }
}