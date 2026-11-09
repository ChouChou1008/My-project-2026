using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [field: SerializeField]  public int MaxSize { get;private set; } = 20;
    [SerializeField] private CheckItem  _checkItem;
    
    private List<ItemData> _items;
    private int _count;
    
    public event Action OnTryAdd;
    
    private void Awake()
    {
        _items = new List<ItemData>(MaxSize);
    }
    
    public bool TryAdd(ItemData item) 
    {
        if ((_items == null) || (_items.Count >= MaxSize))
        {
            return false;
        }
        _items.Add(item);
        OnTryAdd?.Invoke();
        return true;
    }


}
