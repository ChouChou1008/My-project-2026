using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerScene : MonoBehaviour
{
    public void ChangeScence()
    {
        SceneManager.LoadScene("Scenes/TestSence");
    }
}
