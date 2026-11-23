using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Audio : MonoBehaviour
{
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioMixer  _audioMixer;

    public void SetVolume(float volume)
    {
        _audio.volume = Mathf.Clamp(volume, 0.0f, 1.0f);
    }

    public void IncreaseVolume(float volume)
    {
        _audio.volume = Mathf.Clamp(_audio.volume + volume, 0.0f, 1.0f);
    }

    public void DecreaseVolume(float volume)
    {
        _audio.volume = Mathf.Clamp(_audio.volume - volume, 0.0f, 1.0f);
    }
    
    
}
