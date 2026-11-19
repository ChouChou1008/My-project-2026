using UnityEngine;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform _inventory;          // InventoryPanel
    private Transform _itemListTr;         // Bag
    private CanvasGroup _canvasGroup;
    private GameObject _placeholder;
    private Inventory _inventoryData;

    private int _originalIndex;
    private bool _isDropped;               // Drop이 받아 줬는지

    private void Awake()
    {
        CacheComponent();
    }

    private void CacheComponent()
    {
        _inventory = GameObject.Find("InventoryPanel").transform;
        _itemListTr = GameObject.Find("Bag").transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _inventoryData = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalIndex = transform.GetSiblingIndex();  // Drag한 순서 기억
        _isDropped = false;

        transform.SetParent(_inventory);
        transform.SetAsLastSibling();
        _canvasGroup.blocksRaycasts = false;
        CreatePlaceholder();  
    }

    private void CreatePlaceholder()
    {
        _placeholder = new GameObject("Placeholder", typeof(RectTransform));
        _placeholder.transform.SetParent(_itemListTr, false);
        _placeholder.transform.SetSiblingIndex(_originalIndex); 
    }
    

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        if (_isDropped)                      // Drop이 이미 처리함
        {
            return;
        }

        ReturnToBag();                       // 칸 밖에 놓음 → 원래 자리
    }

    public void SwapWith(Transform target)        // Drop이 불러 줌
    {
        ReturnToBag();                            // Slot은 무조건 원래 자리로

        if (_inventoryData == null)
        {
            Debug.LogError("[Drag] 씬에서 Inventory를 찾지 못했어요", this);
            return;
        }

        int targetIndex = target.GetSiblingIndex();
        _inventoryData.SwapOrder(_originalIndex, targetIndex);   // 데이터만 교환 → RefreshSlots로 다시 그리기

        _isDropped = true;
    }

    private void ReturnToBag()
    {
        if (_placeholder != null)
        {
            _placeholder.transform.SetParent(null);   // Bag에서 먼저 빼고 (순서 계산에서 제외)
            Destroy(_placeholder);
            _placeholder = null;
        }
        transform.SetParent(_itemListTr);
        transform.SetSiblingIndex(_originalIndex);
    }
}