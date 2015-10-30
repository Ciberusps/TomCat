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
    public float sensitivity = 100;

    public UISprite visualization;

    public AudioSource micro;
    public AudioSource voice;
    public AudioSource finished;

    private bool _recordVoice = false;
    private float loudness;
    private int amountSamples = 256; //increase to get better average, but will decrease performance. Best to leave it
    private string _MicroLogString;
    private string _RecordLogString;
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
        micro.clip = Microphone.Start(null, true, 10, _maxFreq);//Starts recording
        while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
        micro.mute = true;
        micro.Play(); // Play the audio source!

        _micSamples = micro.clip.samples * micro.clip.channels;
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

        /*if (!_recordIsPlaying && inputVoice == InputVoice.onUpdate)
        {
            /*if (recordType == Record.withSamples)
            {
                if (loudness != 0 && loudness > startRecordThreshold)
                {
                    RecordWithSamples();
                    visualization.color = Color.red;
                    _recordVoice = true;
                }

                if (loudness != 0 && loudness < endRecordThreshold && _recordVoice)
                {
                    _recordVoice = false;
                    visualization.color = Color.white;
                    _PlayVoiceRecord();
                }

            }
            else#1#
            if (recordType == Record.byStartAndEndMicro)
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
        }*/
    }

    private void FixedUpdate()
    {
        if (!finished.isPlaying)
            _recordIsPlaying = false;

        if (!_recordIsPlaying && inputVoice == InputVoice.onUpdate)
            if (recordType == Record.withSamples)
            {
                /*if (voice.isPlaying == false)
                {*/
                if (loudness != 0 && loudness > startRecordThreshold)
                {
                    if (!_recordVoice)
                    {
                        _numOfClips++;
                        voice.clip = AudioClip.Create("MyClip_" + _numOfClips, _micSamples, 1, _maxFreq, false, false);
                        _voiceStartTime = Time.time;
                        //                            _lastSample = 0;
                        _lastMicPos = Microphone.GetPosition(null);
                        _lastSample = _lastMicPos;
                        _voiceOffSet = 0;
                        _recordVoice = true;
                    }

                    RecordWithSamples();
                }

                if (loudness != 0 && loudness < endRecordThreshold && _recordVoice)
                {
                    _recordVoice = false;
                    _voiceEndTime = Time.time;
                    visualization.color = Color.white;
                    _PlayVoiceRecord();
                }
                /*}*/
            }
    }

    void OnGUI()
    {
        _MicroLogString =
            "                     MICRO"
            + "\n Loudness: " + loudness
            + "\n AvVol: " + GetAveragedVolume()
            + "\n sensitivity: " + sensitivity
            + "\n sourceVol: " + (sourceVolume / 10)

            + "\n Channels: " + micro.clip.channels
            + "\n Samples: " + micro.clip.samples;
        if (voice.clip != null)
            _MicroLogString += "\nVoice clip: " + voice.clip.length;

        _RecordLogString =
            "                     Samples"
            + "\n Record is playing: " + _recordIsPlaying
            + "\n Record voice: " + _recordVoice
            + "\n Voice start: " + _voiceStartTime
            + "\n Voice end: " + _voiceEndTime
            + "\n Last Sample: " + _lastSample
            + "\n Microphone pos: " + Microphone.GetPosition(null)
            + "\n Last mic pos: " + _lastMicPos
            + "\n Mic samples(samp * channels): " + _micSamples;


        GUI.TextArea(new Rect(10, 10, 180, 300), _MicroLogString);
        GUI.TextArea(new Rect(Screen.width - 190, 10, 180, 300), _RecordLogString);
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

            /*  voice.mute = false;
              voice.loop = false;
              voice.Play();*/

            finished.mute = false;
            finished.loop = false;
            finished.Play();


            //            voice.PlayScheduled(AudioSettings.dspTime);
            //            voice.SetScheduledStartTime(_voiceStartTime);

            //voice.PlayScheduled(0);

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

            var randPitch = randNum == 0
                    ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                    : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

            var voiceLength = _maxFreq * Mathf.CeilToInt(_voiceEndTime - _voiceStartTime) * voice.clip.channels;
            print(voiceLength);

            float[] data = new float[voiceLength];
            voice.clip.GetData(data, 0);

            finished.clip = AudioClip.Create("Finished_" + _numOfClips, voiceLength, voice.clip.channels, _maxFreq, false, false);
            finished.clip.SetData(data, 0);

            finished.pitch = randPitch;

            /*Запись во 2ой
            var randNum = Random.Range(0, 10) > 5 ? 0 : 1;

            var randPitch = randNum == 0
                    ? Random.Range((float)pitchSlowBound.x, pitchSlowBound.y)
                    : Random.Range((float)pitchFastBound.x, pitchFastBound.y);

            voice.pitch = randPitch;*/
        }

        //        micro.pitch = randPitch;
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

    void RecordWithSamples()
    {
        /* //        float[] data = new float[micro.clip.samples];
         float[] data = new float[micro.clip.samples];

         var micPos = Microphone.GetPosition(null);
         var diff = micPos - _lastMicPos;
         _lastMicPos = micPos;

         micro.clip.GetData(data, micPos);
         //voice.clip.SetData(data, _lastSample);
         voice.clip.SetData(data, _lastSample);

         _lastSample += diff;*/
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
}
