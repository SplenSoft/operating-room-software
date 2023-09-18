using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
//using UnityEditor;

public class VersionInfo : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        //        if (Application.platform == RuntimePlatform.IPhonePlayer) {
        //            gameObject.GetComponent<Text> ().text = "Version " + Application.version + "(" + PlayerSettings.iOS.buildNumber + ")";
        //        } else {
        gameObject.GetComponent<Text>().text = "Version " + Application.version;
        //        }
    }


}
