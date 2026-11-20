using UnityEngine;

public class Check : MonoBehaviour
{
    [SerializeField] private ManagerScene _managerScene;
    [SerializeField] private BoxTurret  _boxTurret;
    private bool _isCheck;
    private bool _income;
    private void Update()
    {
        if (_boxTurret.IsCheck)
        { 
            gameObject.SetActive(true);
            _isCheck = true;
        }

        if (_isCheck && _income)
        {
            _managerScene.ChangeScence();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("감지");
            _income = true;
        }
    }
}
