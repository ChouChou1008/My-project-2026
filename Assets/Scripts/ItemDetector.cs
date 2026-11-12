using System;
using UnityEngine;

public class ItemDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _playerMask;

    private bool _isCheckGather;
    public bool IsCheckGather => _isCheckGather;

    public static event Action<ItemDetector> OnCheckGather;     // ItemDetector → ItemDetector2D

    private void OnDisable()
    {
        _isCheckGather = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            _isCheckGather = true;
            Debug.Log("범위에서 들어옴");
            OnCheckGather?.Invoke(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((_playerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            _isCheckGather = false;
            Debug.Log("범위에서 나감");
            OnCheckGather?.Invoke(this);   
        }
    }
    
}