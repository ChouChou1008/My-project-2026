using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace ForestWhispers.Game
{
    /// <summary>
    /// 가까운 채집물을 찾아 표시하고, E 키(또는 PressGather)로 채집한다.
    /// 채집 중에는 이동 스크립트를 꺼서 움직이지 못하게 한다. Player 루트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerGather : MonoBehaviour
    {
        [Header("Gather Settings")]
        [SerializeField] private float _gatherRadius = 1.5f;
        [SerializeField] private float _fallbackDuration = 0.8f;
        [SerializeField] private float _maxVerticalSpeed = 0.5f;

        [Header("Reference Settings")]
        [SerializeField] private MonoBehaviour _movement;
        [SerializeField] private float _rightYaw = 90f;

        private Transform _transform;
        private Rigidbody _rigidbody;
        private GatherPose _pose;
        private GatherItem _nearestItem;
        private bool _isGathering;
        private bool _isGatherPressed;

        public bool IsGathering => _isGathering;

        public void Setup(MonoBehaviour movement)
        {
            _movement = movement;
        }

        public void PressGather()
        {
            _isGatherPressed = true;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Update()
        {
            ReadInput();
            FindNearestItem();
            TryGather();
        }

        private void CacheComponent()
        {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            _pose = GetComponentInChildren<GatherPose>();
        }

        private void ReadInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            _isGatherPressed |= Input.GetKeyDown(KeyCode.E);
#elif ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            _isGatherPressed |= keyboard != null && keyboard.eKey.wasPressedThisFrame;
#endif
        }

        private void FindNearestItem()
        {
            GatherItem nearest = null;
            float bestDistance = _gatherRadius;
            foreach (GatherItem item in GatherItem.ActiveItems)
            {
                float distance = Vector2.Distance(item.transform.position, _transform.position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                nearest = item;
            }

            if (nearest == _nearestItem)
            {
                return;
            }

            if (_nearestItem != null)
            {
                _nearestItem.SetHighlighted(false);
            }

            _nearestItem = nearest;
            if (_nearestItem != null)
            {
                _nearestItem.SetHighlighted(true);
            }
        }

        private void TryGather()
        {
            bool isPressed = _isGatherPressed;
            _isGatherPressed = false;
            if (!isPressed ||
                _isGathering ||
                _nearestItem == null ||
                !IsStanding())
            {
                return;
            }

            StartCoroutine(Gather(_nearestItem));
        }

        private bool IsStanding()
        {
            return Mathf.Abs(GetVelocity().y) < _maxVerticalSpeed;
        }

        private IEnumerator Gather(GatherItem item)
        {
            SetLocked(true);
            FaceItem(item);

            bool hasPose = _pose != null && _pose.isActiveAndEnabled;
            float pickDelay = hasPose ? _pose.PickDelay : _fallbackDuration * 0.5f;
            float remaining = (hasPose ? _pose.TotalTime : _fallbackDuration) - pickDelay;
            if (hasPose)
            {
                _pose.Play(item.transform.position);
            }

            yield return new WaitForSeconds(pickDelay);
            item.PickUp(hasPose && _pose.RightHand != null ? _pose.RightHand : _transform);
            yield return new WaitForSeconds(remaining * 0.6f);
            GatherInventory.Add(item.ItemId, item.Amount);
            item.Vanish();
            yield return new WaitForSeconds(remaining * 0.4f);

            SetLocked(false);
        }

        private void SetLocked(bool isLocked)
        {
            _isGathering = isLocked;

            if (isLocked)
            {
                SetVelocity(Vector3.zero);      // 미끄러지지 않게 먼저 멈춤
            }

            _rigidbody.isKinematic = isLocked;  // 3D에는 simulated가 없어 isKinematic으로 물리 정지

            if (_movement != null)
            {
                _movement.enabled = !isLocked;
            }
        }

        private void FaceItem(GatherItem item)
        {
            // PlayerMove가 루트를 회전시키므로 채집도 루트를 회전 (모델만 돌리면 이후 방향이 어긋남)
            float direction = item.transform.position.x >= _transform.position.x ? 1f : -1f;
            _transform.rotation = Quaternion.Euler(0f, _rightYaw * direction, 0f);
        }

        // Unity 6부터 Rigidbody.velocity → linearVelocity 로 이름 변경
        private Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rigidbody.linearVelocity;
#else
            return _rigidbody.velocity;
#endif
        }

        private void SetVelocity(Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = velocity;
#else
            _rigidbody.velocity = velocity;
#endif
        }
    }
}