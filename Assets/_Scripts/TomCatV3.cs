using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class TomCatV3 : MonoBehaviour
{
    public enum InputVoice
    {
        buttons,
        onUpdate
    }

    public enum Record
    {
        withSamples,
        byStartAndEndMicro
    }

    public enum VoiceHandle
    {
        onlyRandPitch,
        reversePlaying,
        withFunnyMoments
    }

    public InputVoice inputVoice = InputVoice.buttons;
    public Record recordType = Record.byStartAndEndMicro;
    public VoiceHandle voiceHandle = VoiceHandle.onlyRandPitch;
    public float startRecordThreshold = 100F;
    public float endRecordThreshold = 40F;
    public float funnyMomentTreshold;
    public Vector2 pitchSlowBound;
    public Vector2 pitchFastBound;
    [Range(0, 100)]
    public float sourceVolume = 100;

    [Range(0, 100)]
    public float volume = 5;
    public float sensitivity = 100;
    public int voiceClipLength = 10;
    public int numOfSamplesBeforeStart;
    public int numOfSamplesAfterEnd;
    public int numOfGetSamplesForAvLoudness;
    public float notEndRecordTime = 0.5F;
    public float notRecordingTime = 0.3F;
    public float funnyMomentTime = 0.1F;
    public float wordTime = 0.25F;
    public bool onlyFunnyVoice = true;
    public UISprite visualization;
    public List<AudioClip> funnyMoments;

    public AudioSource micro;
    public AudioSource voice;
    public AudioSource finished;
    public AudioSource samplesBeforeStart;
    public AudioSource funnyMoment;
    public UILabel startTresholdLabel;
    public UILabel endTresholdLabel;
    public UIToggle playReverseToggle;
    public UIToggle withFunnyMomentsToggle;

    private AudioClip _samplesAfterEnd;
    private AudioClip _avLoudness;
    private bool _recordVoice = false;
    private float _loudness;
    private int amountSamples = 256; //increase to get better average, but will decrease performance. Best to leave it
    private string _MicroLogString;
    private string _RecordLogString;
    private string _TreasholdsString;
    private int _minFreq, _maxFreq;
    private bool _recordIsPlaying = false;
    private float _timer = 0;
    private int _lastSample = 0;
    private int _voiceOffSet = 0;
    private int _micSamples;
    private int _micPosOnStartRecord;
    private float _voiceStartTime;
    private float _voiceEndTime;
    private int _numOfClips;
    private int _voiceLength;
    private int _micPos;
    private int _loudnessGetCount;
    private float _notEndRecordTimer = 0;
    private float _notRecordingTimer = 0;
    private bool _notRecording = false;
    private float _randPitch;
    private float _averageLoudness = 0;
    private int _countOfGetLoudness = 0;
    private int _lastMicPos = 0;
    private bool _funnyMomentTime = false;
    private float _funnyMomentTimer;
    private float _finishedTimeStop;
    private float _timeFromLastFunnyMoment = 0;


    void Start()
    {
        _timeFromLastFunnyMoment = wordTime;
    }

    public void GetMicCaps()
    {
        Microphone.GetDeviceCaps(null, out _minFreq, out _maxFreq);//Gets the frequency of the device
        if ((_minFreq + _maxFreq) == 0)//These 2 lines of code are mainly for windows computers
            _maxFreq = 44100;
    }

    public void StartMicro()
    {
        micro.loop = true; // Set the AudioClip to loop
        micro.clip = Microphone.Start(null, true, 1, _maxFreq);//Starts recording
        while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
        micro.mute = true;
        micro.Play(); // Play the audio source!


        _micSamples = micro.clip.samples * micro.clip.channels;
    }

    private void FixedUpdate()
    {
        if (micro.clip == null)
        {
            GetMicCaps();
            StartMicro();
            voiceHandle = VoiceHandle.onlyRandPitch;
        }

        _micPos = Microphone.GetPosition(null);

        if (!_recordVoice)
            micro.volume = (sourceVolume / 100);
        else
            voice.volume = (sourceVolume / 100);

        _loudness = GetAveragedVolume() * sensitivity * (sourceVolume / 10);
        //        _averageLoudness = _loudness/_countOfGetLoudness;
        /*var curPos = _micPos;
        var currentPart = Mathf.CeilToInt(_micPos/numOfGetSamplesForAvLoudness) * numOfGetSamplesForAvLoudness;
        print(curPos);
        print(" > ");
        print(currentPart);*/

        switch (voiceHandle)
        {
            case VoiceHandle.withFunnyMoments:
                if (_recordIsPlaying)
                {
                    _timeFromLastFunnyMoment += Time.fixedDeltaTime;

                    if (finished.isPlaying)
                        visualization.color = Color.green;
                    else if (funnyMoment.isPlaying)
                        visualization.color = Color.magenta;

                    if (finished.time >= finished.clip.length)
                    {
                        _recordIsPlaying = false;
                        _finishedTimeStop = 0;
                    }

                    if (!_funnyMomentTime)
                    {
                        if (_loudness > 0 && _loudness < funnyMomentTreshold && _timeFromLastFunnyMoment > wordTime)
                            _funnyMomentTimer += Time.fixedDeltaTime;
                        else
                            _funnyMomentTimer = 0;

                        if (_funnyMomentTimer >= funnyMomentTime)
                        {
                            _funnyMomentTime = true;
                            _PlayFunnyMoment();
                        }
                    }
                    else
                    {
                        if (!funnyMoment.isPlaying)
                        {
                            _timeFromLastFunnyMoment = 0;
                            _funnyMomentTime = false;
                            _funnyMomentTimer = 0;
                            _ContinueFinishedClip();
                        }
                    }
                }
                else
                {
                    finished.time = 0;
                    visualization.color = Color.white;
                }
                

                if (!_recordIsPlaying && inputVoice == InputVoice.onUpdate)
                    if (recordType == Record.withSamples)
                    {
                        if (_recordVoice)
                            RecordWithSamples();
                        if (_loudness != 0 && _loudness > startRecordThreshold)
                        {
                            _notEndRecordTimer = 0;

                            if (!_recordVoice)
                            {
                                TakeSecFromMic();
                                _numOfClips++;
                                voice.clip = AudioClip.Create("MyClip_" + _numOfClips, _maxFreq * micro.clip.channels * voiceClipLength, micro.clip.channels, _maxFreq, false, false);
                                _voiceStartTime = Time.time;
                                _micPosOnStartRecord = _micPos;
                                _lastSample = _micPosOnStartRecord;

                                _voiceOffSet = 0;
                                _recordVoice = true;
                            }

                        }

                        if (_loudness != 0 && _loudness < endRecordThreshold)
                        {
                            if (_recordVoice)
                            {
                                if (_notEndRecordTimer == 0)
                                    _notEndRecordTimer = Time.fixedTime + notEndRecordTime;

                                if (_notEndRecordTimer <= Time.fixedTime)
                                {
                                    _notEndRecordTimer = 0;
                                    _recordVoice = false;
                                    _voiceEndTime = Time.time;
                                    visualization.color = Color.white;
                                    RecordSamplesAfterVoice();
                                    _PlayVoiceRecord();
                                }
                            }
                        }
                    }

                _lastMicPos = _micPos;
                break;
            default:
                if (!finished.isPlaying /*&& !funnyMoment.isPlaying*/)
                {
                    if (_notRecording)
                    {
                        _notRecording = false;
                        _notRecordingTimer = Time.time + notRecordingTime;
                    }

                    if (Time.time > _notRecordingTimer)
                    {
                        _recordIsPlaying = false;
                    }
                }

                if (!finished.isPlaying)
                    _recordIsPlaying = false;

                /*if (voiceHandle != VoiceHandle.withFunnyMoments)
                {
                    if (!finished.isPlaying)
                        _recordIsPlaying = false;

                    if (!finished.isPlaying && !funnyMoment.isPlaying)
                    {
                        if (_notRecording)
                        {
                            _notRecording = false;
                            _notRecordingTimer = Time.time + notRecordingTime;
                        }

                        if (Time.time > _notRecordingTimer)
                        {
                            _recordIsPlaying = false;
                        }
                    }
                }
                else
                {
                    if (!_funnyMomentTime && !finished.isPlaying)
                        _recordIsPlaying = false;
                }*/

                if (_recordIsPlaying)
                {
                    /*if (funnyMoment.isPlaying)
                        visualization.color = Color.magenta;
                    else*/
                    visualization.color = Color.green;

                    /*if (voiceHandle == VoiceHandle.withFunnyMoments)
                    {

                        if (!funnyMoment.isPlaying && _loudness != 0 && _loudness < funnyMomentTreshold)
                        {
                            if (_funnyMomentTime)
                            {
                                print("FUUUUUN");
                                print(Time.time);
                                _funnyMomentTime = false;
                                _funnyMomentTimer = Time.time + funnyMomentTime;
                            }

                            if (Time.time > _funnyMomentTimer)
                            {
                                PlayFunnyMoment();
                            }
                        }

                        if (!funnyMoment.isPlaying && !_funnyMomentTime && Time.time > _funnyMomentTimer)
                            _funnyMomentTime = true;

                    }*/
                }
                else if (!_recordVoice /*&& !funnyMoment.isPlaying*/)
                {
                    visualization.color = Color.white;
                }

                if (!_recordIsPlaying && /*!funnyMoment.isPlaying && */inputVoice == InputVoice.onUpdate)
                    if (recordType == Record.withSamples)
                    {
                        if (_recordVoice)
                            RecordWithSamples();
                        if (_loudness != 0 && _loudness > startRecordThreshold)
                        {
                            _notEndRecordTimer = 0;

                            if (!_recordVoice)
                            {
                                TakeSecFromMic();
                                _numOfClips++;
                                voice.clip = AudioClip.Create("MyClip_" + _numOfClips, _maxFreq * micro.clip.channels * voiceClipLength, micro.clip.channels, _maxFreq, false, false);
                                _voiceStartTime = Time.time;
                                _micPosOnStartRecord = _micPos;
                                _lastSample = _micPosOnStartRecord;

                                _voiceOffSet = 0;
                                _recordVoice = true;
                            }

                        }

                        if (_loudness != 0 && _loudness < endRecordThreshold)
                        {
                            if (_recordVoice)
                            {
                                if (_notEndRecordTimer == 0)
                                    _notEndRecordTimer = Time.fixedTime + notEndRecordTime;

                                if (_notEndRecordTimer <= Time.fixedTime)
                                {
                                    _notEndRecordTimer = 0;
                                    _recordVoice = false;
                                    _voiceEndTime = Time.time;
                                    visualization.color = Color.white;
                                    RecordSamplesAfterVoice();
                                    _PlayVoiceRecord();
                                }
                            }
                        }
                    }

                _lastMicPos = _micPos;
                break;
        }
    }

    void OnGUI()
    {
        _MicroLogString =
            "                     MICRO"
            + "\n Loudness: " + Math.Round(_loudness, 3)
            + "\n AV Loudness: " + Math.Round(_averageLoudness, 3);
        /*+ "\n AvVol: " + GetAveragedVolume()
        + "\n sensitivity: " + sensitivity
        + "\n sourceVol: " + (sourceVolume / 10)
        + "\n Channels: " + micro.clip.channels
        + "\n Samples: " + micro.clip.samples
        + "\n Time: " + Time.time
        + "\n Timer: " + _notEndRecordTimer;*/
        if (voice.clip != null)
            _MicroLogString += "\n Voice clip: " + voice.clip.length;

        _RecordLogString =
            "                     Samples"
            + "\n Record is playing: " + _recordIsPlaying
            + "\n Record voice: " + _recordVoice
            + "\n Voice start: " + _voiceStartTime
            + "\n Voice end: " + _voiceEndTime
            + "\n Last Sample: " + _lastSample
            + "\n Microphone pos: " + _micPos
            + "\n Last mic pos: " + _micPosOnStartRecord
            + "\n Mic samples(samp * channels): " + _micSamples
            + "\n Voice record length : " + _voiceLength
            + "\n Voice record length(sec) : " + _voiceLength / _maxFreq
            + "\n Finished time: " + finished.time;

        /*_TreasholdsString =
            "                     Tresholds"
            + "\n Treshold start: " + startRecordThreshold
            + "\n Treshold end: " + endRecordThreshold;*/

        startTresholdLabel.text = "Start treshold: " + startRecordThreshold.ToString();
        endTresholdLabel.text = "End treshold: " + endRecordThreshold.ToString();

        GUI.TextArea(new Rect(10, 10, 160, 100), _MicroLogString);
        GUI.TextArea(new Rect(10, 120, 230, 200), _RecordLogString);
    }

    public void _StartVoiceRecord()
    {
        if (recordType == Record.byStartAndEndMicro)
        {
            _recordVoice = true;

            micro.Stop();
            Microphone.End(null);

            voice.loop = true; // Set the AudioClip to loop
            voice.clip = Microphone.Start(null, true, 10, _maxFreq);//Starts recording
            while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
            voice.mute = true;
            voice.Play(); // Play the audio source!

            visualization.color = Color.red;

            Debug.LogWarning("Start record " + _loudness);
        }
        else
        {
            _recordVoice = true;

            voice.loop = true; // Set the AudioClip to loop
            voice.mute = true;

            RecordWithSamples();

            visualization.color = Color.red;

            Debug.LogWarning("Start record " + _loudness);
        }
    }

    public void _EndVoiceRecord()
    {
        _recordVoice = false;
        voice.Stop(); //Stops the audio
        Microphone.End(null); //Stops the recording of the device    

        _PlayVoiceRecord();

        visualization.color = Color.green;
        Debug.LogWarning("End record " + _loudness);
    }

    public void _PlayVoiceRecord()
    {
        if (recordType == Record.byStartAndEndMicro)
        {
            _recordIsPlaying = true;

            _HandleSound();

            print("Play micro");

            micro.mute = false;
            micro.loop = false;
            micro.Play();

            Invoke("_PlayVoice", micro.clip.length);
        }
        else if (recordType == Record.withSamples)
        {
            _recordIsPlaying = true;
            _HandleSound();

            finished.mute = false;
            finished.loop = false;
            finished.Play();

            _notRecording = true;
            //            _funnyMomentTime = true;
        }
    }

    private void _PlayVoice()
    {
        Invoke("_RecordIsNotPlaying", voice.clip.length);

        print("Voice played");
        voice.mute = false;
        voice.loop = false;
        voice.Play();
    }

    private void _RecordIsNotPlaying()
    {
        _recordIsPlaying = false;
        StartMicro();
    }

    private void _HandleSound()
    {
        if (recordType == Record.byStartAndEndMicro)
        {
            var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

            _randPitch = randNum == 0
                    ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                    : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

            voice.pitch = _randPitch;
        }
        else if (recordType == Record.withSamples)
        {
            //             ЗАпись в 3ий
            _voiceLength = Mathf.CeilToInt(_maxFreq * (_voiceEndTime - _voiceStartTime) * voice.clip.channels);
            print(_voiceLength);

            _HandleSamplesBeforeStart();

            float[] dataVoice = new float[_voiceLength];
            voice.clip.GetData(dataVoice, 0);

            float[] dataSamplesBeforeStart = new float[numOfSamplesBeforeStart];
            samplesBeforeStart.clip.GetData(dataSamplesBeforeStart, 0);

            /*float[] dataSamplesAfterEnd = new float[numOfSamplesAfterEnd];
            _samplesAfterEnd.GetData(dataSamplesAfterEnd, 0);*/
            if (finished.clip != null)
                DestroyImmediate(finished.clip);
            finished.clip = AudioClip.Create("Finished_" + _numOfClips, _voiceLength + numOfSamplesBeforeStart /*+ numOfSamplesAfterEnd*/, voice.clip.channels, _maxFreq, false, false);
            finished.clip.SetData(dataSamplesBeforeStart, 0);
            finished.clip.SetData(dataVoice, numOfSamplesBeforeStart);
            //            finished.clip.SetData(dataSamplesAfterEnd, numOfSamplesBeforeStart + _voiceLength);

            float[] samples = new float[finished.clip.samples * finished.clip.channels];
            finished.clip.GetData(samples, 0);
            int i = 0;
            while (i < samples.Length)
            {
                samples[i] = samples[i] * volume;
                ++i;
            }
            finished.clip.SetData(samples, 0);

            DestroyImmediate(samplesBeforeStart.clip);
            DestroyImmediate(voice.clip);
            DestroyImmediate(_samplesAfterEnd);

            switch (voiceHandle)
            {
                case VoiceHandle.onlyRandPitch:
                    if (onlyFunnyVoice)
                    {
                        _randPitch = Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                    }
                    else
                    {
                        var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

                        _randPitch = randNum == 0
                            ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                            : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                    }
                    finished.pitch = _randPitch;
                    break;
                case VoiceHandle.reversePlaying:

                    if (onlyFunnyVoice)
                    {
                        _randPitch = Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                    }
                    else
                    {
                        var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

                        _randPitch = randNum == 0
                            ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                            : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                    }

                    _randPitch = -_randPitch;

                    finished.pitch = _randPitch;
                    finished.time = finished.clip.length;
                    break;
                case VoiceHandle.withFunnyMoments:
                    /*finished.pitch = 1.78F;*/
                    finished.pitch = 1F;
                    break;
            }
        }
    }

    public void _HandleSamplesBeforeStart()
    {
        float[] partOne = new float[_micPosOnStartRecord];
        samplesBeforeStart.clip.GetData(partOne, 0);

        var samplesLength = samplesBeforeStart.clip.samples * samplesBeforeStart.clip.channels;
        var secondPartLength = samplesLength - _micPosOnStartRecord;

        print("SamplesLength: " + samplesLength);
        print("Second part Length: " + secondPartLength);

        float[] partTwo = new float[secondPartLength];
        samplesBeforeStart.clip.GetData(partTwo, _micPosOnStartRecord);

        samplesBeforeStart.clip.SetData(partTwo, 0);
        samplesBeforeStart.clip.SetData(partOne, secondPartLength);

        float[] result = new float[numOfSamplesBeforeStart];
        samplesBeforeStart.clip.GetData(result, _maxFreq - numOfSamplesBeforeStart);

        samplesBeforeStart.clip = AudioClip.Create("SamplesBeforeStart_" + _numOfClips,
            numOfSamplesBeforeStart * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);

        samplesBeforeStart.clip.SetData(result, 0);
    }

    float GetAveragedVolume()
    {
        float[] data = new float[amountSamples];
        float a = 0;

        if (micro.isPlaying)
            micro.GetOutputData(data, 0);
        else if (voice.isPlaying)
            voice.GetOutputData(data, 0);

        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }

        return a / amountSamples;
    }

    void TakeSecFromMic()
    {
        samplesBeforeStart.clip = AudioClip.Create("SamplesBeforeStart_" + _numOfClips, micro.clip.samples * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);
        float[] data = new float[micro.clip.samples * micro.clip.channels];
        micro.clip.GetData(data, 0);
        samplesBeforeStart.clip.SetData(data, 0);
    }

    void RecordSamplesAfterVoice()
    {
        _samplesAfterEnd = AudioClip.Create("SamplesAfterEnd" + _numOfClips, numOfSamplesAfterEnd * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);

        float[] data = new float[numOfSamplesAfterEnd];
        micro.clip.GetData(data, _micPos);
        _samplesAfterEnd.SetData(data, 0);
    }

    void RecordWithSamples()
    {
        visualization.color = Color.red;

        var micPos = Microphone.GetPosition(null);
        var diff = micPos - _lastSample;

        if (diff > 0)
        {
            float[] data = new float[diff * micro.clip.channels];
            micro.clip.GetData(data, _lastSample);
            voice.clip.SetData(data, _voiceOffSet);
            _voiceOffSet += diff;
        }

        _lastSample = micPos;
    }


    public void ChangeTreshold(GameObject go)
    {
        switch (go.name)
        {
            case "StartTresholdUP":
                startRecordThreshold++;
                break;
            case "StartTresholdDOWN":
                startRecordThreshold--;
                break;
            case "EndTresholdUP":
                endRecordThreshold++;
                break;
            case "EndTresholdDOWN":
                endRecordThreshold--;
                break;
        }
    }

    public void ChangePitch()
    {
        onlyFunnyVoice = !onlyFunnyVoice;
    }

    public void HandleFromClip()
    {
        switch (voiceHandle)
        {
            case VoiceHandle.onlyRandPitch:
                if (onlyFunnyVoice)
                {
                    _randPitch = Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                }
                else
                {
                    var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

                    _randPitch = randNum == 0
                        ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                        : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                }

                finished.time = 0;
                finished.pitch = _randPitch;
                break;
            case VoiceHandle.reversePlaying:

                if (onlyFunnyVoice)
                {
                    _randPitch = Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                }
                else
                {
                    var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

                    _randPitch = randNum == 0
                        ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                        : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
                }

                _randPitch = -_randPitch;

                finished.pitch = _randPitch;
                finished.time = finished.clip.length;
                break;
            case VoiceHandle.withFunnyMoments:

                break;
        }

        finished.mute = false;
        finished.loop = false;
        finished.Play();
        _recordIsPlaying = true;
    }

    public void ChangeVoiceHandle(string name)
    {
        if (name == "PlayReverse")
        {
            if (voiceHandle == VoiceHandle.reversePlaying)
            {
                voiceHandle = VoiceHandle.onlyRandPitch;
            }
            else if (voiceHandle == VoiceHandle.onlyRandPitch)
            {
                voiceHandle = VoiceHandle.reversePlaying;
            }
            else if (voiceHandle == VoiceHandle.withFunnyMoments)
            {
                withFunnyMomentsToggle.value = false;
                voiceHandle = VoiceHandle.reversePlaying;
            }
        }
        else if (name == "WithFunnyMoments")
        {
            if (voiceHandle == VoiceHandle.withFunnyMoments)
            {
                voiceHandle = VoiceHandle.onlyRandPitch;
            }
            else if (voiceHandle == VoiceHandle.onlyRandPitch)
            {
                voiceHandle = VoiceHandle.withFunnyMoments;
            }
            else if (voiceHandle == VoiceHandle.reversePlaying)
            {
                playReverseToggle.value = false;
                voiceHandle = VoiceHandle.withFunnyMoments;
            }
        }
    }

    private void _PlayFunnyMoment()
    {
        _finishedTimeStop = finished.time;
        finished.Stop();
        funnyMoment.clip = funnyMoments[Random.Range(0, 1)];
        funnyMoment.Play();
    }

    private void _ContinueFinishedClip()
    {
        _funnyMomentTime = false;
        finished.Play();
        finished.time = _finishedTimeStop;
    }

    /*public void PlayFunnyMoment()
    {
        finished.Stop();
        funnyMoment.clip = funnyMoments[0/*Random.Range(0, funnyMoments.Count + 1)#1#];
        funnyMoment.Play();
    }*/

    /*private void GetAverageLoudnessOnRange()
    {
        funnyMoment.clip = AudioClip.Create("SamplesBeforeStart_" + _numOfClips, micro.clip.samples * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);
        float[] data = new float[micro.clip.samples * micro.clip.channels];
        micro.clip.GetData(data, 0);
        funnyMoment.clip.SetData(data, 0);

        float[] partOne = new float[_micPosOnStartRecord];
        funnyMoment.clip.GetData(partOne, 0);

        var samplesLength = funnyMoment.clip.samples * funnyMoment.clip.channels;
        var secondPartLength = samplesLength - _micPosOnStartRecord;

        print("SamplesLength: " + samplesLength);
        print("Second part Length: " + secondPartLength);

        float[] partTwo = new float[secondPartLength];
        funnyMoment.clip.GetData(partTwo, _micPosOnStartRecord);

        funnyMoment.clip.SetData(partTwo, 0);
        funnyMoment.clip.SetData(partOne, secondPartLength);

        float[] result = new float[numOfSamplesBeforeStart];
        funnyMoment.clip.GetData(result, _maxFreq - numOfSamplesBeforeStart);

        funnyMoment.clip = AudioClip.Create("funnyMoment_" + _numOfClips,
            numOfSamplesBeforeStart * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);

        funnyMoment.clip.SetData(result, 0);

        float[] avLoud = new float[numOfGetSamplesForAvLoudness];
        funnyMoment.clip.GetData(avLoud, _micSamples - numOfGetSamplesForAvLoudness);

        float sumOfSamples = 0;

        for (int i = 0; i < avLoud.Length; i++)
        {
            sumOfSamples += Mathf.Abs(avLoud[i]);
        }

        _averageLoudness = sumOfSamples / avLoud.Length;
    }*/
}
