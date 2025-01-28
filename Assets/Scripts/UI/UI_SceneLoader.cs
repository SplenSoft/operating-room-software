using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class UI_SceneLoader : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        FindObjectOfType<objectToTurnOff>(true).gameObject.SetActive(false); //Temp fix
        videoPlayer.loopPointReached += CheckVideoEnd;
    }

    private void CheckVideoEnd(VideoPlayer source)
    {
        SceneManager.LoadScene("Start");
    }

  
}
