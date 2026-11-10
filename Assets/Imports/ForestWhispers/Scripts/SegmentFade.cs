using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 배경판 깊이에 붙은 먼 폭포처럼, 이웃 구간의 배경 물체가 다른 구간의 배경판 띠 위로 겹쳐 보이지 않게
    /// 카메라가 자기 구간 쪽으로 올 때만 서서히 나타나게 한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SegmentFade : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float _centerX;
        [SerializeField] private float _segment = 30f;

        private SpriteRenderer _renderer;
        private Transform _cameraTransform;
        private float _baseAlpha = 1f;

        public void Setup(float centerX, float segment)
        {
            _centerX = centerX;
            _segment = segment;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void LateUpdate()
        {
            UpdateFade();
        }

        private void CacheComponent()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseAlpha = _renderer.color.a;
            Camera main = Camera.main;
            _cameraTransform = main != null ? main.transform : null;
        }

        private void UpdateFade()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            // 구간 경계(중앙에서 반 구간)까지는 그대로, 이웃 구간 중앙에 가까워질수록 사라짐
            float half = _segment * 0.5f;
            float distance = Mathf.Abs(_cameraTransform.position.x - _centerX);
            float visible = Mathf.Clamp01(1f - (distance - half) / half);
            Color color = _renderer.color;
            color.a = _baseAlpha * visible;
            _renderer.color = color;
        }
    }
}
