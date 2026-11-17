using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [field: SerializeField]  public int MaxSize { get; private set; } = 20;
    [SerializeField] private Slot[] _slots;
    [SerializeField] private Slot[] _storeSlots;

    private readonly Dictionary<ItemData, int> _items = new Dictionary<ItemData, int>();
    private  List<ItemData> _order = new List<ItemData>();
    private int  _totalCount = 0;
    
    public bool IsFull => _totalCount >= MaxSize;
    
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

    public void RefreshSlots()
    {
        DrawSlots(_slots);
        DrawSlots(_storeSlots);
    }

    private void DrawSlots(Slot[] _slot)
    {
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
        Debug.Log("Inventory 정리");
        RefreshSlots();
        return true;
    }
    public void SwapOrder(int from, int to)
    {
        if (from == to ||
            from < 0 ||
            to < 0 ||
            from >= _order.Count)                 // 빈 칸을 끌어왔으면 무시
        {
            return;
        }

        if (to >= _order.Count)                   // 빈 칸에 놓음 → 맨 뒤로
        {
            ItemData item = _order[from];
            _order.RemoveAt(from);
            _order.Add(item);
        }
        else                                      // 아이템끼리 → 자리 교환
        {
            (_order[from], _order[to]) = (_order[to], _order[from]);
        }

        RefreshSlots();                           // 가방 창 + 상점 가방 다시 그리기
    }
    
}
