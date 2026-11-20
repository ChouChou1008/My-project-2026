using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get ; private set;}
    
    public Coin coin{get; private set;}
    public InventoryData Inventory{get; } = new ();
   

    private void Awake()
    {
        if (!SetSingleton())
        {
            return;
        }
        CacheComponent();
    }

    private bool SetSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        return true;
    }


    private void CacheComponent()
    {
        coin = GetComponent<Coin>();
    }
    
}
