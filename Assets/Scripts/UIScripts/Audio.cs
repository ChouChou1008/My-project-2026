using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Audio : MonoBehaviour
{
    [SerializeField] private AudioMixer  _audioMixer;

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        _audioMixer.SetFloat("Audio",Mathf.Log10(volume) * 20);
    }
    
    
}
