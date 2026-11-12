using UnityEngine;

namespace ForestWhispers.Map2D
{
    /// <summary>폭포, 굴뚝 연기, 깃발, 나비 같은 프레임 애니메이션 재생.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [Header("Flipbook Settings")]
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 12f;

        private SpriteRenderer _renderer;
        private float _startOffset;
        private int _lastFrame = -1;

        public void Setup(Sprite[] frames, float framesPerSecond)
        {
            _frames = frames;
            _framesPerSecond = framesPerSecond;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void Start()
        {
            _startOffset = Random.value * 10f;
        }

        private void Update()
        {
            ShowFrame();
        }

        private void CacheComponent()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void ShowFrame()
        {
            if (_frames == null || _frames.Length == 0)
            {
                return;
            }

            int frame = (int)((Time.time + _startOffset) * _framesPerSecond) % _frames.Length;
            if (frame == _lastFrame)
            {
                return;
            }

            _lastFrame = frame;
            _renderer.sprite = _frames[frame];
        }
    }
}
