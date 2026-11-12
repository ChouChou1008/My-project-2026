using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemData _itemData;
    
    public ItemData Data => _itemData;
}
