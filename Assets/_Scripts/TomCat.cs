using UnityEngine;
using System.Collections;
using System.Linq;

public class TomCat : MonoBehaviour
{
    public AudioClip sample;
    public Vector2 pitchSlowBound;
    public Vector2 pitchFastBound;
    public int lengthOfRecord;

    private AudioSource _audioSource;
    private AudioClip _voice;
    private AudioClip _waitForVoiceClip;
    private string[] _microphoneDevices;
    private bool _microConnected;
    private int _minFreq, _maxFreq;
    private bool _waitingForVoice;
    private int waitingForVoiceTime;

    void Start()
    {
        _audioSource = gameObject.GetComponent<AudioSource>();
    
        _microphoneDevices = Microphone.devices;

        _CheckMicrophone();
        _waitForVoiceClip = Microphone.Start(null, true, waitingForVoiceTime, _maxFreq);


        _HandleSound();

        _audioSource.clip = _voice;
        _audioSource.clip = sample;

        PlaySound();
    }

    public void PlaySound()
    {
        _HandleSound();
        _audioSource.Play();
    }

    private void _HandleSound()
    {
        var randNum = Random.Range(0, 2);
        _audioSource.pitch =
            randNum == 0 
                ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y) 
                : Random.Range((float)pitchFastBound.x, pitchFastBound.y);  
    }

   /* private void _RecordFromMicrophone()
    {
        if (_microphoneDevices.Length > 0)
            for (var i = 0; i < _microphoneDevices.Length; i++)
            {
                print(_microphoneDevices[i]);
            }
    }*/

    private void _CheckMicrophone()
    {
        if (_microphoneDevices.Length <= 0)
        {
            _microConnected = false;
            Debug.LogWarning("Micro not connected");
        }
        else
        {
            _microConnected = true;

            Microphone.GetDeviceCaps(null, out _minFreq, out _maxFreq);

            if (_minFreq == 0 && _maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate  
                _maxFreq = 44100;
            }
        }
    }

    void Update()
    {
        if (_microConnected && _waitingForVoice)
        {
//            _waitForVoiceClip;
        }
        else
        {
            _voice = Microphone.Start(null, true, lengthOfRecord, _maxFreq);
        }
    }
}
