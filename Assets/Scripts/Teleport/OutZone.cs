using UnityEngine;

public class OutZone : MonoBehaviour
{
    [SerializeField] private Transform _teleportPoint;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private GameObject _globalVolume;
    
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            TeleportPoint(other.transform);
            _globalVolume.SetActive(true);
        }
    }
    private void TeleportPoint(Transform target)
    {
        target.position = _teleportPoint.position;
        if (_playerCamera != null)
        {
            _playerCamera.SnapToTarget();                    // 카메라 즉시 이동 (미끄러지지 않게)
        }
    }
}
