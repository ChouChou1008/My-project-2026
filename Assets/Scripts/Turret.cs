using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private MonsterState _monsterState;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (_monsterState == null)
        {
            Debug.LogError("[Turret] MonsterState가 연결되지 않았어요", this);
            return;
        }

        Debug.Log("감지");
        _monsterState.Play();                            // 이미 실행됐으면 MonsterState가 무시함
    }
}
