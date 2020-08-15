using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TextSpeech;
using UnityEngine.Android;

public class VoiceRecognition : MonoBehaviour
{
    [SerializeField] private Text uiText;

    void Start()
    {
        Setup(GameConstants.LANG_CODE);

        // register callbacks
#if UNITY_ANDROID
        SpeechToText.instance.onPartialResultsCallback = OnPartialSpeechResult;
#endif
        SpeechToText.instance.onResultCallback = OnFinalSpeechResult;

        //TextToSpeech.instance.onStartCallBack = OnSpeakStart;
        //TextToSpeech.instance.onDoneCallback = OnSpeakStop;

        CheckPermission();
    }

    void Setup(string code)
    {
        //TextToSpeech.instance.Setting(code, 1, 1);
        SpeechToText.instance.Setting(code);   
    }

    // check users permission to use mic
    void CheckPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    #region TextToSpeech
    // might use in the future

    public void StartSpeaking(string message)
    {
        TextToSpeech.instance.StartSpeak(message);
    }

    public void StopSpeaking()
    {
        TextToSpeech.instance.StopSpeak();
    }

    public void OnSpeakStart()
    {
        Debug.Log("Started speaking..");
    }
    public void OnSpeakStop()
    {
        Debug.Log("Stopped speaking..");
    }
    #endregion

    #region SpeechToText
    public void StartListening()
    {
        SpeechToText.instance.StartRecording();
    }

    public void StopListening()
    {
        SpeechToText.instance.StopRecording();
    }

    public void OnFinalSpeechResult(string result)
    {
        uiText.text = result;
    }

    public void OnPartialSpeechResult(string result)
    {
        uiText.text = result;
    }

    #endregion





}
