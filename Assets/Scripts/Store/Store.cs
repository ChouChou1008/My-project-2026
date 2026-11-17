using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class Store : MonoBehaviour
{
    [Header("인벤토리")]
    [SerializeField] private Inventory _inventory;
    
    [Header("첫 번째 제작템")]
    [SerializeField] private ResultItemData _firstItem;
    [SerializeField] private TMP_Text _firstItemText;
    [SerializeField] private TMP_Text _firstItemPrice;
    [SerializeField] private TMP_Text _firstNeedItemText;
    
    [Header("두 번째 제작템")]
    [SerializeField] private ResultItemData _secondItem;
    [SerializeField] private TMP_Text _secondItemText;
    [SerializeField] private TMP_Text _secondItemPrice;
    [SerializeField] private TMP_Text _secondNeedItemText;
    
    [Header("세 번째 제작템")]
    [SerializeField] private ResultItemData _thirdItem;
    [SerializeField] private TMP_Text _thirdItemText;
    [SerializeField] private TMP_Text _thirdItemPrice;
    [SerializeField] private TMP_Text _thirdNeedItemText;


    private void Update()
    {
        SlotUpdate();
    }

    public void SlotUpdate()
    {
        _firstItemText.text = _firstItem.Description;    
        _firstItemPrice.text = _firstItem.SellPrice.ToString();
        _firstNeedItemText.text = GetNeedText(_firstItem); 
        
        _secondItemText.text = _secondItem.Description;
        _secondItemPrice.text = _secondItem.SellPrice.ToString();
        _secondNeedItemText.text = GetNeedText(_secondItem);
        
        _thirdItemText.text = _thirdItem.Description;
        _thirdItemPrice.text = _thirdItem.SellPrice.ToString();
        _thirdNeedItemText.text = GetNeedText(_thirdItem);
        
    }

    private string GetNeedText(ResultItemData recipe)
    {
        string text = "";

        foreach (ResultItemData.Ingredient ingredient in recipe.Ingredients)
        {
            int have = _inventory.GetCount(ingredient.Item);      // 가진 수
            int need = ingredient.Count;                          // 필요한 수

            text += $"{ingredient.Item.ItemName} {have}/{need}  ";
        }

        return text.Trim();                                       // 끝 공백 제거
    }
    

}
