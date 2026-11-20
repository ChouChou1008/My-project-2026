using UnityEngine;

public class FristSenceCheck : MonoBehaviour
{
    [SerializeField] private ManagerScene _managerScene;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("감지");
            _managerScene.ChangeScence();
        }
    }
}
