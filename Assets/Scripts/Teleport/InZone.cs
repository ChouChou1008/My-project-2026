using UnityEngine;

public class InZone : MonoBehaviour
{
    [SerializeField] private Transform _teleportPoint;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private GameObject _globalVolume;
    [SerializeField] private Renderer _nextMap;              // 도착할 맵의 배경 (Sprite/Mesh 렌더러, 카메라 경계)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            TeleportPoint(other.transform);
            _globalVolume.SetActive(false);
        }
    }
    private void TeleportPoint(Transform target)
    {
        target.position = _teleportPoint.position;
        if (_playerCamera != null)
        {
            _playerCamera.SetBounds(_nextMap);               // 경계 교체 + 카메라 즉시 이동
        }
    }
   
}
