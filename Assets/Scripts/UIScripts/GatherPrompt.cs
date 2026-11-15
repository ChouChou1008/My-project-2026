using TMPro;
using UnityEngine;

public class GatherPrompt : MonoBehaviour
{
    [Header("Prompt Settings")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.3f, -0.1f);

    [Header("Bob Settings")]
    [SerializeField] private float _bobSpeed = 2.5f;
    [SerializeField] private float _bobHeight = 0.05f;

    [Header("Fade Settings")]
    [SerializeField] private float _fadeSpeed = 4f;

    private Transform _transform;
    private CanvasGroup _canvasGroup;

    private ItemDetector _target;          // 지금 안내 중인 아이템 (null = 없음)
    private float _topHeight;                // 아이템 중심에서 꼭대기까지 높이
    private float _targetAlpha;              // 0 = 숨김, 1 = 보임

    private void Awake()
    {
        CacheComponent();
    }

    private void Start()
    {
        ResetPrompt();
    }

    private void OnEnable()
    {
        ItemDetector.OnCheckGather += OnItemCheckGather;
    }

    private void OnDisable()
    {
        ItemDetector.OnCheckGather -= OnItemCheckGather;
    }

    private void LateUpdate()
    {
        FollowTarget();
        UpdateFade();
    }

    private void CacheComponent()
    {
        _transform = transform;
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void ResetPrompt()
    {
        _target = null;
        _targetAlpha = 0f;
        _canvasGroup.alpha = 0f;
    }

    private void OnItemCheckGather(ItemDetector detector)
    {
        if (detector.IsCheckGather)
        {
            Show(detector);
            return;
        }

        if (detector != _target)             // 다른 아이템에서 나간 거면 무시
        {
            return;
        }

        Hide();
    }

    public void Show(ItemDetector detector)
    {
        Item item = detector.GetComponent<Item>();
        if (item == null ||
            item.Data == null)
        {
            return;
        }

        _nameText.text = item.Data.ItemName;
        _target = detector;
        _topHeight = GetTopHeight(detector.transform);
        _targetAlpha = 1f;
    }

    public void Hide()
    {
        _target = null;                      // 위치는 그대로 두고 사라지기만
        _targetAlpha = 0f;
    }

    private void FollowTarget()
    {
        if (_target == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        Vector3 top = _target.transform.position + Vector3.up * _topHeight;

        _transform.position = top + _offset + Vector3.up * bob;
    }

    private void UpdateFade()
    {
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
    }

    private float GetTopHeight(Transform target)
    {
        Renderer itemRenderer = target.GetComponentInChildren<Renderer>();
        if (itemRenderer == null)
        {
            return 0f;
        }

        return itemRenderer.bounds.max.y - target.position.y;
    }
}