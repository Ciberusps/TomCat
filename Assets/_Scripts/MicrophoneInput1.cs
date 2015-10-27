using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneInput1 : MonoBehaviour
{
    public static MicrophoneInput1 instance;

    public float sensitivity = 100;
    public float loudness = 0;

    // Use this for initialization
    void Start()
    {
        
    }

    public void Restart()
    {
        audio.clip = Microphone.Start(null, true, 10, 44100);
        audio.loop = true;
        audio.mute = true;
        while (!(Microphone.GetPosition(null) > 0))
            audio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        loudness = GetAveragedVolume() * sensitivity;
    }

    float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;

        audio.GetOutputData(data, 0);
        for (var i = 0; i < data.Length; i++)
        {
            a += Mathf.Abs(data[i]);
        }

        return a / 256;
    }

    void OnEnable()
    {
        instance = this;
    }

    void OnDisable()
    {
        instance = null;
    }
}
