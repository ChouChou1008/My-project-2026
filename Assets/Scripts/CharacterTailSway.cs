using UnityEngine;

// 꼬리 뼈(Tail01~Tail04)를 절차적으로 흔드는 컴포넌트
// - 걷기/달리기 애니메이션에는 꼬리 키가 없으므로 LateUpdate에서 Animator 결과 위에 덮어씀
// - 이동 속도에 비례해 흔들림 폭/속도가 커짐
public class CharacterTailSway : MonoBehaviour
{
    [Header("Bone Settings")]
    [SerializeField] private Transform[] _tailBones;        // Tail01 → Tail04 순서
    [SerializeField] private Transform _root;               // 캐릭터 루트 (비우면 자기 자신)

    [Header("Sway Settings")]
    [SerializeField] private float _idleAngle = 4f;         // 정지 시 좌우 흔들림 각도
    [SerializeField] private float _moveAngle = 18f;        // 최고 속도 시 좌우 흔들림 각도
    [SerializeField] private float _idleFrequency = 1.2f;   // 정지 시 흔들림 속도 (Hz)
    [SerializeField] private float _moveFrequency = 3.0f;   // 최고 속도 시 흔들림 속도 (Hz)
    [SerializeField] private float _phaseOffset = 0.6f;     // 뼈마다 늦게 따라오는 정도 (rad)
    [SerializeField] private float _tipMultiplier = 1.4f;   // 끝으로 갈수록 각도 증가 배율

    [Header("Bounce Settings")]
    [SerializeField] private float _bounceAngle = 8f;       // 이동 시 위아래 출렁임 각도

    [Header("Speed Settings")]
    [SerializeField] private float _maxSpeed = 5f;          // 이 속도에서 _moveAngle 도달
    [SerializeField] private float _speedSmooth = 6f;       // 속도 변화 보간 속도

    private Quaternion[] _restRotations;
    private Vector3[] _sideAxes;
    private Vector3[] _bounceAxes;
    private Transform _transform;
    private Vector3 _lastPosition;
    private float _speed01 = -1f;
    private float _phase;

    private void Awake()
    {
        CacheComponent();
    }

    private void Start()
    {
        CacheRestPose();
        ResetTail();
    }

    private void LateUpdate()
    {
        UpdateSpeed();
        UpdatePhase();
        Sway();
    }

    private void CacheComponent()
    {
        _transform = transform;

        if (_root == null)
        {
            _root = _transform;
        }
    }

    private void CacheRestPose()
    {
        int count = _tailBones.Length;
        _restRotations = new Quaternion[count];
        _sideAxes = new Vector3[count];
        _bounceAxes = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform bone = _tailBones[i];
            if (bone == null)
            {
                continue;
            }

            _restRotations[i] = bone.localRotation;

            // 캐릭터 기준 축을 각 뼈의 로컬 축으로 변환 → 뼈 축 방향과 무관하게 동작
            Quaternion toLocal = Quaternion.Inverse(bone.rotation);
            _sideAxes[i] = toLocal * _root.forward;
            _bounceAxes[i] = toLocal * _root.right;
        }
    }

    private void ResetTail()
    {
        _lastPosition = _root.position;
        _speed01 = 0f;
        _phase = 0f;

        for (int i = 0; i < _tailBones.Length; i++)
        {
            if (_tailBones[i] == null)
            {
                continue;
            }

            _tailBones[i].localRotation = _restRotations[i];
        }
    }

    private void UpdateSpeed()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 delta = _root.position - _lastPosition;
        delta.y = 0f;
        _lastPosition = _root.position;

        float target = Mathf.Clamp01(delta.magnitude / Time.deltaTime / _maxSpeed);
        _speed01 = Mathf.Lerp(_speed01, target, _speedSmooth * Time.deltaTime);
    }

    private void UpdatePhase()
    {
        float frequency = Mathf.Lerp(_idleFrequency, _moveFrequency, _speed01);
        _phase += frequency * Mathf.PI * 2f * Time.deltaTime;
    }

    private void Sway()
    {
        float sideAngle = Mathf.Lerp(_idleAngle, _moveAngle, _speed01);
        float bounceAngle = _bounceAngle * _speed01;
        int count = _tailBones.Length;

        for (int i = 0; i < count; i++)
        {
            Transform bone = _tailBones[i];
            if (bone == null)
            {
                continue;
            }

            float weight = Mathf.Lerp(1f, _tipMultiplier, count > 1 ? (float)i / (count - 1) : 0f);
            float wave = _phase - i * _phaseOffset;

            Quaternion side = Quaternion.AngleAxis(Mathf.Sin(wave) * sideAngle * weight, _sideAxes[i]);
            Quaternion bounce = Quaternion.AngleAxis(Mathf.Sin(wave * 2f) * bounceAngle * weight, _bounceAxes[i]);

            bone.localRotation = _restRotations[i] * side * bounce;
        }
    }
}
