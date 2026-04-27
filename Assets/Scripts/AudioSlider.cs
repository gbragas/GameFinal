using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class AudioSlider : MonoBehaviour
{
    [SerializeField]
    private AudioMixer Mixer;
    [SerializeField]
    private List<AudioSource> AudioSources;
    [SerializeField]
    private TextMeshProUGUI ValueText;
    [SerializeField]
    private AudioMixMode MixMode;

    private void Start()
    {
        Mixer.SetFloat("Volume", Mathf.Log10(PlayerPrefs.GetFloat("Volume", 1) * 20));
    }

    public void OnChangeSlider(float Value)
    {
        float ValuePercent = Value*100;
        ValueText.SetText($"{ValuePercent.ToString("N0")} %");

        switch (MixMode)
        {
            case AudioMixMode.LinearAudioSourceVolume:
                for (int i = 0; i < AudioSources.Count; i++)
                {
                    AudioSources[i].volume = Value;
                }
                break;
            case AudioMixMode.LinearMixerVolume:
                Mixer.SetFloat("Volume", (-80 + Value * 80));
                break;
            case AudioMixMode.LogrithmicMixerVolume:
                Mixer.SetFloat("Volume", Mathf.Log10(Value) * 20);
                break;
        }

        float a = Mathf.Log10(Value) * 20;

        PlayerPrefs.SetFloat("Volume", Value);
        PlayerPrefs.Save();
    }


    public enum AudioMixMode
    {
        LinearAudioSourceVolume,
        LinearMixerVolume,
        LogrithmicMixerVolume
    }
}