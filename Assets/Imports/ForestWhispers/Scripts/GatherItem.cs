using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 채집할 수 있는 약초, 버섯 등. 활성 목록에 스스로 등록하므로 Collider가 필요 없다.
    /// </summary>
    public class GatherItem : MonoBehaviour
    {
        public static readonly List<GatherItem> ActiveItems = new List<GatherItem>();

        [Header("Item Settings")]
        [SerializeField] private string _itemId = "herb";
        [SerializeField] private int _amount = 1;

        [Header("Effect Settings")]
        [SerializeField] private SpriteRenderer _highlight;
        [SerializeField] private float _flySpeed = 9f;
        [SerializeField] private float _vanishTime = 0.3f;

        private Transform _transform;
        private Transform _followTarget;
        private Vector3 _baseScale;
        private bool _isPicked;

        public string ItemId => _itemId;
        public int Amount => _amount;

        public void Setup(string itemId, int amount, SpriteRenderer highlight)
        {
            _itemId = itemId;
            _amount = amount;
            _highlight = highlight;
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (_highlight == null || _isPicked)
            {
                return;
            }

            _highlight.enabled = isHighlighted;
        }

        public void PickUp(Transform followTarget)
        {
            _isPicked = true;
            _followTarget = followTarget;
            _transform.SetParent(null, true);
            ActiveItems.Remove(this);
            SetHighlightVisible(false);
        }

        public void Vanish()
        {
            StartCoroutine(VanishRoutine());
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            ResetItem();
        }

        private void OnEnable()
        {
            ActiveItems.Add(this);
        }

        private void OnDisable()
        {
            ActiveItems.Remove(this);
            
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        private void CacheComponent()
        {
            _transform = transform;
        }

        private void ResetItem()
        {
            _baseScale = _transform.localScale;
            _isPicked = false;
            SetHighlightVisible(false);
        }

        private void SetHighlightVisible(bool isVisible)
        {
            if (_highlight == null)
            {
                return;
            }

            _highlight.enabled = isVisible;
        }

        private void FollowTarget()
        {
            if (_followTarget == null)
            {
                return;
            }

            _transform.position = Vector3.MoveTowards(_transform.position, _followTarget.position, _flySpeed * Time.deltaTime);
        }

        private IEnumerator VanishRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _vanishTime)
            {
                elapsed += Time.deltaTime;
                _transform.localScale = _baseScale * (1f - elapsed / _vanishTime);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
