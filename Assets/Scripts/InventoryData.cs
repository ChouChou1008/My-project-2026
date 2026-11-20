using System.Collections.Generic;
using UnityEngine;


public class InventoryData
{
    public readonly Dictionary<ItemData, int> Items = new Dictionary<ItemData, int>();
    public  List<ItemData> Order = new List<ItemData>();
    public int TotalCount;
}
