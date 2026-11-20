using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerScene : MonoBehaviour
{
    public void ChangeScence()
    {
        SceneManager.LoadScene("Scenes/MainSence");
    }

    public void ChangeTestScence()
    {
        SceneManager.LoadScene(Random.Range(2,3));
    }

    public void ResetScence()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    
    public void Exit()
    {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
    
}
