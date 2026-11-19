using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 맵 이동용 화면 페이드. 어두워진 순간에 플레이어를 옮기고 다시 밝힌다.
/// OnGUI로 그려서 Canvas 설정과 상관없이 동작한다.
/// </summary>
public class PuzzleFader : MonoBehaviour
{
    private const float FadeDuration = 0.4f;

    private static PuzzleFader _instance;

    private float _alpha;
    private string _message;
    private bool _isBusy;

    public static bool IsBusy => _instance != null && _instance._isBusy;

    public static void FadeAndRun(Action onDark)
    {
        GetInstance().Begin(onDark, null);
    }

    public static void FadeToMessage(string message)
    {
        GetInstance().Begin(null, message);
    }

    private static PuzzleFader GetInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var holder = new GameObject("PuzzleFader");
        _instance = holder.AddComponent<PuzzleFader>();
        return _instance;
    }

    private void Begin(Action onDark, string message)
    {
        if (_isBusy)
        {
            return;
        }

        StartCoroutine(FadeRoutine(onDark, message));
    }

    private IEnumerator FadeRoutine(Action onDark, string message)
    {
        _isBusy = true;
        yield return Fade(0f, 1f);

        onDark?.Invoke();

        if (!string.IsNullOrEmpty(message))
        {
            _message = message;                  // 마지막 출구: 검은 화면에 문구를 띄우고 끝
            yield break;
        }

        yield return new WaitForSeconds(0.15f);
        yield return Fade(1f, 0f);
        _isBusy = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _alpha = Mathf.Lerp(from, to, elapsed / FadeDuration);
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

        if (string.IsNullOrEmpty(_message))
        {
            return;
        }

        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Screen.height * 0.05f)
        };
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), _message, style);
    }
}
