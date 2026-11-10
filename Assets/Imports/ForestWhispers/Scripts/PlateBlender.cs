using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 구간마다 놓인 배경판이 겹치는 띠를 카메라 위치에 맞춰 섞는다.
    /// 카메라가 구간 중앙에 있으면 그 구간 배경판이 화면 전체에 그대로(레퍼런스 그림과 같게) 보이고,
    /// 다음 구간 중앙으로 갈수록 띠가 다음 배경판으로 넘어간다.
    /// </summary>
    public class PlateBlender : MonoBehaviour
    {
        private static readonly int BlendId = Shader.PropertyToID("_Blend");

        [Header("Blend Settings")]
        [SerializeField] private Renderer[] _strips;
        [SerializeField] private float[] _seamX;
        [SerializeField] private float _segment = 30f;

        private Transform _cameraTransform;
        private MaterialPropertyBlock _block;
        private float _lastCameraX = float.NaN;

        public void Setup(Renderer[] strips, float[] seamX, float segment)
        {
            _strips = strips;
            _seamX = seamX;
            _segment = segment;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void LateUpdate()
        {
            UpdateBlend();
        }

        private void CacheComponent()
        {
            _block = new MaterialPropertyBlock();
            Camera main = Camera.main;
            _cameraTransform = main != null ? main.transform : null;
        }

        private void UpdateBlend()
        {
            if (_cameraTransform == null || _strips == null)
            {
                return;
            }

            float cameraX = _cameraTransform.position.x;
            if (Mathf.Approximately(cameraX, _lastCameraX))
            {
                return;
            }

            _lastCameraX = cameraX;
            for (int i = 0; i < _strips.Length; i++)
            {
                if (_strips[i] == null)
                {
                    continue;
                }

                // 왼쪽 구간 중앙(0) → 오른쪽 구간 중앙(1)
                float weight = Mathf.Clamp01((cameraX - (_seamX[i] - _segment * 0.5f)) / _segment);
                _strips[i].GetPropertyBlock(_block);
                _block.SetFloat(BlendId, weight);
                _strips[i].SetPropertyBlock(_block);
            }
        }
    }
}
