using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Linq;
using System.Collections.ObjectModel;

public class ConfigurationManager : MonoBehaviour
{
    public static ConfigurationManager _instance;
    public bool isDebug = false;
    private Tracker tracker;
    private RoomConfiguration roomConfiguration;

    void Awake()
    {
        if (_instance != null)
            Destroy(this.gameObject);

        _instance = this;

        CreateTracker();

        roomConfiguration.collections = new List<Tracker>();
    }

    Tracker CreateTracker()
    {
        tracker = new Tracker
        {
            objects = new List<TrackedObject.Data>()
        };
        return tracker;
    }

    void RefreshRoomConfig()
    {
        roomConfiguration.collections.Clear();
        roomConfiguration.collections.TrimExcess();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.K))
        {
            SaveRoom("test");
        }

        if (Input.GetKeyUp(KeyCode.L))
        {
            LoadRoom();
        }
    }

    void SaveRoom(string title)
    {
        if (tracker.objects.Count > 0)
        {
            tracker.objects.Clear();
            tracker.objects.TrimExcess();
        }

        TrackedObject[] foundObjects = FindObjectsOfType<TrackedObject>();

        foreach (TrackedObject obj in foundObjects)
        {
            CreateTracker();
            if (obj.transform == obj.transform.root)
            {
                CreateTracker();
                TrackedObject[] temps = obj.transform.GetComponentsInChildren<TrackedObject>();
                foreach (TrackedObject to in temps)
                {
                    tracker.objects.Add(to.GetData());
                }

                roomConfiguration.collections.Add(tracker);
            }
        }

        // ======SAVING JSON=========
        string json = JsonConvert.SerializeObject(roomConfiguration, new JsonSerializerSettings
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

    public void LoadRoom()
    {
        string folder = Application.dataPath + "/Configs/";
        string configName = "test.json";
        Debug.Log($"Loading Config at /{folder}/{configName}");
        string path = Path.Combine(folder, configName);

        if (File.Exists(path))
        {
            CreateTracker();
            string json = File.ReadAllText(path);
            roomConfiguration = JsonConvert.DeserializeObject<RoomConfiguration>(json);
            GenerateRoomConfig();
        }
    }

    private string attachPointGUID = "C9614497-545A-414A-8452-3B7CF50EE43E";
    void GenerateRoomConfig()
    {
        // insert room size logic here

        foreach (Tracker t in roomConfiguration.collections)
        {
            List<AttachmentPoint> newPoints = new List<AttachmentPoint>();

            //t.objects.Reverse();
            foreach (TrackedObject.Data to in t.objects)
            {
                GameObject go = null;
                if (to.global_guid != attachPointGUID && to.global_guid != "" && to.global_guid != null) // if it is not an AttachPoint, we need to place the Selectable
                {
                    go = Instantiate(ObjectMenu.Instance.GetPrefabByGUID(to.global_guid), to.pos, to.rot);
                    go.name = to.instance_guid;
                    go.GetComponent<Selectable>().guid = to.instance_guid;
                }

                if (to.parent != null)
                {
                    if (to.global_guid == null || to.global_guid == "") // embedded selectable component
                    {
                        Debug.Log($"Finding: {to.parent}");
                        go = GameObject.Find(to.parent);
                        go.GetComponent<Selectable>().guid = to.instance_guid;
                    }
                    else if (to.global_guid == attachPointGUID) // attachment point component
                    {
                        Debug.Log($"Finding: {to.parent}");
                        GameObject myself = GameObject.Find(to.parent);
                        myself.GetComponent<AttachmentPoint>().guid = to.instance_guid;
                        newPoints.Add(myself.GetComponent<AttachmentPoint>());
                    }
                    else // selectable attached to an attachment point
                    {
                        AttachmentPoint ap = newPoints.Single(s => s.guid == to.parent);
                        ap.SetAttachedSelectable(go.GetComponent<Selectable>());
                        go.transform.SetParent(ap.gameObject.transform);
                    }
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
