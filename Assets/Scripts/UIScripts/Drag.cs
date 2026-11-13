using UnityEngine;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Transform _dragLayer;          

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent;                    

    public bool IsDropped { get; set; }                    

    private void Awake()
    {
        CacheComponent();
    }

    private void CacheComponent()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        IsDropped = false;

        transform.SetParent(_dragLayer);
        transform.SetAsLastSibling();
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.position = eventData.position;        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        if (IsDropped)
        {
            return;                                          
        }

        MoveTo(_originalParent);                             
    }

    public void MoveTo(Transform slot)
    {
        transform.SetParent(slot);
        _rectTransform.anchoredPosition = Vector2.zero;      
    }

    public Transform OriginalParent => _originalParent;
}