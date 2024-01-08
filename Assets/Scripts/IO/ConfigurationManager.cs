using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

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
        NewRoomSave();
    }

    Tracker CreateTracker()
    {
        tracker = new Tracker
        {
            objects = new List<TrackedObject.Data>()
        };
        return tracker;
    }

    RoomConfiguration NewRoomSave()
    {
        roomConfiguration = new RoomConfiguration
        {
            collections = new List<Tracker>()
        };
        return roomConfiguration;
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

    void SaveConfiguration(string title)
    {
        CreateTracker();

        TrackedObject[] foundObjects = Selectable.SelectedSelectable.transform.root.GetComponentsInChildren<TrackedObject>();

        foreach (TrackedObject obj in foundObjects)
        {
            tracker.objects.Add(obj.GetData());
        }

        //====== SAVING JSON =======
        string json = JsonConvert.SerializeObject(tracker, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        string folder = Application.dataPath + $"/Saved/Configs/";
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

    void SaveRoom(string title)
    {
        CreateTracker();
        NewRoomSave();

        roomConfiguration.roomDimension = RoomSize.Instance.currentDimensions;

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
        string folder = Application.dataPath + $"/Saved/";
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
        Debug.Log($"Saved Room: {path}");
    }

    private string attachPointGUID = "C9614497-545A-414A-8452-3B7CF50EE43E";
    public void LoadRoom()
    {
        string folder = Application.dataPath + "/Saved/";
        string configName = "test.json";
        Debug.Log($"Loading Room at /{folder}/{configName}");
        string path = Path.Combine(folder, configName);

        if (File.Exists(path))
        {
            CreateTracker();
            string json = File.ReadAllText(path);
            roomConfiguration = JsonConvert.DeserializeObject<RoomConfiguration>(json);
            GenerateRoomConfig();
        }
    }

    async void GenerateRoomConfig()
    {
        RoomSize.RoomSizeChanged?.Invoke(roomConfiguration.roomDimension);

        foreach (Tracker t in roomConfiguration.collections)
        {
            List<AttachmentPoint> newPoints = new List<AttachmentPoint>();
            List<TrackedObject> newObjects = new List<TrackedObject>();
            foreach (TrackedObject.Data to in t.objects)
            {
                GameObject go = null;
                if (to.global_guid != attachPointGUID && to.global_guid != "" && to.global_guid != null) // if it is not an AttachPoint, we need to place the Selectable
                {
                    go = Instantiate(ObjectMenu.Instance.GetPrefabByGUID(to.global_guid));
                    go.transform.SetPositionAndRotation(to.pos, to.rot);
                    go.name = to.instance_guid;
                    go.GetComponent<Selectable>().guid = to.instance_guid;
                    LogScale(go.GetComponent<Selectable>(), to);
                    newObjects.Add(go.GetComponent<TrackedObject>());
                }

                if (to.parent != null)
                {
                    if (to.global_guid == null || to.global_guid == "") // embedded selectable component
                    {
                        go = GameObject.Find(to.parent);
                        go.GetComponent<Selectable>().guid = to.instance_guid;
                        LogScale(go.GetComponent<Selectable>(), to);
                        newObjects.Add(go.GetComponent<TrackedObject>());
                        go.transform.rotation = to.rot;
                    }
                    else if (to.global_guid == attachPointGUID) // attachment point component
                    {
                        GameObject myself = GameObject.Find(to.parent);
                        myself.GetComponent<AttachmentPoint>().guid = to.instance_guid;
                        newPoints.Add(myself.GetComponent<AttachmentPoint>());
                    }
                    else // selectable attached to an attachment point
                    {
                        AttachmentPoint ap = newPoints.Single(s => s.guid == to.parent);
                        ap.SetAttachedSelectable(go.GetComponent<Selectable>());
                        go.transform.SetParent(ap.gameObject.transform);
                        go.GetComponent<Selectable>().ParentAttachmentPoint = ap;
                        LogScale(go.GetComponent<Selectable>(), to);
                        newObjects.Add(go.GetComponent<TrackedObject>());
                    }
                }
            }

            newObjects.Reverse();
            foreach (TrackedObject obj in newObjects)
            {
                if (obj.GetScaleLevel() != null)
                {
                    obj.GetComponent<Selectable>().ScaleLevels.ForEach((item) => item.Selected = false);
                    obj.GetScaleLevel().Selected = true;
                }
            }

            await Task.Yield();

            foreach (TrackedObject obj in newObjects)
            {
                if (!string.IsNullOrEmpty(obj.GetComponent<Selectable>().GUID) && obj.transform != obj.transform.root)
                {
                    obj.transform.localPosition = Vector3.zero;
                }
            }
        }

        void LogScale(Selectable s, TrackedObject.Data to)
        {
            if (to.scaleLevel.Selected && to.scaleLevel != s.CurrentScaleLevel)
            {
                s.GetComponent<TrackedObject>().StoreValues(to);
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
