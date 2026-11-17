using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class Store : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ResultItemData _firstItem;
    [SerializeField] private TMP_Text _firstItemText;
    [SerializeField] private TMP_Text _firstItemPrice;
    [SerializeField] private TMP_Text _needItemText;

    private void Awake() => SlotUpdate();
    
    public void SlotUpdate()
    {
        _firstItemText.text = _firstItem.Description;    
        _firstItemPrice.text = _firstItem.SellPrice.ToString();
        _needItemText.text = GetNeedText(_firstItem);             // "약초 0/3"
    }

    private string GetNeedText(ResultItemData recipe)
    {
        string text = "";

        foreach (ResultItemData.Ingredient ingredient in recipe.Ingredients)
        {
            int have = _inventory.GetCount(ingredient.Item);      // 가진 수
            int need = ingredient.Count;                          // 필요한 수

            text += $"{ingredient.Item.ItemName} {have}/{need}  ";
            Debug.Log($"{text}");
        }

        return text.Trim();                                       // 끝 공백 제거
    }
    

}
