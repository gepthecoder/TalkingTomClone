using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum PlayerState { Idle=0,
                        Listening,
                                Talking,
                                    Walking,
                                            Chilling,
                                                    Returning, }


[RequireComponent(typeof(AudioSource))]
public class GameController : MonoBehaviour
{
    #region Singleton
    private static GameController instance;
    public static GameController Instance
    {
        get
        {
            if (instance == null) { instance = FindObjectOfType(typeof(GameController)) as GameController; }
            return instance;
        }
        set { instance = value; }
    }
    #endregion

    [Range(0, 10)] [SerializeField] private float rotationSpeed = 3f;
    [Range(0, 10)] [SerializeField] private float movementSpeed = .2f;

    [SerializeField] private Transform sitDownPos;
    [SerializeField] private Transform defaultPos;

    private GameObject PLAYER;

    private Animator _anime;
    private PlayerState _state = PlayerState.Idle;
    private AudioSource _aSource;

    private VoiceRecognition voiceRecognito;

    private float[] _clipsData;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        voiceRecognito = GetComponent<VoiceRecognition>();

        PLAYER = GameObject.FindGameObjectWithTag(GameConstants.playerTag);
        if (PLAYER != null)
        {
            _anime = PLAYER.GetComponent<Animator>();
        }

        _aSource = GetComponent<AudioSource>();
        _clipsData = new float[GameConstants.SampleDataLength]; /*1024*/

        Idle();
    }

    void Update()
    {
        CheckIfHit();
        COMMAND_HANDLER();

        if (_state == PlayerState.Walking)
        {
            // turn
            TurnToChair();
            // move
            MoveToChair();
            Walk(true);
        }
        
        if(_state == PlayerState.Chilling)
        {
            // face camera
            TurnToDefault();
            // sit down
            Walk(false);
        }

        if(_state == PlayerState.Returning)
        {
            StandUp(false);

            ReturnBackToDefaultPos();
            _anime.SetBool(GameConstants.AnimeTransition, true);

            if (isPlayerOnDefaultPos())
            {
                StandUp(true);
                _state = PlayerState.Idle;
                PLAYER.transform.rotation = Quaternion.Slerp(PLAYER.transform.rotation, defaultPos.rotation, 1.5f);
                _anime.SetBool(GameConstants.AnimeTransition, false);
            }
        }

        /*
         * 1 -> if in IDLE and the VOLUME  is ABOVE THRESOLD
         * we want to switch to LISTEN
         */
        if ((_state == PlayerState.Idle || _state == PlayerState.Chilling) && IsVolumeAboveThreshold())
        {
            SwitchState();
        }
    }

    public void TRIGGER_STATE(int state)
    {
        switch (state)
        {
            case (int)PlayerState.Chilling:
                _state = PlayerState.Chilling;
                break;

            default:
                Debug.Log("Assertion error");
                break;
        }
    }

    void TurnToChair(/*bool chair*/)
    {
        //if (chair)
        //{
            Vector3 dir = sitDownPos.position - PLAYER.transform.position;
            if(dir == Vector3.zero) { return; }
            Quaternion r = Quaternion.LookRotation(dir);
            float step = rotationSpeed * Time.deltaTime;
            PLAYER.transform.rotation = Quaternion.Slerp(PLAYER.transform.rotation, r, step);
        //}
        //else
        //{
        //    Vector3 dir = defaultPos.position - PLAYER.transform.position;
        //    if (dir == Vector3.zero) { return; }

        //    Quaternion r = Quaternion.LookRotation(dir);
        //    float step = rotationSpeed * Time.deltaTime;
        //    PLAYER.transform.rotation = Quaternion.Slerp(PLAYER.transform.rotation, r, step);
        //}
    }

    void TurnToDefault(/*bool chair*/)
    {
        Vector3 dir = defaultPos.position - PLAYER.transform.position;
        Debug.Log("Look rotation: " + dir);
        if (dir == Vector3.zero) { return; }

        Quaternion r = Quaternion.LookRotation(dir);
        float step = rotationSpeed * Time.deltaTime;
        PLAYER.transform.rotation = Quaternion.Slerp(PLAYER.transform.rotation, r, step);
    }


    void MoveToChair()
    {
        Vector3 cPos = PLAYER.transform.position;
        float step = movementSpeed * Time.deltaTime;
        PLAYER.transform.position = Vector3.MoveTowards(cPos, sitDownPos.position, step);
    }

    void ReturnBackToDefaultPos()
    {
        Vector3 cPos = PLAYER.transform.position;
        float step = movementSpeed * Time.deltaTime;
        PLAYER.transform.position = Vector3.MoveTowards(cPos, defaultPos.position, step);
    }

    private void COMMAND_HANDLER()
    {
        if (voiceRecognito.uiText.text.Contains(GameConstants.COMMAND_SIT_DOWN))
        {
            /*
            * ####### SET STATE TO WALKING
            */
            _state = PlayerState.Walking;
            voiceRecognito.uiText.text = "";
        }
        if (voiceRecognito.uiText.text.Contains(GameConstants.COMMAND_STAND_UP))
        {
            /*
            * ####### SET STATE TO RETURNING
            */
            _state = PlayerState.Returning;
            voiceRecognito.uiText.text = "";
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
        //Debug.Log("Clip loudness: " + clipLoudness);
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
            case PlayerState.Chilling:
                _state = PlayerState.Listening;
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

    private void Walk(bool walk)
    {
        if (_anime != null)
        {
            _anime.SetBool(GameConstants.AnimeWalk, walk);
        }
    }

    private void StandUp(bool isPlayerOnDefaultPos)
    {
        if (_anime != null)
        {
            _anime.SetBool(GameConstants.AnimeStandUp, !isPlayerOnDefaultPos);
        }
    }

    bool isPlayerOnDefaultPos()
    {
        return PLAYER.transform.position == defaultPos.position;
    }

    public void GetPunched()
    {
        _anime.SetTrigger(GameConstants.AnimeGetPunched);
    }

    public void CheckIfHit()
    {
        if (Input.GetMouseButtonDown(0) && isPlayerOnDefaultPos())
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.tag == "Player")
                {
                    GetPunched();
                }
            }
        }
    }
}
