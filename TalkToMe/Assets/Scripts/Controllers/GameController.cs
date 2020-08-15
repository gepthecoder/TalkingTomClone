using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum PlayerState { Idle=0,
                        Listening,
                                Talking, }


[RequireComponent(typeof(AudioSource))]
public class GameController : MonoBehaviour
{
    private Animator _anime;
    private PlayerState _state = PlayerState.Idle;
    private AudioSource _aSource;

    private float[] _clipsData;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag(GameConstants.playerTag);
        if(player != null) { _anime = player.GetComponent<Animator>(); }
        _aSource = GetComponent<AudioSource>();
        _clipsData = new float[GameConstants.SampleDataLength]; /*1024*/
        Idle();
    }

    void Update()
    {
        /*
         * 1 -> if in IDLE and the VOLUME  is ABOVE THRESOLD
         * we want to switch to LISTEN
         
         */
        if(_state == PlayerState.Idle && IsVolumeAboveThreshold())
        {
            SwitchState();
        }
    }
    
    private bool IsVolumeAboveThreshold()
    {
        if (_aSource.clip == null) { return false; }

        _aSource.clip.GetData(_clipsData, _aSource.timeSamples); // read 1024 samples, which is above thresold
        var clipLoudness = 0f;
        foreach (var sample in _clipsData)
        {
            clipLoudness += Mathf.Abs(sample);
        }
        clipLoudness /= GameConstants.SampleDataLength;
        Debug.Log("Clip loudness: " + clipLoudness);
        // we record 1 sec of audio and we want to analyse and detect the clip loudness is almost finished

        return clipLoudness > GameConstants.SoundThreshold; /*0.025f*/
    }

    private void SwitchState()
    {
        switch (_state)
        {
            case PlayerState.Idle:
                _state = PlayerState.Listening;
                Listen();
                break;
            case PlayerState.Listening:
                _state = PlayerState.Talking;
                Talk();
                break;
            case PlayerState.Talking:
                _state = PlayerState.Idle;
                Idle();
                break;
        }
    }

    private void Idle()
    {
        /*
         * 1 -> Play Idle Animation
         * 2 -> Reset sound after playback
         * 3 -> Contineously record the sound with lowest duration possible
        */

        if(_anime != null)
        {
            _anime.SetTrigger(GameConstants.AnimeIdle);
            if(_aSource.clip != null)
            {// if playback happened
                _aSource.Stop();
                _aSource.clip = null;
            }
            _aSource.clip = Microphone.Start
                (GameConstants.MicrophoneDeviceName, true, GameConstants.IdleRecordingLength, GameConstants.RecordingFrequency);
        }
    }

    private void Listen()
    {
        /*
         * 1 -> Play Listen Animation
         * 2 -> Start recording user sound 
         * 3 -> Transition to talking state after some time
        */

        if(_anime != null)
        {
            _anime.SetTrigger(GameConstants.AnimeListen);
            _aSource.clip = Microphone.Start(GameConstants.MicrophoneDeviceName, false, GameConstants.RecordingLength, GameConstants.RecordingFrequency);
            Invoke("SwitchState", GameConstants.RecordingLength);
        }
    }

    private void Talk()
    {
        /*
         * 1 -> Play Talk Animation
         * 2 -> Stop Recording
         * 3 -> Play recorded sound
         * 4 -> Transition to Idle after the playback
        */

        if (_anime != null)
        {
            _anime.SetTrigger(GameConstants.AnimeTalk);

            Microphone.End(null);
            if(_aSource.clip != null) { _aSource.Play(); }
            Invoke("SwitchState", GameConstants.RecordingLength);
        }
    }

}
