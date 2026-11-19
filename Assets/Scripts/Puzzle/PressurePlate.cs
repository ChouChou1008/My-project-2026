using UnityEngine;

/// <summary>
/// 박스가 올라가 있으면 눌리는 발판.
/// 트리거 대신 매 물리 프레임 OverlapBox로 확인해서, 박스가 잠겨(정지) 있어도 정확하게 감지한다.
/// </summary>
public class PressurePlate : PuzzleCondition
{
    [SerializeField] private LayerMask _detectLayer;                         // Box 레이어
    [SerializeField] private Vector2 _checkSize = new Vector2(0.6f, 0.6f);
    [SerializeField] private Vector2 _checkOffset = new Vector2(0f, 0.5f);   // 발판 위 박스 중심 높이
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Light _light;
    [SerializeField] private Color _offColor = new Color(0.25f, 0.5f, 0.45f);
    [SerializeField] private Color _onColor = new Color(0.5f, 1.6f, 1.2f);

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Collider2D[] _hits = new Collider2D[4];
    private ContactFilter2D _filter;
    private Material _material;
    private bool _isPressed;

    public override bool IsSolved => _isPressed;

    public void Setup(LayerMask detectLayer, Renderer plateRenderer, Light plateLight)
    {
        _detectLayer = detectLayer;
        _renderer = plateRenderer;
        _light = plateLight;
    }

    private void Awake()
    {
        _filter.useTriggers = false;
        _filter.SetLayerMask(_detectLayer);

        if (_renderer != null)
        {
            _material = _renderer.material;                                  // 발판마다 따로 색을 바꾸기 위한 복사본
        }

        ApplyVisual();
    }

    private void FixedUpdate()
    {
        Vector2 center = (Vector2)transform.position + _checkOffset;
        bool pressed = Physics2D.OverlapBox(center, _checkSize, 0f, _filter, _hits) > 0;

        if (pressed == _isPressed)
        {
            return;
        }

        _isPressed = pressed;
        ApplyVisual();
        NotifyChanged();
    }

    private void ApplyVisual()
    {
        if (_material != null)
        {
            _material.SetColor(EmissionColorId, _isPressed ? _onColor : _offColor);
        }

        if (_light != null)
        {
            _light.enabled = _isPressed;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isPressed ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position + (Vector3)_checkOffset, _checkSize);
    }
}
