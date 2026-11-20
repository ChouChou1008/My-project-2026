using UnityEngine;
using UnityEngine.Rendering;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField] private ManagerScene _managerScene;
    private float elapsedTime = 0f;
    [SerializeField] private float duration = 5f;

    private bool isSun = true;

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsedTime / duration);

        if (isSun)
        {
            _volume.weight = Mathf.Lerp(0f, 1f, progress);
        }
        else
        {
            _volume.weight = Mathf.Lerp(1f, 0f, progress);
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