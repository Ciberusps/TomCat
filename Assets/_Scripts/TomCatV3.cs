using UnityEngine;
using System.Collections;
using System.Threading;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TomCatV3 : MonoBehaviour
{
    public enum InputVoice
    {
        buttons,
        onUpdate
    }

    public InputVoice inputVoice = InputVoice.buttons;
    public float startRecordThreshold = 1F;
    public float endRecordThreshold = 1F;
    public Vector2 pitchSlowBound;
    public Vector2 pitchFastBound;
    [Range(0, 100)]

    public float sourceVolume = 100;
    public float sensitivity = 100;

    public UISprite visualization;

    public AudioSource micro;
    public AudioSource voice;

    private bool _recordVoice = false;
    //    private bool _stopRecord = false;
    private float loudness;
    private int amountSamples = 256; //increase to get better average, but will decrease performance. Best to leave it
    private string _logString;
    private int _minFreq, _maxFreq;
    private bool _recordIsPlaying = false;
    private float _timer = 0;

    public void GetMicCaps()
    {
        Microphone.GetDeviceCaps(null, out _minFreq, out _maxFreq);//Gets the frequency of the device
        if ((_minFreq + _maxFreq) == 0)//These 2 lines of code are mainly for windows computers
            _maxFreq = 44100;
    }

    public void StartMicro()
    {
        micro.clip = Microphone.Start(null, true, 1, _maxFreq);//Starts recording
        while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
        micro.mute = true;
        micro.loop = true;
        micro.Play();
    }

    void Update()
    {
        if (micro.clip == null)
        {
            GetMicCaps();
            StartMicro();
        }

        if (!_recordVoice)
            micro.volume = (sourceVolume / 100);
        else
            voice.volume = (sourceVolume / 100);

        loudness = GetAveragedVolume() * sensitivity * (sourceVolume / 10);

        if (!_recordIsPlaying && inputVoice == InputVoice.onUpdate)
        {
            if (!_recordVoice)
            {
                if (loudness != 0 && loudness > startRecordThreshold)
                    _StartVoiceRecord();
            }
            else
            {
                if (loudness != 0 && loudness < endRecordThreshold)
                    _EndVoiceRecord();
            }
        }
    }

    void OnGUI()
    {
        _logString = "Average volume : " + GetAveragedVolume()
                 + "\nSensitivity: " + sensitivity
                 + "\nSource volume / 10: " + (sourceVolume / 10);

        _logString = "Loudness: " + loudness + "\n AvVol: " + GetAveragedVolume() + "\n sensitivity: " + sensitivity + "\n sourceVol: " + (sourceVolume / 10);
        GUI.TextArea(new Rect(10, 10, 180, 300), _logString);
    }

    public void _StartVoiceRecord()
    {
        _recordVoice = true;

        micro.Stop();
        Microphone.End(null);

        voice.clip = Microphone.Start(null, true, 10, _maxFreq);//Starts recording
        while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
        voice.mute = true;
        voice.Play();

        visualization.color = Color.red;

        Debug.LogWarning("Start record " + loudness);
    }

    public void _EndVoiceRecord()
    {
        _recordVoice = false;
        _recordIsPlaying = true;

        micro.Stop();
        voice.Stop(); //Stops the audio
        Microphone.End(null); //Stops the recording of the device    

        _PlayVoiceRecord();

        visualization.color = Color.green;
        Debug.LogWarning("End record " + loudness);
    }

    public void _PlayVoiceRecord()
    {
        _HandleSound();

        micro.mute = false;
        micro.loop = false;
        micro.Play();

        //        print("Mic mute: " + micro.mute);

        _timer = 0;
        StartCoroutine(WaitForMicroPlay());
    }

    IEnumerator WaitForMicroPlay()
    {
        _timer += Time.deltaTime;
        yield return new WaitForSeconds(0.1F);

        Debug.LogWarning("End playing micro" + _timer);
        voice.mute = false;
        voice.loop = false;
        voice.Play();
        //print("Voice mute: " + voice.mute);
        //StartCoroutine(WaitForVoicePlay());
        //StartMicro();
    }

    /*    IEnumerator WaitForVoicePlay()
        {
            if (voice.isPlaying)
                yield return new WaitForSeconds(0.1F);

            _recordIsPlaying = false;

    //        StartMicro();


            Debug.LogWarning("End playing voice");
        }*/

    private void _HandleSound()
    {
        var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

        var randPitch = randNum == 0
                ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

        voice.pitch = randPitch;
        micro.pitch = randPitch;
    }

    float GetAveragedVolume()
    {
        float[] data = new float[amountSamples];
        float a = 0;

        if (!_recordVoice)
            micro.GetOutputData(data, 0);
        else
            voice.GetOutputData(data, 0);

        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }

        return a / amountSamples;
    }
}
