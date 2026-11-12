using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _smoothSpeed = 5f;
    private void LateUpdate()
    {
        Vector3 targetPos = _player.transform.position + _offset;
        
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPos, 
            _smoothSpeed * Time.deltaTime);
    }
    
}
