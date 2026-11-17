using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [field: SerializeField]  public int MaxSize { get; private set; } = 20;
    [SerializeField] private Slot[] _slots;
    [SerializeField] private Slot[] _storeSlots;
    
    private Dictionary<ItemData,int> _items;
    private  List<ItemData> _order = new List<ItemData>();
    private int  _totalCount = 0;
    private bool _isSell;
    public bool IsFull => _totalCount >= MaxSize;
    

    private void Awake()
    {
        _items = new Dictionary<ItemData,int>(MaxSize);
    }

    private void Start()
    {
        RefreshSlots();
    }
    
    
    public bool TryAdd(ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        if (_totalCount >= MaxSize)                         // 총 개수 기준으로 먼저 확인
        {
            Debug.LogWarning("Inventory is full");
            return false;
        }

        if (_items.TryGetValue(item, out int count))        // 이미 있는 종류 → 개수 +1
        {
            _items[item] = count + 1;
        }
        else                                                // 새 종류 → 칸 추가
        {
            _items.Add(item, 1);
            _order.Add(item);
        }

        _totalCount++;
        CheckGather(_items);
        RefreshSlots();
        return true;
    }

    private void RefreshSlots()
    {
        DrawSlots(_slots);
        DrawSlots(_storeSlots);
    }

    private void DrawSlots(Slot[] _slot)
    {
        if (_slot == null)          // Inspector에서 비워 둔 경우
        {
            return;
        }
        for (int i = 0; i < _slot.Length; i++)
        {
            if (i >= _order.Count)                            // 아이템이 없는 칸
            {
                _slot[i].Clear();
                continue;
            }

            ItemData item = _order[i];                        // i번째로 주운 아이템
            _slot[i].SetItem(item, _items[item]);            // 개수는 Dictionary에서
        }
    }

    private void CheckGather(Dictionary<ItemData,int> item)
    {
        foreach(KeyValuePair<ItemData,int> pair in item)
        {
            Debug.Log($"{pair.Key.ItemName} x{pair.Value}");   
        }
    }

    public int GetCount(ItemData item)
    {
        if (_items.TryGetValue(item, out int count))
        {
            return count;
        }

        return 0;
    }

    public bool TryRemove(ItemData item, int count)
    {
        if (GetCount(item) < count)
        {
            return false;
        }
        _items[item] -=  count;
        _totalCount -=  count;

        if (_items[item] <= 0)
        {
            _items.Remove(item);
            _order.Remove(item);
        }
        RefreshSlots();
        return true;
    }
    
    
}
