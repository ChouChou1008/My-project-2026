using UnityEngine;

public class StoreTrigger : MonoBehaviour
{
   [SerializeField] private LayerMask _playerLayer;
   [SerializeField] private GameObject _storeUI;
   private void OnTriggerEnter2D(Collider2D other)
   {
      if((_playerLayer.value & (1 << other.gameObject.layer)) !=0)
      {
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
