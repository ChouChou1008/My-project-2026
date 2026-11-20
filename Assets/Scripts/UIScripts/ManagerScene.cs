using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerScene : MonoBehaviour
{
    public void ChangeScence()
    {
        SceneManager.LoadScene("Scenes/MapDesin");
    }

    public void ChangeTestScence()
    {
        SceneManager.LoadScene("Scenes/otherTest");
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
