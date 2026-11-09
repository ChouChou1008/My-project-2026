using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemData _itemData;
    
    public ItemData itemData => _itemData;

    private void Start()
    {
       // ItemData(_itemData.name,_itemData.sprite);
    }
}
