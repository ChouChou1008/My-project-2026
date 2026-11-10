using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 채집 동작을 Humanoid IK로 만든다. Animator와 같은 오브젝트(Model)에 붙인다.
    /// Idle을 재생하는 채로 몸을 숙이므로 꼬리와 호흡 애니메이션은 그대로 유지된다.
    /// Animator Controller 레이어의 IK Pass가 켜져 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class GatherPose : MonoBehaviour
    {
        private enum Phase
        {
            None = -1,
            Down,
            Pick,
            Lift,
            Up
        }

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Timing Settings")]
        [SerializeField] private float _downTime = 0.4f;
        [SerializeField] private float _pickTime = 0.3f;
        [SerializeField] private float _liftTime = 0.4f;
        [SerializeField] private float _upTime = 0.35f;

        [Header("Pose Settings")]
        [SerializeField] private float _crouchDepth = 0.42f;
        [SerializeField] private float _bendAngle = 32f;
        [SerializeField] private float _stepLength = 0.22f;
        [SerializeField] private float _armReach = 0.7f;

        private Animator _animator;
        private Transform _transform;
        private Transform _rightHand;
        private Transform _chest;
        private Vector3 _targetPoint;
        private Phase _phase = Phase.None;
        private float _phaseTimer = -1f;
        private bool _hasSpeedParameter;

        public Transform RightHand => _rightHand;
        public float PickDelay => _downTime + _pickTime * 0.5f;
        public float TotalTime => _downTime + _pickTime + _liftTime + _upTime;
        public bool IsPlaying => _phase != Phase.None;

        public void Play(Vector3 targetPoint)
        {
            _targetPoint = targetPoint;
            _phase = Phase.Down;
            _phaseTimer = 0f;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            CacheBones();
        }

        private void Update()
        {
            AdvancePhase();
            HoldIdle();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            ApplyPose();
        }

        private void CacheComponent()
        {
            _animator = GetComponent<Animator>();
            _transform = transform;
        }

        private void CacheBones()
        {
            if (!_animator.isHuman)
            {
                Debug.LogWarning("GatherPose는 Humanoid Rig가 필요합니다: " + name);
                enabled = false;
                return;
            }

            _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (_chest == null)
            {
                _chest = _animator.GetBoneTransform(HumanBodyBones.Spine);
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                _hasSpeedParameter |= parameter.nameHash == SpeedHash;
            }
        }

        private void AdvancePhase()
        {
            if (_phase == Phase.None)
            {
                return;
            }

            _phaseTimer += Time.deltaTime;
            float length = GetPhaseLength(_phase);
            if (_phaseTimer < length)
            {
                return;
            }

            _phaseTimer -= length;
            _phase = _phase == Phase.Up ? Phase.None : _phase + 1;
        }

        private void HoldIdle()
        {
            if (_phase == Phase.None || !_hasSpeedParameter)
            {
                return;
            }

            _animator.SetFloat(SpeedHash, 0f);
        }

        private float GetPhaseLength(Phase phase)
        {
            switch (phase)
            {
                case Phase.Down: return _downTime;
                case Phase.Pick: return _pickTime;
                case Phase.Lift: return _liftTime;
                default: return _upTime;
            }
        }

        private float GetPhaseProgress()
        {
            float t = Mathf.Clamp01(_phaseTimer / GetPhaseLength(_phase));
            return t * t * (3f - 2f * t);
        }

        private float GetCrouchWeight()
        {
            float progress = GetPhaseProgress();
            switch (_phase)
            {
                case Phase.Down: return progress;
                case Phase.Pick: return 1f;
                case Phase.Lift: return 1f - progress * 0.75f;
                default: return 0.25f * (1f - progress);
            }
        }

        private float GetHandWeight()
        {
            float progress = GetPhaseProgress();
            switch (_phase)
            {
                case Phase.Down: return progress;
                case Phase.Up: return 1f - progress;
                default: return 1f;
            }
        }

        private void ApplyPose()
        {
            if (_phase == Phase.None)
            {
                ClearWeights();
                return;
            }

            float crouch = GetCrouchWeight();
            float scale = _animator.humanScale;
            PinFoot(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, _stepLength * scale * crouch, scale);
            PinFoot(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, -_stepLength * 0.6f * scale * crouch, scale);
            MoveBody(crouch, scale);
            ReachHand(scale);
            Look();
        }

        private void PinFoot(AvatarIKGoal goal, AvatarIKHint hint, float forwardOffset, float scale)
        {
            Vector3 position = _animator.GetIKPosition(goal) + _transform.forward * forwardOffset;
            _animator.SetIKPositionWeight(goal, 1f);
            _animator.SetIKRotationWeight(goal, 1f);
            _animator.SetIKPosition(goal, position);
            _animator.SetIKRotation(goal, _animator.GetIKRotation(goal));
            _animator.SetIKHintPositionWeight(hint, 1f);
            _animator.SetIKHintPosition(hint, position + (_transform.forward + Vector3.up) * scale * 0.5f);
        }

        private void MoveBody(float crouch, float scale)
        {
            _animator.bodyPosition += Vector3.down * (_crouchDepth * scale * crouch);
            _animator.bodyRotation = Quaternion.AngleAxis(_bendAngle * crouch, _transform.right) * _animator.bodyRotation;
        }

        private void ReachHand(float scale)
        {
            Vector3 target = _phase == Phase.Lift || _phase == Phase.Up ? GetChestPoint(scale) : GetGroundPoint(scale);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, GetHandWeight());
            _animator.SetIKPosition(AvatarIKGoal.RightHand, target);
        }

        private Vector3 GetGroundPoint(float scale)
        {
            Vector3 shoulder = _animator.bodyPosition;
            Vector3 offset = _targetPoint - shoulder;
            offset.z = 0f;
            return shoulder + Vector3.ClampMagnitude(offset, _armReach * scale * 1.4f);
        }

        private Vector3 GetChestPoint(float scale)
        {
            Vector3 chest = _chest != null ? _chest.position : _animator.bodyPosition;
            return chest + _transform.forward * (0.28f * scale) + Vector3.up * (0.05f * scale);
        }

        private void Look()
        {
            _animator.SetLookAtWeight(GetCrouchWeight(), 0.3f, 0.8f, 1f, 0.5f);
            _animator.SetLookAtPosition(_rightHand != null ? _rightHand.position : _targetPoint);
        }

        private void ClearWeights()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            _animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
            _animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
            _animator.SetLookAtWeight(0f);
        }
    }
}
