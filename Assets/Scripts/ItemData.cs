using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData: ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private Sprite _sprite;
    
    
}