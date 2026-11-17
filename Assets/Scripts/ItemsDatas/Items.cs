using UnityEngine;
using UnityEngine.Pool;
    
public class Items : MonoBehaviour
{
    private IObjectPool<Items> _pool;


    public void SetPool(IObjectPool<Items> pool)
    {
        _pool = pool;
    }

    public void OnReturnToPool()
    {
        if (_pool == null)
        {
            Destroy(gameObject);
            return;
        }
        _pool.Release(this);
    }
}
