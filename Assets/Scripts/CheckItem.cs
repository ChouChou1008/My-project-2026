using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CheckItem : MonoBehaviour 
{
    [SerializeField] private int _inventoryMaxSize = 20;
    [SerializeField] private Inventory _inventory;
    
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            Debug.Log("아이템 감지");
            ItemData itemData = other.GetComponent<ItemData>();
            if(itemData == null) Debug.Log("No Component");
            
            if (!_inventory.TryAdd(itemData))
            {
                Debug.Log("Inventory is Max");
                return;
            }
            Destroy(other.gameObject);
        }
    }
}
