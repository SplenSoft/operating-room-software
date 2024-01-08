using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ConfigurationLoader : MonoBehaviour
{
    void Start()
    {
        if(Directory.Exists(Application.dataPath + "/Saved/Configs/"))
        {
            string[] files = Directory.GetFiles(Application.dataPath + "/Saved/Configs/");
            foreach(string f in files.Where(x => x.EndsWith(".json")))
            {
                ObjectMenu.Instance.AddCustomMenuItem(f);
            }
        }
    }
}
