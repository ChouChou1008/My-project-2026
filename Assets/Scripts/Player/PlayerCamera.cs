using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _smoothSpeed = 5f;

    [Header("카메라 Y 범위 (X는 제한 없음)")]
    [SerializeField] private bool _clampY = true;         // 끄면 위아래도 제한 없이 따라감
    [SerializeField] private float _minY = -10f;
    [SerializeField] private float _maxY = 10f;

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

    private Vector3 GetTargetPosition()
    {
        Vector3 targetPos = _player.transform.position + _offset;

        if (_clampY)
        {
            targetPos.y = Mathf.Clamp(targetPos.y, _minY, _maxY);
        }

        return targetPos;
    }

    private void OnDrawGizmosSelected()
    {
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
