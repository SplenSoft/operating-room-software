using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Linq;

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

    private string attachPointGUID = "C9614497-545A-414A-8452-3B7CF50EE43E";
    void GenerateConfig()
    {
        List<AttachmentPoint> newPoints = new List<AttachmentPoint>();

        tracker.objects.Reverse();
        foreach(TrackedObject.Data to in tracker.objects)
        {
            GameObject go = null;
            if(to.global_guid != attachPointGUID) // if it is not an AttachPoint, we need to place the Selectable
            {
               go = Instantiate(ObjectMenu.Instance.GetPrefabByGUID(to.global_guid), to.pos, to.rot);
               go.name = to.instance_guid;
            }

            if(to.parent != null)
            {
                if(to.global_guid == attachPointGUID)
                {
                    GameObject myself = GameObject.Find(to.parent);
                    myself.GetComponent<AttachmentPoint>().guid = to.instance_guid;
                    newPoints.Add(myself.GetComponent<AttachmentPoint>());
                }
                else
                {
                    AttachmentPoint ap = newPoints.Single(s => s.guid == to.parent);
                    ap.SetAttachedSelectable(go.GetComponent<Selectable>());
                    go.transform.SetParent(ap.gameObject.transform);
                }
            }   
        }
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
