using UnityEngine;

public class OtherSceneManager : MonoBehaviour
{
    [SerializeField] private ManagerScene _managerScene;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _managerScene.ResetScence();
        }
    }
}
