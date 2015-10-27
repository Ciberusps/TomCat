using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TomCatV2 : MonoBehaviour
{
    public enum InputVoice
    {
        buttons,
        onUpdate
    }

    // public GameObject audioInputObject;

    public InputVoice inputVoice = InputVoice.buttons;
    public float startRecordThreshold = 1F;
    public float endRecordThreshold = 1F;
    public Vector2 pitchSlowBound;
    public Vector2 pitchFastBound;
    [Range(0, 100)]
    public float sourceVolume = 100;
    public MicControlC micCon;
    public float sensitivity = 100;
    public Image visualization;

    //public AudioClip waitForMicroClip;
    //public AudioClip micro;

    private bool _recordVoice = false;
    private bool _stopRecord = false;
    private float loudness;
    private int amountSamples = 256; //increase to get better average, but will decrease performance. Best to leave it


    void Start()
    {
        /*if (audioInputObject != null)
            _micIn = audioInputObject.GetComponent<MicrophoneInput>();*/
    }

    void Update()
    {
        if (inputVoice == InputVoice.buttons)
        {
            loudness = micCon.loudness;
        }
        else
        {
            audio.volume = (sourceVolume / 100);
            //l = MicInput.MicLoudness;
            //l = MicrophoneInput1.instance.loudness;

            if (!_recordVoice)
            {
                loudness = micCon.loudness;
            }
            else if (_recordVoice)
            {
                loudness = GetAveragedVolume() * sensitivity * (sourceVolume / 10);
            }

            if (!_stopRecord)
            {
                if (!_recordVoice)
                {
                    if (loudness > startRecordThreshold)
                        _StartVoiceRecord();
                }
                else
                {
                    if (loudness < endRecordThreshold)
                        _EndVoiceRecord();
                }
            }
        }

        print(loudness);
    }

    public void _StartVoiceRecord()
    {
        _recordVoice = true;

        //Microphone.End(null);
        audio.clip = Microphone.Start(null, false, 30, 16000);
        visualization.color = Color.red;

        Debug.LogWarning("StartRecord " + micCon.loudness);
    }

    public void _EndVoiceRecord()
    {
        _recordVoice = false;
        Microphone.End(null);

        //Debug.LogWarning("EndRecord " + micCon.loudness);
        Debug.LogWarning("EndRecord: " + loudness);
        _stopRecord = true;
        visualization.color = Color.white;


        _HandleSound();
        Play();
    }

    public void Play()
    {
        audio.Play();
        audio.loop = false;
        Debug.LogWarning("play " + micCon.loudness);
    }

    private void _HandleSound()
    {
        var randNum = Random.Range(0, 2);
        audio.pitch =
            randNum == 0
                ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
    }

    public void RestartMicroRecord()
    {
        micCon.StartMicrophone();

        //MicrophoneInput1.instance.Restart();
        _stopRecord = false;
    }

    float GetAveragedVolume()
    {
        float[] data = new float[amountSamples];
        float a = 0;

        audio.GetOutputData(data, 0);
        /*foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }*/

        for (int i = 0; i < data.Length - 1; i++)
        {
            /*if (data[i] == 0)
                print("S: " + i);
                */
            a += Mathf.Abs(data[i]);
        }
        if (a == 0)
            print("End A: " + a);
        return a / amountSamples;
    }
}
