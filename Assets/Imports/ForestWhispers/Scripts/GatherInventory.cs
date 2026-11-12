using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>채집한 아이템 개수를 맵 이동 중에도 유지한다. UI는 Changed 이벤트로 갱신하면 된다.</summary>
    public static class GatherInventory
    {
        private static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public static event Action<string, int> Changed;

        public static int GetCount(string itemId)
        {
            return _counts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public static void Add(string itemId, int amount)
        {
            int total = GetCount(itemId) + amount;
            _counts[itemId] = total;
            Debug.Log("채집: " + itemId + " x" + total);
            Changed?.Invoke(itemId, total);
        }

        public static void Reset()
        {
            _counts.Clear();
        }
    }
}
