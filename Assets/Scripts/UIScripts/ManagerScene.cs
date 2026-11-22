using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerScene : MonoBehaviour
{
    [SerializeField] private GameObject _settingObject;
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

    public void SettingButton()
    {
        _settingObject.SetActive(true);
    }

    public void OnButtonClick()
    {
        _settingObject.SetActive(false);
    }
    
    
    public void Exit()
    {
       #if UNITY_EDITOR
           UnityEditor.EditorApplication.isPlaying = false;
       #else
           Application.Quit();
       #endif
    }
    
}
