using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerScene : MonoBehaviour
{
    [SerializeField] private GameObject _settingObject;
    [SerializeField] private GameObject _controlsImage;
    [SerializeField] private GameObject _audioImage;
    [SerializeField] private Slider _volumeSlider;
    
    
    public float ChangeVolume(float volume)
    {
        return _volumeSlider.value = volume;
    }
    public void ChangeControls()
    {
        _controlsImage.SetActive(true);
    }

    public void ChangeAudio()
    {
        _controlsImage.SetActive(false);
        _audioImage.SetActive(true);
    }
    
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
