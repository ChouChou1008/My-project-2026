using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;

    public void SetItem(ItemData item, int count)
    {
        _icon.enabled = true;
        _icon.sprite = item.ItemSprite;
        _countText.text = count > 1 ? count.ToString() : string.Empty;   // 1개면 숫자 숨김
    }

    public void Clear()
    {
        _icon.enabled = false;
        _icon.sprite = null;
        _countText.text = string.Empty;
    }
}