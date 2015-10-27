using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TomCatV2 : MonoBehaviour
{
    public GameObject audioInputObject;
    public float startRecordThreshold = 1F;
    public float endRecordThreshold = 1F;
    public Vector2 pitchSlowBound;
    public Vector2 pitchFastBound;

    //    private MicrophoneInput _micIn;
    public MicControlC micCon;

    private bool _recordVoice = false;
    private float l;
    private bool _stopRecord = false;
    void Start()
    {
        /*if (audioInputObject != null)
            _micIn = audioInputObject.GetComponent<MicrophoneInput>();*/
    }

    void Update()
    {
        /* if (!_recordVoice)
         {
             float l = _micIn.loudness;

             if (l > startRecordThreshold)
             {
                 _StartVoiceRecord();
             }
         }
         else
         {
             float l = _micIn.loudness;

             if (l < endRecordThreshold)
             {
                 _EndVoiceRecord();
             }
         }*/


        l = micCon.loudness;


        if (!_stopRecord)
        {
            if (!_recordVoice)
            {
                if (l > startRecordThreshold)
                    _StartVoiceRecord();
            }
            else
            {
                if (l < endRecordThreshold)
                    _EndVoiceRecord();
            }
        }
        

        print(l);
    }

    public void _StartVoiceRecord()
    {
        _recordVoice = true;
        audio.clip = Microphone.Start(null, false, 30, 44100);

        Debug.LogWarning("StartRecord " + micCon.loudness);
    }

    public void _EndVoiceRecord()
    {
        _recordVoice = false;
        Microphone.End(null);

        Debug.LogWarning("EndRecord " + micCon.loudness);

        _stopRecord = true;

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
        _stopRecord = false;
    }
}
