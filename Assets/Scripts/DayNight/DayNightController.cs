using UnityEngine;
using UnityEngine.Rendering;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField] private ManagerScene _managerScene;
    [SerializeField] private float duration = 5f;

    [SerializeField] private GameObject _sun;
    [SerializeField] private GameObject _moon;
    [SerializeField] private GameObject _clock;
    
    
    private float elapsedTime = 0f;
    

    private bool isSun = true;

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsedTime / duration); // 0과 1 사이로 제한하는 함수 
        
        if (isSun)
        {
            _volume.weight = Mathf.Lerp(0f, 1f, progress);
            _clock.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -360f, progress));
            _sun.SetActive(true);
            _moon.SetActive(false);
        }
        else
        {
            _volume.weight = Mathf.Lerp(1f, 0f, progress);
            _clock.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -360f, progress));
            _sun.SetActive(false);
            _moon.SetActive(true);
        }

        if (progress >= 1f)
        {
            if (duration >= 1f && _volume.gameObject.activeSelf)
            {
                Relotation();
            }  
            
            elapsedTime = 0f;
            isSun = !isSun;
        }
    }

    private void Relotation()
    {
        _managerScene.ChangeTestScence();
    }
}