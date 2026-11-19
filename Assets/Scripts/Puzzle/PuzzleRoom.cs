using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 퍼즐 맵 하나의 관리자.
/// - 플레이어가 이 맵 안에 있을 때 R 키로 박스를 처음 자리로 되돌린다(박스가 구석에 끼었을 때).
/// - 플레이어가 맵 아래로 떨어지면 시작 지점으로 돌려보낸다.
/// - 맵에 처음 들어오면 맵 이름을 잠깐 보여 준다.
/// </summary>
public class PuzzleRoom : MonoBehaviour
{
    [SerializeField] private string _title;
    [SerializeField] private float _minX;
    [SerializeField] private float _maxX;
    [SerializeField] private float _killY = -5f;
    [SerializeField] private Transform _spawn;
    [SerializeField] private PushableBox2D[] _boxes;
    [SerializeField] private float _titleTime = 3f;

    private Vector3[] _boxStartPositions;
    private Transform _player;
    private Rigidbody2D _playerBody;
    private bool _isInside;
    private float _titleTimer;

    public void Setup(string title, float minX, float maxX, float killY, Transform spawn, PushableBox2D[] boxes)
    {
        _title = title;
        _minX = minX;
        _maxX = maxX;
        _killY = killY;
        _spawn = spawn;
        _boxes = boxes;
    }

    private void Awake()
    {
        _boxStartPositions = new Vector3[_boxes.Length];
        for (int i = 0; i < _boxes.Length; i++)
        {
            _boxStartPositions[i] = _boxes[i].transform.position;
        }
    }

    private void Start()
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller == null)
        {
            Debug.LogError("[PuzzleRoom] 씬에서 PlayerController를 찾지 못했어요", this);
            enabled = false;
            return;
        }

        _player = controller.transform;
        _playerBody = controller.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        bool inside = _player.position.x >= _minX && _player.position.x <= _maxX;
        if (inside && !_isInside)
        {
            _titleTimer = _titleTime;                         // 맵에 들어온 순간
        }

        _isInside = inside;
        if (!_isInside)
        {
            return;
        }

        _titleTimer -= Time.deltaTime;

        if (_player.position.y < _killY)
        {
            Respawn();
        }

        if (_boxes.Length > 0 &&
            Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetBoxes();
        }
    }

    private void Respawn()
    {
        _playerBody.linearVelocity = Vector2.zero;
        _playerBody.position = _spawn.position;
        _player.position = _spawn.position;
    }

    private void ResetBoxes()
    {
        for (int i = 0; i < _boxes.Length; i++)
        {
            Rigidbody2D body = _boxes[i].GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.position = _boxStartPositions[i];
            _boxes[i].transform.position = _boxStartPositions[i];
        }
    }

    private void OnGUI()
    {
        if (!_isInside)
        {
            return;
        }

        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.025f) };

        if (_boxes.Length > 0)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(16f, Screen.height - 48f, 400f, 40f), "R : 박스 처음 자리로", style);
        }

        if (_titleTimer <= 0f)
        {
            return;
        }

        var titleStyle = new GUIStyle(style)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = Mathf.RoundToInt(Screen.height * 0.045f)
        };
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_titleTimer));
        GUI.Label(new Rect(0f, Screen.height * 0.12f, Screen.width, 80f), _title, titleStyle);
    }
}
