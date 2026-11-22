using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private MonsterState _monsterState;
    [SerializeField] private GameObject _boosIntro;
    [SerializeField] private PlayerController _controller;

    private Coroutine _boosIntroCoroutine;
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
        
        _controller.enabled = false;
        StartCoroutine(Corotine());
        /*_boosIntro.SetActive(true);
        _monsterState.Play();      */                      // 이미 실행됐으면 MonsterState가 무시함
    }

    private IEnumerator Corotine()
    {
        _boosIntro.SetActive(true);
        yield return new WaitForSeconds(4.5f);
        _monsterState.Play();
        yield return new WaitForSeconds(5f);
        _boosIntro.SetActive(false);
        _controller.enabled = true;
    }
}
