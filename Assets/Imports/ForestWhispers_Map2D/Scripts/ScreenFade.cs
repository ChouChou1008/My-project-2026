using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForestWhispers.Map2D
{
    /// <summary>
    /// 맵 전환용 페이드. 렌더 파이프라인과 상관없이 동작하도록 OnGUI로 그린다.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float _duration = 0.35f;

        private static ScreenFade _instance;
        private float _alpha;
        private bool _isLoading;

        public static void LoadScene(string sceneName)
        {
            GetInstance().StartLoading(sceneName);
        }

        private static ScreenFade GetInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var holder = new GameObject("ScreenFade");
            DontDestroyOnLoad(holder);
            _instance = holder.AddComponent<ScreenFade>();
            return _instance;
        }

        private void StartLoading(string sceneName)
        {
            if (_isLoading)
            {
                return;
            }

            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            _isLoading = true;
            yield return Fade(0f, 1f);
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Fade(1f, 0f);
            _isLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _alpha = Mathf.Lerp(from, to, elapsed / _duration);
                yield return null;
            }

            _alpha = to;
        }

        private void OnGUI()
        {
            if (_alpha <= 0f)
            {
                return;
            }

            GUI.color = new Color(0f, 0f, 0f, _alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        }
    }
}
