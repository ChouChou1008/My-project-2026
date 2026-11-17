using TMPro;
using UnityEngine;

public class StoreTrigger : MonoBehaviour
{
   [SerializeField] private LayerMask _playerLayer;
   [SerializeField] private GameObject _storeUI;
   [SerializeField] private Store _store;
   private void OnTriggerEnter2D(Collider2D other)
   {
      if((_playerLayer.value & (1 << other.gameObject.layer)) !=0)
      {
         //_store.SlotUpdate();
         _storeUI.SetActive(true);
         
      }
   }

   private void OnTriggerExit2D(Collider2D other)
   {
      if((_playerLayer.value & (1 << other.gameObject.layer)) !=0)
      {
         _storeUI.SetActive(false);
      }
   }
    
}
