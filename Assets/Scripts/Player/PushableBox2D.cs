using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PushableBox2D : MonoBehaviour
{
    [SerializeField] private LayerMask _blockLayer;          // 벽, 다른 박스 (Player는 빼기)

    
    private const RigidbodyConstraints2D LockedConstraints =
        RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private ContactFilter2D _filter;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

    public Collider2D Collider => _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<BoxCollider2D>();

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;       // 낭떠러지에서는 떨어지도록
        _rigidbody.constraints = LockedConstraints;

        _filter.useTriggers = false;
        _filter.SetLayerMask(_blockLayer);
    }

    public void BeginPush()
    {
        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void EndPush()
    {
        SetVelocityX(0f);
        _rigidbody.constraints = LockedConstraints;
    }

    public bool CanMove(float directionX, float distance)
    {
        int count = _rigidbody.Cast(new Vector2(directionX, 0f), _filter, _hits, distance + 0.02f);

        for (int i = 0; i < count; i++)
        {
            // 진행 방향을 정면으로 막는 면(벽, 옆 박스)만 장애물로 봄
            // 깔고 있는 바닥(법선이 위쪽)은 무시
            if (_hits[i].normal.x * directionX < -0.5f)
            {
                return false;
            }
        }

        return true;
    }

    public void SetVelocityX(float x)
    {
        Vector2 velocity = _rigidbody.linearVelocity;
        velocity.x = x;
        _rigidbody.linearVelocity = velocity;
    }
}