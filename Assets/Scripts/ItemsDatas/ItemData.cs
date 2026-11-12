using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemName;
    [SerializeField] private Sprite _itemSprite;
    [SerializeField] private ItemGrade _itemGrade;
    [SerializeField] private int _itemPrice;
    
    public string ItemName => _itemName;
    public Sprite ItemSprite => _itemSprite;
    public ItemGrade Grade => _itemGrade;
    public int ItemPrice => _itemPrice;

    public enum ItemGrade
    {
        Common,
        Rare,
        Epic,
        Legendary,
    }
    
}
