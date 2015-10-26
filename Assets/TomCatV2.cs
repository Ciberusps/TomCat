using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TomCatV2 : MonoBehaviour
{
    public GameObject audioInputObject;
    public float startRecordThreshold = 1F;
    public float endRecordThreshold = 1F;

    private MicrophoneInput _micIn;
    private bool _recordVoice;
    void Start()
    {
        if (audioInputObject != null)
            _micIn = audioInputObject.GetComponent<MicrophoneInput>();
    }

    void Update()
    {
        if (!_recordVoice)
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
        }
        
    }

    private void _StartVoiceRecord()
    {
        _recordVoice = true;
        _micIn.StartVoiceRecord();
    }

    private void _EndVoiceRecord()
    {
        _recordVoice = false;
        _micIn.StartVoiceRecord();
    }
}
