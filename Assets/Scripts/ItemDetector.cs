using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class ItemDetector : MonoBehaviour 
{
    [SerializeField] private LayerMask _playerMask;
    
    
    private bool _isCheckGather;
    
    public bool  IsCheckGather=>_isCheckGather;

    public static event Action<ItemDetector> OnCheckGather;

    private void OnDisable()
    {
        _isCheckGather = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if((_playerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            _isCheckGather = true;
            OnCheckGather?.Invoke(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if((_playerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            _isCheckGather = false;
            OnCheckGather?.Invoke(this);
        }
    }
}
