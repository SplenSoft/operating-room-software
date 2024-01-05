using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ConfigurationManager : MonoBehaviour
{
    public static ConfigurationManager _instance;
    public bool isDebug = false;
    private Tracker tracker;

    void Awake()
    {
        if(_instance != null)
            Destroy(this.gameObject);

        _instance = this;

        CreateTracker();
    }

    Tracker CreateTracker()
    {
        tracker = new Tracker
        {
            objects = new List<TrackedObject.Data>()
        };
        return tracker;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.K))
        {
            TestSaveData("test");
        }

        if (Input.GetKeyUp(KeyCode.L))
        {
            LoadConfig();
        }
    }

    void TestSaveData(string title)
    {
        if (tracker.objects.Count > 0)
        {
            tracker.objects.Clear();
            tracker.objects.TrimExcess();
        }

        TrackedObject[] foundObjects = FindObjectsOfType<TrackedObject>();

        foreach (TrackedObject obj in foundObjects)
        {
            tracker.objects.Add(obj.GetData());
        }

        // ======SAVING JSON=========
        string json = JsonConvert.SerializeObject(tracker, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        string folder = Application.dataPath + $"/Configs/";
        string configName = title.Replace(" ", "_") + ".json";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string path = Path.Combine(folder, configName);

        //Overwrite data
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.WriteAllText(path, json);
        Debug.Log($"Saved Config: {path}");
    }

    public void LoadConfig()
    {
        string folder = Application.dataPath + "/Configs/";
        string configName = "test.json";
        Debug.Log($"Loading Config at /{folder}/{configName}");
        string path = Path.Combine(folder, configName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            tracker = JsonConvert.DeserializeObject<Tracker>(json);
            GenerateConfig();
        }
    }

    void GenerateConfig()
    {

    }

    public static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

}
