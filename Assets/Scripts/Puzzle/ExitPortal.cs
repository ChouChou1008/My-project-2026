using UnityEngine;

/// <summary>
/// 맵과 맵 사이의 빛나는 문.
/// 닫혀 있으면 어둡고, 보이지 않는 벽(_barrier)이 길을 막는다.
/// 열리면 벽이 사라지고 빛이 숨 쉬듯 깜빡여서 그대로 걸어 다음 맵으로 넘어갈 수 있다.
/// 마지막 문(_isFinal)은 들어가면 클리어 문구를 띄운다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ExitPortal : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private Collider2D _barrier;            // 닫혀 있는 동안 길을 막는 벽 (없으면 처음부터 지나갈 수 있음)
    [SerializeField] private bool _isFinal;
    [SerializeField] private string _clearMessage = "숲의 끝에 도착했어요!";
    [SerializeField] private Light _light;
    [SerializeField] private GameObject _openEffect;         // 열렸을 때만 켜는 이펙트
    [SerializeField] private float _closedIntensity = 0.3f;
    [SerializeField] private float _minIntensity = 2f;
    [SerializeField] private float _maxIntensity = 5f;
    [SerializeField] private float _pulseSpeed = 2.5f;
    [SerializeField] private bool _startOpen;

    private bool _isOpen;
    private bool _isUsed;

    public void Setup(LayerMask playerLayer, Collider2D barrier, bool isFinal, Light glowLight, GameObject openEffect, bool startOpen)
    {
        _playerLayer = playerLayer;
        _barrier = barrier;
        _isFinal = isFinal;
        _light = glowLight;
        _openEffect = openEffect;
        _startOpen = startOpen;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        SetVisual(false);

        if (_startOpen)
        {
            Open();
        }
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        SetVisual(true);
    }

    private void Update()
    {
        if (!_isOpen || _light == null)
        {
            return;
        }

        float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
        _light.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);
    }

    private void SetVisual(bool isOpen)
    {
        if (_barrier != null)
        {
            _barrier.enabled = !isOpen;
        }

        if (_light != null)
        {
            _light.intensity = isOpen ? _minIntensity : _closedIntensity;
        }

        if (_openEffect != null)
        {
            _openEffect.SetActive(isOpen);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryFinish(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryFinish(other);                                    // 문 앞에 서 있는 동안 열린 경우도 처리
    }

    private void TryFinish(Collider2D other)
    {
        if (!_isFinal ||
            !_isOpen ||
            _isUsed ||
            (_playerLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        _isUsed = true;
        PuzzleFader.FadeToMessage(_clearMessage);
    }
}
