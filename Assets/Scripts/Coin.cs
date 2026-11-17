using System;
using TMPro;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int _startCoins = 30;
    [SerializeField] private TMP_Text _storeCoinText;
    [SerializeField] private TMP_Text _inventoryCoinText;
    
    public int Coins { get; private  set; }
    
    public event Action<int> OnCoinChange;
    
    private void Awake() =>Coins = _startCoins;

    private void Update()
    {
        _storeCoinText.text = Coins.ToString();
        _inventoryCoinText.text = Coins.ToString();
    }

    public void Add(int amount)
    {
        if (amount < 0)
        {
            return;
        }
        Coins += amount;
        OnCoinChange?.Invoke(Coins);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Coins < amount)
        {
            return false;
        }
        Coins -= amount;
        OnCoinChange?.Invoke(Coins);
        return true;
    }
}
