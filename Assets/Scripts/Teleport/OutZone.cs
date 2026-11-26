using UnityEngine;

public class OutZone : MonoBehaviour
{
    [SerializeField] private Transform _teleportPoint;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private GameObject _globalVolume;
    [SerializeField] private Renderer _nextMap;              // 도착할 맵의 배경 (Sprite/Mesh 렌더러, 카메라 경계)

    [Header("통과 조건 (비우면 조건 없음)")]
    [SerializeField] private ItemData _requiredKey;          // 이 키가 가방에 있어야 통과 (소모하지 않음)
    [SerializeField] private GameObject _lockMessage;        // 키가 없을 때 띄울 안내 UI (선택)


    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (!HasKey())
        {
            if (_lockMessage != null)
            {
                _lockMessage.SetActive(true);
            }
            return;                                          // 키 없음 → 텔레포트 안 함
        }

        TeleportPoint(other.transform);
        _globalVolume.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (_lockMessage != null)
        {
            _lockMessage.SetActive(false);
        }
    }

    private bool HasKey()
    {
        if (_requiredKey == null)                            // Map1-2처럼 조건 없는 곳
        {
            return true;
        }

        return GameManager.Instance != null &&
               GameManager.Instance.Inventory.Items.ContainsKey(_requiredKey);
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
