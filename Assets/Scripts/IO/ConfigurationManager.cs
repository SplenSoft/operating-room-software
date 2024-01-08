using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using Unity.VisualScripting;

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

    public void SaveConfiguration(string title)
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
        string folder = Application.persistentDataPath + $"/Saved/Configs/";
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

        ObjectMenu.Instance.AddCustomMenuItem(path);
    }

    public void SaveRoom(string title)
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
        string folder = Application.persistentDataPath + $"/Saved/";
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

    public async Task<GameObject> LoadConfig(string file)
    {
        Debug.Log($"Loading config file at {file}");

        if (File.Exists(file))
        {
            CreateTracker();
            string json = File.ReadAllText(file);
            tracker = JsonConvert.DeserializeObject<Tracker>(json);
        }

        newPoints = new List<AttachmentPoint>();
        newObjects = new List<TrackedObject>();

        ProcessTrackedObjects(tracker.objects);
        await ResetObjectPositions(newObjects);

        return GetRoot();
    }

    public void LoadRoom(string file)
    {
        Debug.Log($"Clearing default room objects");
        TrackedObject[] existingObjects = FindObjectsOfType<TrackedObject>();
        foreach (TrackedObject to in existingObjects)
        {
            if (to.transform == to.transform.root) Destroy(to.gameObject);
        }

        Debug.Log($"Loading Room at {file}");

        if (File.Exists(file))
        {
            CreateTracker();
            string json = File.ReadAllText(file);
            roomConfiguration = JsonConvert.DeserializeObject<RoomConfiguration>(json);
            GenerateRoomConfig();
        }
    }

    List<TrackedObject> newObjects;
    List<AttachmentPoint> newPoints;
    async void GenerateRoomConfig()
    {
        RoomSize.RoomSizeChanged?.Invoke(roomConfiguration.roomDimension);

        foreach (Tracker t in roomConfiguration.collections)
        {
            newPoints = new List<AttachmentPoint>();
            newObjects = new List<TrackedObject>();

            ProcessTrackedObjects(t.objects);
            await ResetObjectPositions(newObjects);
        }
    }

    void ProcessTrackedObjects(List<TrackedObject.Data> trackedObjects)
    {
        foreach (TrackedObject.Data to in trackedObjects)
        {
            GameObject go = null;
            if (to.global_guid != attachPointGUID && !string.IsNullOrEmpty(to.global_guid)) // if it is not an AttachPoint, we need to place the Selectable
            {
                go = InstantiateObject(to);
                newObjects.Add(go.GetComponent<TrackedObject>());
            }

            if (to.parent != null)
            {
                if (to.global_guid == null || to.global_guid == "") // embedded selectable component
                {
                    go = ProcessEmbeddedSelectable(to);
                    newObjects.Add(go.GetComponent<TrackedObject>());
                }
                else if (to.global_guid == attachPointGUID) // attachment point component
                {
                    ProcessAttachmentPoint(to);
                }
                else // selectable attached to an attachment point
                {
                    ProcessAttachedSelectable(go, to);
                    newObjects.Add(go.GetComponent<TrackedObject>());
                }
            }
        }
    }

    GameObject InstantiateObject(TrackedObject.Data to)
    {
        GameObject go = Instantiate(ObjectMenu.Instance.GetPrefabByGUID(to.global_guid));
        go.transform.SetPositionAndRotation(to.pos, to.rot);
        go.name = to.instance_guid;
        go.GetComponent<Selectable>().guid = to.instance_guid;
        LogScale(go.GetComponent<Selectable>(), to);
        return go;
    }

    void ProcessAttachmentPoint(TrackedObject.Data to)
    {
        GameObject myself = GameObject.Find(to.parent);
        myself.GetComponent<AttachmentPoint>().guid = to.instance_guid;
        newPoints.Add(myself.GetComponent<AttachmentPoint>());
    }

    GameObject ProcessEmbeddedSelectable(TrackedObject.Data to)
    {
        GameObject go = GameObject.Find(to.parent);
        go.GetComponent<Selectable>().guid = to.instance_guid;
        LogScale(go.GetComponent<Selectable>(), to);
        go.transform.rotation = to.rot;
        return go;
    }

    void ProcessAttachedSelectable(GameObject go, TrackedObject.Data to)
    {
        AttachmentPoint ap = newPoints.Single(s => s.guid == to.parent);
        ap.SetAttachedSelectable(go.GetComponent<Selectable>());
        go.transform.SetParent(ap.gameObject.transform);
        go.GetComponent<Selectable>().ParentAttachmentPoint = ap;
        LogScale(go.GetComponent<Selectable>(), to);
    }

    async Task ResetObjectPositions(List<TrackedObject> newObjects)
    {
        newObjects.Reverse();
        foreach (TrackedObject obj in newObjects)
        {
            ResetScaleLevels(obj);
        }

        await Task.Yield();

        foreach (TrackedObject obj in newObjects)
        {
            ResetLocalPosition(obj);
        }
    }

    void ResetScaleLevels(TrackedObject obj)
    {
        if (obj.GetScaleLevel() != null)
        {
            obj.GetComponent<Selectable>().ScaleLevels.ForEach((item) => item.Selected = false);
            obj.GetScaleLevel().Selected = true;
        }
    }

    void ResetLocalPosition(TrackedObject obj)
    {
        if (!string.IsNullOrEmpty(obj.GetComponent<Selectable>().GUID) && obj.transform != obj.transform.root)
        {
            obj.transform.localPosition = Vector3.zero;
            obj.GetComponent<Selectable>().OriginalLocalPosition = obj.transform.localPosition;
        }
    }

    void LogScale(Selectable s, TrackedObject.Data to)
    {
        if (to.scaleLevel.Selected && to.scaleLevel != s.CurrentScaleLevel)
        {
            s.GetComponent<TrackedObject>().StoreValues(to);
        }
    }

    GameObject GetRoot()
    {
        return newObjects.Single(x => x.transform == x.transform.root).gameObject;
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
