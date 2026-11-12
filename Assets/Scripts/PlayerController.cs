using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputAction _moveAction;
    [SerializeField] private InputAction _jumpAction;
    [SerializeField] private InputAction _gatherAction; 
    [SerializeField] private InputAction _speedUpAction;
    [SerializeField] private InputAction _openInventoryAction;

    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.0f;
    [SerializeField] private float _jumpSpeed = 5.0f;

    
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _checkGroundDistance = 0.1f; 
    [SerializeField] private float _gatherTimer = 1.8f; //채집하는 시간
    
    
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Canvas _inventoryMaxcanvas;
    [SerializeField] private Image _image;
    
    private float _gatherTime = -1f;
    private bool IsGathering => _gatherTime > 0f;
    private const float VelocityDeadZone = 0.05f;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _verticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _gatherHash = Animator.StringToHash("Gather");

    private Coroutine _fullMessageCoroutine;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private Vector2 _moveInput;
    private ItemDetector _itemDetector;
    private Transform _transform;
    
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
        ItemDetector.OnCheckGather += OnItemCheckGather;
        BindInputEvent();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _speedUpAction.Disable();
        _jumpAction.Disable();
        _openInventoryAction.Disable();
        _gatherAction.Disable();
        ItemDetector.OnCheckGather -= OnItemCheckGather;
        UnbindInputEvent();
        ResetInput();                       // 채집 등으로 꺼질 때 남은 입력 제거
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

    private void OpenInventory()
    {
        if (!_openInventoryAction.WasPressedThisFrame()) return;
        
        _canvas.SetActive(!_canvas.activeSelf);
    }
    
    private void ReadGather()
    {
        if (!_gatherAction.WasPressedThisFrame()) return;
        if (IsGathering || !_isGround) return;
        if (_inventory.IsFull)
        {
            FullMessage();
            return;
        }
        StartGather();      
        
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
       float t  = Time.time + 3f;
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

    private void OnItemCheckGather(ItemDetector item)  //감지되고 활성화
    {
        if (IsGathering) return;
        if (item.IsCheckGather)
        {
            _itemDetector = item;
            _gatherAction.Enable();
            return;
        }

        if (_itemDetector != item) return;

        _itemDetector = null;
        _gatherAction.Disable();
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
        if (!IsGathering) return;
        _gatherTime -= Time.deltaTime;
        if (IsGathering) return;
        if (_itemDetector == null) return;
        
        Item item = _itemDetector.GetComponent<Item>();
        if (!_inventory.TryAdd(item.Data))
        {
            FullMessage();
            ClearGatherTarget();
            return;
        }
        Debug.Log($"획득한 아이템: {_itemDetector.name}");
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
    
    private void StopHorizontal()
    {
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = 0f;
        _rigidbody.linearVelocity = velocity;
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
        if (IsGathering) return;
        if (!_jumpAction.WasPressedThisFrame()) return;
        
        _isJumpRequested = true;
    }

    private void CheckGround()
    {
        Vector3 origin = _collider.bounds.center; 
        float distance = _collider.bounds.extents.y + _checkGroundDistance;   // 중심 → 발 + 여유

        _isGround = Physics.Raycast(origin, Vector3.down, distance, _groundLayer.value);//자기 몸중심으로 하부까지
        
    }

    private void Move()
    {
        float maxSpeed = _speedUpAction.IsPressed()? _runSpeed : _moveSpeed;
        
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = _moveInput.x * maxSpeed;
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
        float yaw = directionX > 0f ? 0f : 180f;
        _rigidbody.rotation = Quaternion.Euler(0, yaw, 0f);
    }

    private void FaceGatherTarget()
    {
        float directionX = _itemDetector.transform.position.x - _rigidbody.position.x;

        if (Mathf.Abs(directionX) < 0.05f)
        {
            return;
        }
        FaceDirection(directionX);
    }

    private void Jump()
    {
        if (!_isJumpRequested)  // 입력이 있을 때만
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
    private void CacheComponent()
    {
        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _transform = GetComponent<Transform>();
    }
}