using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    
    private Animator _animator;

    private void Awake()
    {
        CacheComponent();
    }

    private void CacheComponent()
    {
        _animator = GetComponentInChildren<Animator>();
    }
}
