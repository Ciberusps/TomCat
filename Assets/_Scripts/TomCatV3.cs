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

    public enum Record
    {
        withSamples,
        byStartAndEndMicro
    }

    public InputVoice inputVoice = InputVoice.buttons;
    public Record recordType = Record.byStartAndEndMicro;
    public float startRecordThreshold = 1F;
    public float endRecordThreshold = 1F;
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
    public float notEndRecordTime = 0.5F;
    public float notRecordingTime = 0.3F;
    public bool onlyFunnyVoice = true;
    public UISprite visualization;

    public AudioSource micro;
    public AudioSource voice;
    public AudioSource finished;
    public AudioSource samplesBeforeStart;
    public UILabel startTresholdLabel;
    public UILabel endTresholdLabel;

    //    private AudioClip samplesBeforeStart;
    private AudioClip _samplesAfterEnd;
    private bool _recordVoice = false;
    private float loudness;
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
    private int _lastMicPos;
    private float _voiceStartTime;
    private float _voiceEndTime;
    private int _numOfClips;
    private int _voiceLength;
    private int _micPos;
    private float _averageLoudnessOfRecord;
    private int _loudnessGetCount;
    private float _rmsValue;
    private const float REFVALUE = 0.1f;
    private float _dbValue = 0;
    private float _notEndRecordTimer = 0;
    private float _samplesBeforeStartTime;
    private int _lastSampleBeforeVoice = 0;
    private int _samplesBeforeStartVoiceOffSet = 0;
    private bool _recordSamplesBeforeStart = false;
    private int _lastMicPosBeforeStart = 0;
    private float _notRecordingTimer = 0;
    private bool _notRecording = false;

    void Start()
    {

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

        //        samplesBeforeStart.clip = AudioClip.Create("SamplesBeforeStart_" + _numOfClips, numOfSamplesBeforeStart * micro.clip.channels, micro.clip.channels, _maxFreq, false, false);

        _micSamples = micro.clip.samples * micro.clip.channels;
    }

    void Update()
    {
        /*if (!_recordVoice)
            micro.volume = (sourceVolume / 100);
        else
            voice.volume = (sourceVolume / 100);

        loudness = GetAveragedVolume() * sensitivity * (sourceVolume / 10);*/
    }

    private void FixedUpdate()
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

        _micPos = Microphone.GetPosition(null);

        if (!finished.isPlaying)
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

        if (_recordIsPlaying)
            visualization.color = Color.green;
        else if (!_recordVoice)
            visualization.color = Color.white;


        /*if (!_recordVoice && !_recordIsPlaying)
        {
            if (_recordSamplesBeforeStart)
            {
                RecordSamplesBeforeVoice();
            }
            else if (!_recordSamplesBeforeStart)
            {
                _lastMicPos = _micPos;
                _lastSampleBeforeVoice = _lastMicPos;
                _samplesBeforeStartVoiceOffSet = 0;
                _recordSamplesBeforeStart = true;   
                RecordSamplesBeforeVoice();
            }
        }*/


        if (!_recordIsPlaying && inputVoice == InputVoice.onUpdate)
            if (recordType == Record.withSamples)
            {
                /*if (voice.isPlaying == false)
                {*/
                if (_recordVoice)
                    RecordWithSamples();
                if (loudness != 0 && loudness > startRecordThreshold)
                {
                    _notEndRecordTimer = 0;

                    if (!_recordVoice)
                    {
                        TakeSecFromMic();
                        _numOfClips++;
                        voice.clip = AudioClip.Create("MyClip_" + _numOfClips, _maxFreq * micro.clip.channels * voiceClipLength, micro.clip.channels, _maxFreq, false, false);
                        _voiceStartTime = Time.time;
                        //                            _lastSample = 0;
                        _lastMicPos = _micPos;
                        _lastSample = _lastMicPos;
                        _recordSamplesBeforeStart = false;

                        _voiceOffSet = 0;
                        _recordVoice = true;
                    }

                    /*_loudnessGetCount++;
                    _averageLoudnessOfRecord += loudness;*/

                    //                    RecordWithSamples();
                }

                if (loudness != 0 && loudness < endRecordThreshold)
                {
                    if (_recordVoice)
                    {
                        if (_notEndRecordTimer == 0)
                            _notEndRecordTimer = Time.fixedTime + notEndRecordTime;
                        //                        _notEndRecordTimer = Time.fixedDeltaTime;

                        if (_notEndRecordTimer <= Time.fixedTime)
                        {
                            _notEndRecordTimer = 0;
                            _recordVoice = false;
                            _voiceEndTime = Time.time;
                            visualization.color = Color.white;
                            RecordSamplesAfterVoice();
                            _PlayVoiceRecord();
                            _averageLoudnessOfRecord = _averageLoudnessOfRecord / _loudnessGetCount;
                        }

                        //                        RecordWithSamples();
                    }
                }
                /*}*/
            }
    }

    void OnGUI()
    {
        _MicroLogString =
            "                     MICRO"
            + "\n Loudness: " + loudness
            + "\n DB: " + _dbValue
            + "\n AvVol: " + GetAveragedVolume()
            + "\n sensitivity: " + sensitivity
            + "\n sourceVol: " + (sourceVolume / 10)
            + "\n Channels: " + micro.clip.channels
            + "\n Samples: " + micro.clip.samples
            + "\n Average loudness of last record: " + _averageLoudnessOfRecord
            + "\n Time: " + Time.time
            + "\n Timer: " + _notEndRecordTimer;
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
            + "\n Last mic pos: " + _lastMicPos
            + "\n Mic samples(samp * channels): " + _micSamples
            + "\n Voice record length : " + _voiceLength
            + "\n Voice record length(sec) : " + _voiceLength / _maxFreq
            + "\n Samples before start: " + _samplesBeforeStartVoiceOffSet
            + "\n Last mic pos before start: " + _lastMicPosBeforeStart;

        /*_TreasholdsString =
            "                     Tresholds"
            + "\n Treshold start: " + startRecordThreshold
            + "\n Treshold end: " + endRecordThreshold;*/

        startTresholdLabel.text = "Start treshold: " + startRecordThreshold.ToString();
        endTresholdLabel.text = "End treshold: " + endRecordThreshold.ToString();

        GUI.TextArea(new Rect(10, 10, 230, 300), _MicroLogString);
        GUI.TextArea(new Rect(Screen.width - 240, 10, 230, 300), _RecordLogString);
        //        GUI.TextArea(new Rect(10, Screen.width/2, 230, 300), _TreasholdsString);
        //        GUI.TextArea(new Rect(10, 10, 180, 300), _MicroLogString);
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

            Debug.LogWarning("Start record " + loudness);
        }
        else
        {
            _recordVoice = true;

            voice.loop = true; // Set the AudioClip to loop
            voice.mute = true;

            RecordWithSamples();

            visualization.color = Color.red;

            Debug.LogWarning("Start record " + loudness);
        }
    }

    public void _EndVoiceRecord()
    {
        _recordVoice = false;
        voice.Stop(); //Stops the audio
        Microphone.End(null); //Stops the recording of the device    

        _PlayVoiceRecord();

        visualization.color = Color.green;
        Debug.LogWarning("End record " + loudness);
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
            /*samplesBeforeStart.mute = false;
            samplesBeforeStart.loop = false;
            samplesBeforeStart.Play();*/
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

            var randPitch = randNum == 0
                    ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                    : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

            voice.pitch = randPitch;
        }
        else if (recordType == Record.withSamples)
        {
            //             ЗАпись в 3ий

            var randNum = Random.Range(0, 10) > 5 ? 0 : 1;
            float randPitch;

            if (!onlyFunnyVoice)
            {
                randPitch = randNum == 0
                        ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                        : Random.Range((float)pitchFastBound.x, pitchFastBound.y);
            }
            else
            {
                randPitch = Random.Range((float)pitchFastBound.x, pitchFastBound.y);
            }

            _voiceLength = Mathf.CeilToInt(_maxFreq * (_voiceEndTime - _voiceStartTime) * voice.clip.channels);
            print(_voiceLength);

            _HandleSamplesBeforeStart();

            float[] dataVoice = new float[_voiceLength];
            voice.clip.GetData(dataVoice, 0);

            float[] dataSamplesBeforeStart = new float[numOfSamplesBeforeStart];
            samplesBeforeStart.clip.GetData(dataSamplesBeforeStart, 0);

            /*float[] dataSamplesAfterEnd = new float[numOfSamplesAfterEnd];
            _samplesAfterEnd.GetData(dataSamplesAfterEnd, 0);*/
            Destroy(finished.clip);
            finished.clip = AudioClip.Create("Finished_" + _numOfClips, _voiceLength + numOfSamplesBeforeStart /*+ numOfSamplesAfterEnd*/, voice.clip.channels, _maxFreq, false, false);
            finished.clip.SetData(dataSamplesBeforeStart, 0);
            finished.clip.SetData(dataVoice, numOfSamplesBeforeStart);
            //            finished.clip.SetData(dataSamplesAfterEnd, numOfSamplesBeforeStart + _voiceLength);
            finished.pitch = randPitch;

            float[] samples = new float[finished.clip.samples * finished.clip.channels];
            finished.clip.GetData(samples, 0);
            int i = 0;
            while (i < samples.Length)
            {
                samples[i] = samples[i] * volume;
                ++i;
            }
            finished.clip.SetData(samples, 0);

            Destroy(samplesBeforeStart.clip);
            Destroy(voice.clip);
            Destroy(_samplesAfterEnd);
            /*Запись во 2ой
            var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

            var randPitch = randNum == 0
                    ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                    : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

            voice.pitch = randPitch;*/
        }

        //        micro.pitch = randPitch;
    }

    public void _HandleSamplesBeforeStart()
    {
        /*float[] partOne = new float[_lastSampleBeforeVoice];
        samplesBeforeStart.clip.GetData(partOne, 0);

        var samplesLength = samplesBeforeStart.clip.samples * samplesBeforeStart.clip.channels;
        var secondPartLength = samplesLength - _lastSampleBeforeVoice;

        float[] partTwo = new float[secondPartLength];
        samplesBeforeStart.clip.GetData(partTwo, _lastSampleBeforeVoice);

        samplesBeforeStart.clip.SetData(partTwo, 0);
        samplesBeforeStart.clip.SetData(partOne, secondPartLength);*/


        /*float[] partOne = new float[_lastMicPosBeforeStart];
        samplesBeforeStart.clip.GetData(partOne, 0);

        var samplesLength = samplesBeforeStart.clip.samples * samplesBeforeStart.clip.channels;
        var secondPartLength = samplesLength - _lastMicPosBeforeStart;

        float[] partTwo = new float[secondPartLength];
        samplesBeforeStart.clip.GetData(partTwo, _lastMicPosBeforeStart);

        samplesBeforeStart.clip.SetData(partTwo, 0);
        samplesBeforeStart.clip.SetData(partOne, secondPartLength);*/

        float[] partOne = new float[_lastMicPos];
        samplesBeforeStart.clip.GetData(partOne, 0);

        var samplesLength = samplesBeforeStart.clip.samples * samplesBeforeStart.clip.channels;
        var secondPartLength = samplesLength - _lastMicPos;

        print("SamplesLength: " + samplesLength);
        print("Second part Length: " + secondPartLength);

        float[] partTwo = new float[secondPartLength];
        samplesBeforeStart.clip.GetData(partTwo, _lastMicPos);

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

    void RecordSamplesBeforeVoice()
    {
        /*var micPos = Microphone.GetPosition(null);
        var diff = micPos - _lastSampleBeforeVoice;

        if (diff > 0)
        {
            float[] data = new float[diff * micro.clip.channels];
            micro.clip.GetData(data, _lastSampleBeforeVoice);
            samplesBeforeStart.clip.SetData(data, _samplesBeforeStartVoiceOffSet);
            _samplesBeforeStartVoiceOffSet += diff;
            if (_samplesBeforeStartVoiceOffSet > _maxFreq)
                _samplesBeforeStartVoiceOffSet = 0;
        }

        _lastSampleBeforeVoice = micPos;*/
        var micPos = Microphone.GetPosition(null);
        var diff = micPos - _lastSampleBeforeVoice;

        if (diff > 0)
        {
            float[] data = new float[diff * micro.clip.channels];
            micro.clip.GetData(data, micPos);
            samplesBeforeStart.clip.SetData(data, _samplesBeforeStartVoiceOffSet);
            _lastMicPosBeforeStart = micPos;
            _samplesBeforeStartVoiceOffSet += diff;
            if (_samplesBeforeStartVoiceOffSet >= numOfSamplesBeforeStart)
                _samplesBeforeStartVoiceOffSet = 0;
        }
        _lastSampleBeforeVoice = micPos;
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
}
