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

    [SerializeField] private GameObject _trail;
    [SerializeField] private Image _image;

    [SerializeField] private LayerMask _pushableLayer;
    [SerializeField] private float _pushCheckDistance = 0.08f;
    [SerializeField] private float _pushSpeed = 1f;           // 걷기(2.0)보다 느리게
    [SerializeField] private float _pushAccel = 3f;             // 클수록 가벼운 박스
    [SerializeField] private float _pushLeanInTime = 0.15f;     // 힘 주는 시간
    [SerializeField] private float _pushAnimNativeSpeed = 1.2f; // 밀기 클립의 원래 이동 속도
    [SerializeField] private float _pushAlignStrength = 10f;
    
    
    
    private const float VelocityDeadZone = 0.05f;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _verticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _gatherHash = Animator.StringToHash("Gather");
    private readonly int _isPushingHash = Animator.StringToHash("IsPushing");
    private readonly int _pushSpeedHash = Animator.StringToHash("PushSpeed");
    private readonly int _pushAnimSpeedHash = Animator.StringToHash("PushAnimSpeed");

    private PushableBox2D _pushTarget;
    private float _facingX = 1f;
    private float _pushTimer;
    private float _pushCurSpeed;
    private bool IsPushing => _pushTarget != null;
    
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private Vector2 _moveInput;
    private ItemDetector _itemDetector;                    
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
        ResetInput();
        
        if(IsPushing) EndPush();
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
            if(IsPushing) EndPush();
            StopHorizontal();
            return;
        }

        if (!_isGround)
        {
            if(IsPushing) EndPush();
            _isJumpRequested = false;
            return;
        }

        if (UpdatePush())
        {
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
            IsPushing ||
            !_isGround
            )
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
        float maxSpeed;
        if (_speedUpAction.IsInProgress())
        {
            maxSpeed = _runSpeed;
            _trail.gameObject.SetActive(true);
        }
        else
        {
            maxSpeed = _moveSpeed;
            _trail.gameObject.SetActive(false);
        }

        
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
        _facingX = Mathf.Sign(directionX);
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

        private bool UpdatePush()
    {
        bool pressingForward = Mathf.Abs(_moveInput.x) > 0.1f &&
                               Mathf.Sign(_moveInput.x) == _facingX;

        if (!IsPushing)
        {
            if (!pressingForward) return false;

            PushableBox2D box = FindPushable();
            if (box == null) return false;

            BeginPush(box);
        }
        else if (!pressingForward || FindPushable() != _pushTarget)
        {
            EndPush();          // 입력을 뗐거나, 방향을 바꿨거나, 박스가 떨어짐
            return false;
        }

        _isJumpRequested = false;
        float dt = Time.fixedDeltaTime;
        _pushTimer += dt;
        
        if (_pushTimer < _pushLeanInTime)
        {
            ApplyPushVelocity(0f);
            return true;
        }

        // 2) 천천히 가속, 막혀 있으면 버티기
        _pushCurSpeed = Mathf.MoveTowards(_pushCurSpeed, _pushSpeed, _pushAccel * dt);

        if (!_pushTarget.CanMove(_facingX, _pushCurSpeed * dt))
        {
            _pushCurSpeed = 0f;
        }

        ApplyPushVelocity(_pushCurSpeed * _facingX);
        return true;
    }

    private PushableBox2D FindPushable()
    {
        Bounds bounds = _collider.bounds;

        // 발 위 0.05부터 콜라이더 중심까지의 높이를 검사
        float bottom = bounds.min.y + 0.05f;
        float top = bounds.center.y;
        Vector2 origin = new Vector2(bounds.center.x, (bottom + top) * 0.5f);
        Vector2 size = new Vector2(0.05f, top - bottom);

        float distance = bounds.extents.x + _pushCheckDistance;

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, new Vector2(_facingX, 0f),
            distance, _pushableLayer);
        return hit.collider != null ? hit.collider.GetComponent<PushableBox2D>() : null;
    }

    private void BeginPush(PushableBox2D box)
    {
        _pushTarget = box;
        _pushTimer = 0f;
        _pushCurSpeed = 0f;
        _trail.gameObject.SetActive(false);
        box.BeginPush();
    }

    private void EndPush()
    {
        if (_pushTarget != null)
        {
            _pushTarget.EndPush();
        }

        _pushTarget = null;
        _pushCurSpeed = 0f;
    }

    private void ApplyPushVelocity(float velocityX)
    {
        // 박스 면에 딱 붙도록 조금씩 보정 (손이 박스에서 뜨거나 파고들지 않게)
        Bounds box = _pushTarget.Collider.bounds;
        float boxEdge = _facingX > 0f ? box.min.x : box.max.x;
        float targetX = boxEdge - _facingX * (_collider.bounds.extents.x + 0.01f);
        float correction = (targetX - _collider.bounds.center.x) * _pushAlignStrength;

        Vector2 velocity = _rigidbody.linearVelocity;
        velocity.x = velocityX + correction;
        _rigidbody.linearVelocity = velocity;

        _pushTarget.SetVelocityX(velocityX);
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
        _animator.SetBool(_isPushingHash,IsPushing);
        
        if (IsPushing)
        {
            float normalized = _pushCurSpeed / _pushSpeed;
            _animator.SetFloat(_pushSpeedHash, normalized, 0.1f, Time.deltaTime);

            // 발이 바닥에서 미끄러져 보이지 않도록 재생 속도를 실제 이동 속도에 맞춤
            float animSpeed = _pushCurSpeed > 0.01f ? _pushCurSpeed / _pushAnimNativeSpeed : 1f;
            _animator.SetFloat(_pushAnimSpeedHash, animSpeed);
        }
        else
        {
            _animator.SetFloat(_pushAnimSpeedHash, 1f);
            _animator.SetFloat(_pushSpeedHash, 0f);
        }
    }
    private void CacheComponent()
    {
        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CapsuleCollider2D>();
        _rigidbody.freezeRotation = true;                       
    }
}