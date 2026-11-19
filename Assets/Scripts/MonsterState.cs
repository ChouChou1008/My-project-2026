using System.Collections;
using UnityEngine;

public class MonsterState : MonoBehaviour
{
    private static readonly int AnimationHash = Animator.StringToHash("animation");

    [SerializeField] private GameObject _hideTarget;     // 연출 끝나고 끌 오브젝트 (비우면 자기 자신)
    [SerializeField] private float _waitTime = 4f;       // 처음 동작(4번) 재생 시간
    [SerializeField] private float _turnDelay = 0.8f;    // 돌아선 뒤 대기
    [SerializeField] private float _moveTime = 1.5f;     // 걸어가는 시간
    [SerializeField] private float _moveSpeed = 3f;

    private Animator _animator;
    private bool _isPlayed;                              // 한 번만 실행

    public bool IsPlayed => _isPlayed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_hideTarget == null)
        {
            _hideTarget = gameObject;
        }
    }

    public void Play()
    {
        if (_isPlayed)
        {
            return;
        }

        _isPlayed = true;
        StartCoroutine(MonsterMove());
    }

    private IEnumerator MonsterMove()
    {
        _animator.SetInteger(AnimationHash, 4);
        yield return new WaitForSeconds(_waitTime);

        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return new WaitForSeconds(_turnDelay);

        _animator.SetInteger(AnimationHash, 2);

        float elapsed = 0f;
        while (elapsed < _moveTime)                      // 매 프레임 조금씩 이동
        {
            transform.Translate(Vector3.forward * (_moveSpeed * Time.deltaTime));
            elapsed += Time.deltaTime;
            yield return null;
        }

        _hideTarget.SetActive(false);
    }
}
