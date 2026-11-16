using UnityEngine;

[CreateAssetMenu(fileName = "ResultData", menuName = "ResultData")]
public class ResultItemData : ScriptableObject 
{
   [SerializeField] private ItemData _resultItem;
   [SerializeField] private string _description;
   [SerializeField] private int _sellPrice;
   [SerializeField] private bool _sellable;
   [SerializeField] private Ingredient[]  _ingredients;
   
   
   public ItemData ResultItem => _resultItem;
   public string Description => _description;
   public int SellPrice => _sellPrice;
   public bool Sellable => _sellable;
   public Ingredient[] Ingredients => _ingredients;
   
   [System.Serializable]
   public class Ingredient
   {
      [SerializeField] private ItemData _item;
      [SerializeField] private int _count = 1;

      public ItemData Item => _item;
      public int Count => _count;
   }
}
