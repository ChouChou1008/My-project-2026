using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _smoothSpeed = 5f;

    [Header("맵 경계 (이 렌더러 밖은 화면에 안 보이게)")]
    [SerializeField] private Renderer _mapBounds;            // SpriteRenderer, MeshRenderer(Quad) 모두 가능. 비우면 아래 Y 범위만 사용

    [Header("카메라 Y 범위 (Map Bounds가 비어 있을 때만 사용)")]
    [SerializeField] private bool _clampY = true;
    [SerializeField] private float _minY = -10f;
    [SerializeField] private float _maxY = 10f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            GetTargetPosition(),
            _smoothSpeed * Time.deltaTime);
    }

    // 순간이동 직후 호출: 부드럽게 따라가지 않고 바로 목표 위치로 이동
    public void SnapToTarget()
    {
        transform.position = GetTargetPosition();
    }

    // 다른 맵으로 이동할 때 카메라 경계를 그 맵으로 교체
    public void SetBounds(Renderer map)
    {
        if (map != null)
        {
            _mapBounds = map;
        }
        SnapToTarget();
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 target = _player.transform.position + _offset;

        if (_mapBounds == null || _cam == null)
        {
            if (_clampY)
            {
                target.y = Mathf.Clamp(target.y, _minY, _maxY);
            }
            return target;
        }

        // 카메라가 보는 화면의 절반 높이/너비 (월드 단위)
        float halfH = _cam.orthographic
            ? _cam.orthographicSize
            : Mathf.Abs(_mapBounds.transform.position.z - target.z)
              * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * _cam.aspect;

        Bounds b = _mapBounds.bounds;

        // 맵이 화면보다 작으면 가운데 고정, 아니면 경계 안으로 Clamp
        target.x = (b.size.x < halfW * 2f)
            ? b.center.x
            : Mathf.Clamp(target.x, b.min.x + halfW, b.max.x - halfW);
        target.y = (b.size.y < halfH * 2f)
            ? b.center.y
            : Mathf.Clamp(target.y, b.min.y + halfH, b.max.y - halfH);

        return target;
    }

    private void OnDrawGizmosSelected()
    {
        if (_mapBounds != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_mapBounds.bounds.center, _mapBounds.bounds.size);
            return;
        }

        if (!_clampY)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        float z = transform.position.z;
        float left = transform.position.x - 20f;
        float right = transform.position.x + 20f;

        Gizmos.DrawLine(new Vector3(left, _minY, z), new Vector3(right, _minY, z));
        Gizmos.DrawLine(new Vector3(left, _maxY, z), new Vector3(right, _maxY, z));
    }
}
