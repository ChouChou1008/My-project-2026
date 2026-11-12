using UnityEngine;
using UnityEngine.Pool;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Items[] _itemsPrefab;
    [SerializeField] private float _spawnInterval = 15f;
    [SerializeField] private int _startCount = 5;

    
    private IObjectPool<Items>[] _pools;
    private Items[] _pointItems;
    private float _spawnTime;
    
    private void Awake()
    {
        _pointItems = new Items[_spawnPoints.Length];
        _pools = new IObjectPool<Items>[_itemsPrefab.Length];

        for (int i = 0; i < _itemsPrefab.Length; i++)
        {
            int index = i;
            _pools[i] = new ObjectPool<Items>(
                () => CreatePool(index),
                OnTakeFromPool,
                OnReturnToPool,
                OnDestroyItem,
                true,
                _spawnPoints.Length,
                _spawnPoints.Length
            );
        }
    }

    private void Start()
    {
        SpawnStart();
    }

    private void Update()
    {
        SpawnTimer();
    }

    private void SpawnStart()
    {
        for (int i = 0; i < _startCount; i++)
        {
            Spawn();
        }
        _spawnTime = _spawnInterval;
    }

    private void SpawnTimer()
    {
        _spawnTime -= Time.deltaTime;
        if (_spawnTime > 0f) return;
        
        _spawnTime = _spawnInterval;
        Spawn();
    }

    private void Spawn()
    {
        int point = GetEmptyPoint();
        if (point < 0) return;
        
        int kind = Random.Range(0, _itemsPrefab.Length); // 둘 중 하나
        Items item = _pools[kind].Get();
        item.transform.position = _spawnPoints[point].position;
        _pointItems[point] = item;
    }

    private int GetEmptyPoint()  //빈 지점
    {
        int start = Random.Range(0, _spawnPoints.Length);
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            int point = (start + i) % _spawnPoints.Length; 
            if (_pointItems[point] == null) return point;
        }

        return -1;
    }
    
    private Items CreatePool(int index)
    {
       Items item = Instantiate(_itemsPrefab[index],transform);
       item.SetPool(_pools[index]);
       return item;
    }

    private void OnTakeFromPool(Items item)
    {
        item.gameObject.SetActive(true);
    }

    private void OnReturnToPool(Items item)
    {
        item.gameObject.SetActive(false);

        for (int i = 0; i < _pointItems.Length; i++)
        {
            if (_pointItems[i] != item) continue;
            _pointItems[i] = null;
            break;
        }
    }

    private void OnDestroyItem(Items item)
    {
        Destroy(item.gameObject);
    }
    
}
