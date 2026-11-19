using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;


public class Store : MonoBehaviour
{
    [Header("인벤토리")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Coin _coin;
    
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

    [Header("제작 & 팔기 버튼")]
    [SerializeField] private Button _sellButton;
    [SerializeField] private GameObject _sellGameObject;
    [SerializeField] private Button _craftButton;
    [SerializeField] private GameObject _craftGameObject;
    
    [Header("첫 번째 판매템")]
    [SerializeField] private TMP_Text _firstSellItemText;
    [SerializeField] private TMP_Text _firstSellItemPrice;
    
    [Header("두 번째 판매템")]
    [SerializeField] private TMP_Text _secondSellItemText;
    [SerializeField] private TMP_Text _secondSellItemPrice;
    
    [Header("세 번째 판매템")]
    [SerializeField] private TMP_Text _thirdSellItemText;
    [SerializeField] private TMP_Text _thirdSellItemPrice;

    private void OnEnable()
    {
        SlotUpdate();
    }

    private void Start()
    {
        if (_coin != null && GameManager.Instance != null)
        {
            _coin = GameManager.Instance.coin;
        }
    }

    public void SlotUpdate()
    {
        _firstItemText.text = _firstItem.Description; 
        _firstSellItemText.text = _firstItem.Description;  
        _firstItemPrice.text = _firstItem.SellPrice.ToString();
        _firstSellItemPrice.text = _firstItem.SellPrice.ToString();
        _firstNeedItemText.text = GetNeedText(_firstItem); 
        
        _secondItemText.text = _secondItem.Description;
        _secondSellItemText.text = _secondItem.Description;
        _secondItemPrice.text = _secondItem.SellPrice.ToString();
        _secondSellItemPrice.text = _secondItem.SellPrice.ToString();
        _secondNeedItemText.text = GetNeedText(_secondItem);
        
        _thirdItemText.text = _thirdItem.Description;
        _thirdSellItemText.text  = _thirdItem.Description;
        _thirdItemPrice.text = _thirdItem.SellPrice.ToString();
        _thirdSellItemPrice.text = _thirdItem.SellPrice.ToString();
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

    public void SellButton()
    {
        Color color = _craftButton.image.color;
        color.a = 0;
        _craftButton.image.color = color;
        color.a = 1;
        _sellButton.image.color = color;
        _craftGameObject.SetActive(false);
        _sellGameObject.SetActive(true);
    }

    public void CreaftButton()
    {
        Color color = _sellButton.image.color;
        color.a = 0;
        _sellButton.image.color = color;
        color.a = 1;
        _craftButton.image.color = color;
        _sellGameObject.SetActive(false);
        _craftGameObject.SetActive(true);
    }
    
    public void CraftFirstItem()
    {
        Creat(_firstItem);
    }

    public void CraftSecondItem()
    {
        Creat(_secondItem);
    }

    public void CraftThirdItem()
    {
        Creat(_thirdItem);
    }

    private void Creat(ResultItemData item)
    {
        foreach (ResultItemData.Ingredient ingredient in item.Ingredients)
        {
            if (_inventory.TryRemove(ingredient.Item, ingredient.Count))
            {
                _inventory.TryAdd(item.ResultItem);
                SlotUpdate(); 
            }
            else
            {
                Debug.Log("재료 부족");
            }
        }
    }
    private void Sell(ResultItemData recipe)
    {
        if (!_inventory.TryRemove(recipe.ResultItem, 1))   // 만든 약 1개 빼기
        {
            Debug.Log("아이템 없음");
            return;
        }

        _coin.Add(recipe.SellPrice);                         // 돈 더하기
        SlotUpdate();
        _inventory.RefreshSlots();
    }

    public void SellFirstItem()
    {
        Sell(_firstItem);
    }

    public void SellSecondItem()
    {
        Sell(_secondItem);
    }

    public void SellThirdItem()
    {
        Sell(_thirdItem);
    }

    private bool CanCraft(ResultItemData item)
    {
        foreach (ResultItemData.Ingredient ingredient in item.Ingredients)
        {
            if (_inventory.GetCount(ingredient.Item) <= ingredient.Count)
            {
                return false;
            }
        }

        return true;
    }
}
